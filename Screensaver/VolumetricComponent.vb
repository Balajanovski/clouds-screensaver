Imports OpenTK.Graphics.OpenGL4
Imports OpenTK

Public Class VolumetricComponent
    Private volumetricShader As Shader
    Private quadRenderer As ScreenQuadRenderer
    Private camera As Camera

    ' Generates fractal brownian motion
    Private noiseGen As NoiseGenerator

    ' Noise cache
    Private noiseLayer1 As Integer
    Private noiseLayer2 As Integer
    Private noiseLayer3 As Integer

    Public Sub New(vertexSrc As String, fragSrc As String, ByRef quadRen As ScreenQuadRenderer, ByRef cam As Camera)
        volumetricShader = New Shader(vertexSrc, fragSrc)
        quadRenderer = quadRen
        camera = cam

        noiseGen = New NoiseGenerator("GenerateFBMNoise.comp")
        noiseLayer1 = noiseGen.GenerateNoise(256, 256, 256)
        noiseGen.AwaitComputationEnd()
        noiseLayer2 = noiseGen.GenerateNoise(64, 64, 64)
        noiseGen.AwaitComputationEnd()
        noiseLayer3 = noiseGen.GenerateNoise(256, 256, 256)

        volumetricShader.Use()
    End Sub

    Public Sub Render(time As Single)
        volumetricShader.Use()

        ' Set uniforms
        volumetricShader.SetVec2("resolution", DisplayDevice.Default.Width, DisplayDevice.Default.Height)
        volumetricShader.SetFloat("time", time)
        volumetricShader.SetVec3("cameraPos", camera.Position)
        volumetricShader.SetMat4("view", False, camera.ViewMatrix)
        volumetricShader.SetFloat("fov", camera.FOV)
        volumetricShader.SetInt("noiseLayer1", 1)
        volumetricShader.SetInt("noiseLayer2", 2)
        volumetricShader.SetInt("noiseLayer3", 3)

        ' Activate textures
        GL.ActiveTexture(TextureUnit.Texture1)
        GL.BindTexture(TextureTarget.Texture3D, noiseLayer1)
        GL.ActiveTexture(TextureUnit.Texture2)
        GL.BindTexture(TextureTarget.Texture3D, noiseLayer2)
        GL.ActiveTexture(TextureUnit.Texture3)
        GL.BindTexture(TextureTarget.Texture3D, noiseLayer3)

        noiseGen.AwaitComputationEnd()
        quadRenderer.Render()
    End Sub
End Class
