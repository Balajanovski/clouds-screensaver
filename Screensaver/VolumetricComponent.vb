Imports OpenTK.Graphics.OpenGL4
Imports OpenTK

Public Class VolumetricComponent
    Private volumetricShader As Shader
    Private quadRenderer As ScreenQuadRenderer
    Private camera As Camera

    ' Generates 3D simplex noise
    Private simplexNoiseGen As NoiseGenerator3D
    Private perlinNoiseGen As NoiseGenerator2D

    ' Noise cache
    Private simplexNoise As Integer
    Private perlinNoise As Integer

    Public Sub New(vertexSrc As String, fragSrc As String, ByRef quadRen As ScreenQuadRenderer, ByRef cam As Camera)
        volumetricShader = New Shader(vertexSrc, fragSrc)
        quadRenderer = quadRen
        camera = cam

        simplexNoiseGen = New NoiseGenerator3D("Generate3DSimplexNoise.comp")
        perlinNoiseGen = New NoiseGenerator2D("Generate2DPerlinNoise.comp")
        simplexNoise = simplexNoiseGen.GenerateNoise(256, 256, 256, SizedInternalFormat.R8)
        simplexNoiseGen.AwaitComputationEnd()
        perlinNoise = perlinNoiseGen.GenerateNoise(256, 256, SizedInternalFormat.Rgba8)
        perlinNoiseGen.AwaitComputationEnd()

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
