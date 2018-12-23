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
    Private Const VOL_TEX_DIM As Integer = 128
    Private volumeData(VOL_TEX_DIM * VOL_TEX_DIM * VOL_TEX_DIM) As Byte
    Private volumeTexture As Integer

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
    Private Const DIST_TEX_DIM As Integer = 64
    Private distortionData(DIST_TEX_DIM * DIST_TEX_DIM) As Byte
    Private distortionTexture As Integer

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

    Private Shared Async Sub AwaitTask(task As Task)
        Await task
    End Sub


    Public Sub New(vertexSrc As String, fragSrc As String, ByRef quadRen As ScreenQuadRenderer, ByRef cam As Camera)
        volumetricShader = New Shader(vertexSrc, fragSrc)
        quadRenderer = quadRen
        camera = cam

        ' Generate 3D noise for cloud opacity testing
        noiseCubeBuilder.SetDestSize(VOL_TEX_DIM, VOL_TEX_DIM, VOL_TEX_DIM)
        noiseCubeBuilder.SetBounds(-1.0, 1.0, -1.0, 1.0, -1.0, 1.0)
        Dim volTexBuildTask As Task = noiseCubeBuilder.BuildAsync(cancelNoiseGeneration)

        ' Generate 2D noise for cloud position distortion
        noisePlaneBuilder.SetDestSize(DIST_TEX_DIM, DIST_TEX_DIM)
        noisePlaneBuilder.SetBounds(-1.0, 1.0, -1.0, 1.0)
        Dim distTexBuildTask As Task = noisePlaneBuilder.BuildAsync(cancelNoiseGeneration)

        ' Copy all of the generated volume data into local array
        AwaitTask(volTexBuildTask)
        For z = 0 To VOL_TEX_DIM - 1
            For y = 0 To VOL_TEX_DIM - 1
                For x = 0 To VOL_TEX_DIM - 1
                    Set3D(CType(noiseCube.GetValue(x, y, z) * 255.0, Byte), x, y, z, volumeData)
                Next
            Next
        Next

        ' Copy all of the generated distortion data into local array
        AwaitTask(distTexBuildTask)
        For y = 0 To DIST_TEX_DIM - 1
            For x = 0 To DIST_TEX_DIM - 1
                Set2D(CType(noisePlane.GetValue(x, y) * 255.0, Byte), x, y, distortionData)
            Next
        Next

        ' Transfer volume noise data into OpenGL texture
        volumeTexture = GL.GenTexture()
        GL.BindTexture(TextureTarget.Texture3D, volumeTexture)
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1)
        GL.TexImage3D(TextureTarget.Texture3D, 0, PixelFormat.Red, VOL_TEX_DIM,
                      VOL_TEX_DIM, VOL_TEX_DIM, 0, PixelFormat.Red, PixelType.UnsignedByte, volumeData)
        GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, All.Linear)
        GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, All.Linear)
        GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, All.MirroredRepeat)
        GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, All.MirroredRepeat)
        GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, All.MirroredRepeat)

        ' Transfer distortion noise data into OpenGL texture
        distortionTexture = GL.GenTexture()
        GL.BindTexture(TextureTarget.Texture2D, distortionTexture)
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelFormat.Red, DIST_TEX_DIM,
                      DIST_TEX_DIM, 0, PixelFormat.Red, PixelType.UnsignedByte, distortionData)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, All.Linear)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, All.Linear)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, All.MirroredRepeat)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, All.MirroredRepeat)

        volumetricShader.Use()
    End Sub

    Public Sub Render(time As Single)
        volumetricShader.Use()

        ' Set uniforms
        volumetricShader.SetInt("volumeTexture", 1)
        volumetricShader.SetInt("cloudDistortion", 2)
        volumetricShader.SetVec2("resolution", DisplayDevice.Default.Width, DisplayDevice.Default.Height)
        volumetricShader.SetFloat("time", time)
        volumetricShader.SetVec3("cameraPos", camera.Position)
        volumetricShader.SetMat4("view", False, camera.ViewMatrix)

        ' Activate and bind textures
        GL.ActiveTexture(TextureUnit.Texture1)
        GL.BindTexture(TextureTarget.Texture3D, volumeTexture)
        GL.ActiveTexture(TextureUnit.Texture2)
        GL.BindTexture(TextureTarget.Texture2D, distortionTexture)

        quadRenderer.Render()
    End Sub
End Class
