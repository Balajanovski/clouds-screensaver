Imports OpenTK.Graphics.OpenGL4
Imports OpenTK

Public Class VolumetricComponent
    Private volumetricShader As Shader
    Private quadRenderer As ScreenQuadRenderer
    Private camera As Camera
    Private earth As EarthManager
    Private sun As SunManager

    ' Generates noises
    Private perlinWorleyNoiseGen As NoiseGenerator3D
    Private worleyNoiseGen As NoiseGenerator3D
    Private perlinNoiseGen As NoiseGenerator2D
    Private curlNoiseGen As NoiseGenerator2D

    ' Noise cache
    Private perlinWorleyNoise As Integer
    Private worleyNoise As Integer
    Private perlinNoise As Integer
    Private curlNoise As Integer

    Public Sub New(vertexSrc As String, fragSrc As String, ByRef quadRen As ScreenQuadRenderer, ByRef cam As Camera, ByRef earthManager As EarthManager, ByRef sunManager As SunManager)
        volumetricShader = New Shader(vertexSrc, fragSrc)
        quadRenderer = quadRen
        camera = cam
        earth = earthManager
        sun = sunManager

        perlinWorleyNoiseGen = New NoiseGenerator3D("Generate3DPerlinWorleyNoise.comp")
        worleyNoiseGen = New NoiseGenerator3D("Generate3DWorleyNoise.comp")
        perlinNoiseGen = New NoiseGenerator2D("GenerateWeatherTexture.comp")
        curlNoiseGen = New NoiseGenerator2D("Generate2DCurlNoise.comp")
        perlinWorleyNoise = perlinWorleyNoiseGen.GenerateNoise(128, 128, 128, SizedInternalFormat.Rgba8)
        perlinWorleyNoiseGen.AwaitComputationEnd()
        worleyNoise = worleyNoiseGen.GenerateNoise(32, 32, 32, SizedInternalFormat.Rgba8)
        worleyNoiseGen.AwaitComputationEnd()
        perlinNoise = perlinNoiseGen.GenerateNoise(1024, 1024, SizedInternalFormat.Rgba8)
        perlinNoiseGen.AwaitComputationEnd()
        curlNoise = curlNoiseGen.GenerateNoise(128, 128, SizedInternalFormat.Rgba8)
        curlNoiseGen.AwaitComputationEnd()

        volumetricShader.Use()
    End Sub

    Public Sub Render(time As Single)
        volumetricShader.Use()

        ' Set uniforms
        volumetricShader.SetVec2("resolution", DisplayDevice.Default.Width, DisplayDevice.Default.Height)
        volumetricShader.SetFloat("time", time)
        volumetricShader.SetVec3("cameraPos", camera.Position)
        volumetricShader.SetMat4("inverseView", False, Matrix4.Invert(camera.ViewMatrix))
        volumetricShader.SetMat4("inverseProjection", False, Matrix4.Invert(camera.ProjectionMatrix))
        volumetricShader.SetVec3("sunColor", sun.color)
        volumetricShader.SetVec3("sunDir", sun.lightDir)
        volumetricShader.SetFloat("EARTH_RADIUS", earth.radius)


        volumetricShader.SetInt("cloudNoise", 1)
        volumetricShader.SetInt("weatherTexture", 2)
        volumetricShader.SetInt("curlNoise", 3)
        volumetricShader.SetInt("worleyNoise", 4)

        ' Activate texture units
        GL.ActiveTexture(TextureUnit.Texture1)
        GL.BindTexture(TextureTarget.Texture3D, perlinWorleyNoise)
        GL.ActiveTexture(TextureUnit.Texture2)
        GL.BindTexture(TextureTarget.Texture2D, perlinNoise)
        GL.ActiveTexture(TextureUnit.Texture3)
        GL.BindTexture(TextureTarget.Texture2D, curlNoise)
        GL.ActiveTexture(TextureUnit.Texture4)
        GL.BindTexture(TextureTarget.Texture3D, worleyNoise)

        quadRenderer.Render()
    End Sub
End Class
