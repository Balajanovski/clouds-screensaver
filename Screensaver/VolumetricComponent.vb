Imports OpenTK.Graphics.OpenGL4
Imports OpenTK
Imports SharpNoise
Imports SharpNoise.Builders
Imports SharpNoise.Modules

Public Class VolumetricComponent
    Private volumetricShader As Shader
    Private quadRenderer As ScreenQuadRenderer
    Private camera As Camera

    ' Noise to be integrated for opacity sampling
    Private billow As New Billow() With {
        .Seed = New Random().Next,
        .Quality = NoiseQuality.Standard,
        .Frequency = 1.5
    }
    Private cloudNoise As New Clamp() With {
        .LowerBound = 0.0,
        .UpperBound = 1.0,
        .Source0 = billow
    }
    Private noiseCube As New NoiseCube()
    Private noiseCubeBuilder As New LinearNoiseCubeBuilder With {
        .DestNoiseCube = noiseCube,
        .SourceModule = cloudNoise
    }
    Private Const VOL_TEX_DIM As Integer = 64
    Private volumeData(VOL_TEX_DIM * VOL_TEX_DIM * VOL_TEX_DIM) As Byte
    Private volumeTex1 As Integer
    Private volumeTex2 As Integer
    Private volumeTex3 As Integer

    ' Cancellation token for asynchronous noise generation
    Private cancelNoiseGeneration As Threading.CancellationToken

    ' Noise for positioning clouds in sky
    Private perlin As New Perlin() With {
        .Seed = New Random().Next,
        .Quality = NoiseQuality.Fast
    }
    Private positionDistortion As New Clamp() With {
        .LowerBound = 0.0,
        .UpperBound = 1.0,
        .Source0 = perlin
    }
    Private noisePlane As New NoiseMap()
    Private noisePlaneBuilder As New PlaneNoiseMapBuilder With {
        .DestNoiseMap = noisePlane,
        .SourceModule = positionDistortion
    }
    Private Const DIST_TEX_DIM As Integer = 32
    Private distortionData(DIST_TEX_DIM * DIST_TEX_DIM) As Byte
    Private distortionTex1 As Integer
    Private distortionTex2 As Integer

    ' Utility method to treat 1D array as a 3D array
    Private Shared Sub Set3D(ByVal val As Byte,
                                ByVal x As Integer,
                                ByVal y As Integer,
                                ByVal z As Integer,
                                ByRef arr As Byte())
        arr(x + VOL_TEX_DIM * (y + VOL_TEX_DIM * z)) = val
    End Sub

    ' Utility method to treat 1D array as a 2D array
    Private Shared Sub Set2D(ByVal val As Byte,
                                ByVal x As Integer,
                                ByVal y As Integer,
                                ByRef arr As Byte())
        arr(x + (DIST_TEX_DIM * y)) = val
    End Sub

    Private Shared Async Sub AwaitTaskAsync(task As Task)
        Await task
    End Sub

    Private Function GenerateCloudVolume() As Integer
        ' Reseed noise generator
        billow.Seed = New Random().Next

        ' Generate 3D noise for cloud opacity testing
        noiseCubeBuilder.Build()

        ' Copy all of the generated volume data into local array
        For z = 0 To VOL_TEX_DIM - 1
            For y = 0 To VOL_TEX_DIM - 1
                For x = 0 To VOL_TEX_DIM - 1
                    Set3D(CType(noiseCube.GetValue(x, y, z) * 255.0, Byte), x, y, z, volumeData)
                Next
            Next
        Next

        ' Transfer volume noise data into OpenGL texture
        Dim volumeTex = GL.GenTexture()
        GL.BindTexture(TextureTarget.Texture3D, volumeTex)
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1)
        GL.TexImage3D(TextureTarget.Texture3D, 0, PixelFormat.Red, VOL_TEX_DIM,
                      VOL_TEX_DIM, VOL_TEX_DIM, 0, PixelFormat.Red, PixelType.UnsignedByte, volumeData)
        GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, All.Linear)
        GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, All.Linear)
        GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, All.MirroredRepeat)
        GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, All.MirroredRepeat)
        GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, All.MirroredRepeat)

        Return volumeTex
    End Function

    Private Function GenerateDistortionData() As Integer
        ' Reseed noise generator
        perlin.Seed = New Random().Next()

        ' Generate 2D noise for cloud position distortion
        noisePlaneBuilder.Build()

        ' Copy all of the generated distortion data into local array
        For y = 0 To DIST_TEX_DIM - 1
            For x = 0 To DIST_TEX_DIM - 1
                Set2D(CType(noisePlane.GetValue(x, y) * 255.0, Byte), x, y, distortionData)
            Next
        Next

        ' Transfer distortion noise data into OpenGL texture
        Dim distortionTexture = GL.GenTexture()
        GL.BindTexture(TextureTarget.Texture2D, distortionTexture)
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelFormat.Red, DIST_TEX_DIM,
                      DIST_TEX_DIM, 0, PixelFormat.Red, PixelType.UnsignedByte, distortionData)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, All.Linear)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, All.Linear)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, All.MirroredRepeat)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, All.MirroredRepeat)

        Return distortionTexture
    End Function

    Public Sub New(vertexSrc As String, fragSrc As String, ByRef quadRen As ScreenQuadRenderer, ByRef cam As Camera)
        volumetricShader = New Shader(vertexSrc, fragSrc)
        quadRenderer = quadRen
        camera = cam

        ' Set 3D noise builder parameters
        noiseCubeBuilder.SetDestSize(VOL_TEX_DIM, VOL_TEX_DIM, VOL_TEX_DIM)
        noiseCubeBuilder.SetBounds(-1.0, 1.0, -1.0, 1.0, -1.0, 1.0)

        ' Set 2D noise builder paramters
        noisePlaneBuilder.SetDestSize(DIST_TEX_DIM, DIST_TEX_DIM)
        noisePlaneBuilder.SetBounds(-1.0, 1.0, -1.0, 1.0)

        ' Generate 3D noise
        volumeTex1 = GenerateCloudVolume()
        volumeTex2 = GenerateCloudVolume()
        volumeTex3 = GenerateCloudVolume()

        ' Generate 2D noise
        distortionTex1 = GenerateDistortionData()
        distortionTex2 = GenerateDistortionData()

        volumetricShader.Use()
    End Sub

    Public Sub Render(time As Single)
        volumetricShader.Use()

        ' Set uniforms
        volumetricShader.SetInt("volumeTexLayer1", 1)
        volumetricShader.SetInt("volumeTexLayer2", 2)
        volumetricShader.SetInt("volumeTexLayer3", 3)
        volumetricShader.SetInt("cloudDistLayer1", 4)
        volumetricShader.SetInt("cloudDistLayer1", 5)
        volumetricShader.SetVec2("resolution", DisplayDevice.Default.Width, DisplayDevice.Default.Height)
        volumetricShader.SetFloat("time", time)
        volumetricShader.SetVec3("cameraPos", camera.Position)
        volumetricShader.SetMat4("view", False, camera.ViewMatrix)
        volumetricShader.SetFloat("fov", camera.FOV)

        ' Activate and bind textures
        GL.ActiveTexture(TextureUnit.Texture1)
        GL.BindTexture(TextureTarget.Texture3D, volumeTex1)
        GL.ActiveTexture(TextureUnit.Texture2)
        GL.BindTexture(TextureTarget.Texture3D, volumeTex2)
        GL.ActiveTexture(TextureUnit.Texture3)
        GL.BindTexture(TextureTarget.Texture3D, volumeTex3)
        GL.ActiveTexture(TextureUnit.Texture4)
        GL.BindTexture(TextureTarget.Texture2D, distortionTex1)
        GL.ActiveTexture(TextureUnit.Texture5)
        GL.BindTexture(TextureTarget.Texture2D, distortionTex2)

        quadRenderer.Render()
    End Sub
End Class
