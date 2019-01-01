Imports OpenTK.Graphics.OpenGL4
Imports OpenTK

Public Class VolumetricComponent
    Private volumetricShader As Shader
    Private quadRenderer As ScreenQuadRenderer
    Private camera As Camera

    ' Generates fractal brownian motion
    Private noiseGen As NoiseGenerator

    ' Noise cache
    Private simplexNoise As Integer

    Public Sub New(vertexSrc As String, fragSrc As String, ByRef quadRen As ScreenQuadRenderer, ByRef cam As Camera)
        volumetricShader = New Shader(vertexSrc, fragSrc)
        quadRenderer = quadRen
        camera = cam

        noiseGen = New NoiseGenerator("GenerateSimplexNoise.comp")
        simplexNoise = noiseGen.GenerateNoise(256, 256, 256)
        noiseGen.AwaitComputationEnd()

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
        volumetricShader.SetInt("simplexNoise", 1)

        ' Activate textures
        GL.ActiveTexture(TextureUnit.Texture1)
        GL.BindTexture(TextureTarget.Texture3D, simplexNoise)

        quadRenderer.Render()
    End Sub
End Class
