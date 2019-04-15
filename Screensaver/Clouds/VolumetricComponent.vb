Imports OpenTK.Graphics.OpenGL4
Imports OpenTK

Public Class VolumetricComponent
    Private volumetricShader As Shader
    Private postProcessClouds As Shader
    Private postProcessShader As Shader

    Private quadRenderer As ScreenQuadRenderer
    Private camera As Camera
    Private earth As EarthManager
    Private sun As SunManager
    Private temporalProjection As VolumetricCloudsFrameBuffer

    ' Store previous view projection matrix for temporal reprojection
    Dim oldViewProjection As Matrix4

    Dim frameIter As Integer

    ' Generates noises
    Private perlinWorleyNoiseGen As NoiseGenerator3D
    Private worleyNoiseGen As NoiseGenerator3D
    Private weatherNoiseGen As NoiseGenerator2D
    Private curlNoiseGen As NoiseGenerator2D

    ' Noise cache
    Private perlinWorleyNoise As Integer
    Private worleyNoise As Integer
    Private weatherNoise As Integer
    Private curlNoise As Integer

    ' Clouds resolution
    Private ReadOnly cloudsResolutionWidth As Integer
    Private ReadOnly cloudsResolutionHeight As Integer

    Public Sub New(vertexSrc As String,
                   fragSrc As String,
                   ByRef quadRen As ScreenQuadRenderer,
                   ByRef cam As Camera,
                   ByRef earthManager As EarthManager,
                   ByRef sunManager As SunManager)
        cloudsResolutionWidth = DisplayDevice.Default.Width * 0.5
        cloudsResolutionHeight = DisplayDevice.Default.Height * 0.5
        temporalProjection = New VolumetricCloudsFrameBuffer(cloudsResolutionWidth, cloudsResolutionHeight)

        volumetricShader = New Shader(vertexSrc, fragSrc)
        quadRenderer = quadRen
        camera = cam
        earth = earthManager
        sun = sunManager
        frameIter = 0
        postProcessClouds = New Shader("ScreenQuadRenderer.vert", "PostProcessClouds.frag")
        oldViewProjection = camera.ProjectionMatrix * camera.ViewMatrix

        perlinWorleyNoiseGen = New NoiseGenerator3D("Generate3DPerlinWorleyNoise.comp")
        worleyNoiseGen = New NoiseGenerator3D("Generate3DWorleyNoise.comp")
        weatherNoiseGen = New NoiseGenerator2D("GenerateWeatherTexture.comp")
        curlNoiseGen = New NoiseGenerator2D("Generate2DCurlNoise.comp")

        perlinWorleyNoise = perlinWorleyNoiseGen.GenerateNoise(128, 128, 128, SizedInternalFormat.Rgba8)

        worleyNoise = worleyNoiseGen.GenerateNoise(32, 32, 32, SizedInternalFormat.Rgba8)

        weatherNoiseGen.Seed(RandomFloatGenerator.instance().NextFloat())
        weatherNoise = weatherNoiseGen.GenerateNoise(1024, 1024, SizedInternalFormat.Rgba8)

        curlNoise = curlNoiseGen.GenerateNoise(128, 128, SizedInternalFormat.Rgba8)


        perlinWorleyNoiseGen.AwaitComputationEnd()
        worleyNoiseGen.AwaitComputationEnd()
        weatherNoiseGen.AwaitComputationEnd()
        curlNoiseGen.AwaitComputationEnd()

        volumetricShader.Use()
    End Sub

    Protected Overrides Sub Finalize()
        volumetricShader.FreeResources()
    End Sub

    Public Sub Render(time As Single)
        volumetricShader.Use()

        ' Set uniforms
        volumetricShader.SetVec2("resolution", cloudsResolutionWidth, cloudsResolutionHeight)
        volumetricShader.SetFloat("time", time)
        volumetricShader.SetVec3("cameraPos", camera.Position)


        Dim inverseView = camera.ViewMatrix.Inverted()
        Dim inverseProjection = camera.ProjectionMatrix.Inverted()
        Dim inverseViewProjection = (camera.ProjectionMatrix * camera.ViewMatrix).Inverted()

        volumetricShader.SetMat4("inverseView", False, inverseView)
        volumetricShader.SetMat4("inverseProjection", False, inverseProjection)
        volumetricShader.SetMat4("inverseViewProjection", False, inverseViewProjection)
        volumetricShader.SetMat4("oldViewProjection", False, oldViewProjection)

        volumetricShader.SetVec3("sunColor", sun.color)
        volumetricShader.SetVec3("sunDir", sun.lightDir)
        volumetricShader.SetFloat("EARTH_RADIUS", earth.radius)
        volumetricShader.SetInt("frameIter", frameIter)

        volumetricShader.SetInt("cloudNoise", 1)
        volumetricShader.SetInt("weatherTexture", 2)
        volumetricShader.SetInt("curlNoise", 3)
        volumetricShader.SetInt("worleyNoise", 4)
        volumetricShader.SetInt("lastFrame", 5)
        volumetricShader.SetInt("lastFrameAlphaness", 6)

        ' Activate texture units
        GL.ActiveTexture(TextureUnit.Texture1)
        GL.BindTexture(TextureTarget.Texture3D, perlinWorleyNoise)
        GL.ActiveTexture(TextureUnit.Texture2)
        GL.BindTexture(TextureTarget.Texture2D, weatherNoise)
        GL.ActiveTexture(TextureUnit.Texture3)
        GL.BindTexture(TextureTarget.Texture2D, curlNoise)
        GL.ActiveTexture(TextureUnit.Texture4)
        GL.BindTexture(TextureTarget.Texture3D, worleyNoise)
        GL.ActiveTexture(TextureUnit.Texture5)
        GL.BindTexture(TextureTarget.Texture2D, temporalProjection.lastFrame)
        GL.ActiveTexture(TextureUnit.Texture6)
        GL.BindTexture(TextureTarget.Texture2D, temporalProjection.lastFrameAlphaness)

        ' Cache view projection matrix for temporal reprojection
        oldViewProjection = camera.ProjectionMatrix * camera.ViewMatrix

        ' Render volumetric to FBO
        temporalProjection.Bind()
        quadRenderer.Render()
        temporalProjection.UnBind()

        ' Increment frame iter counter modulus 16 for temporal reprojection
        frameIter = (frameIter + 1) Mod 16
    End Sub

    Public Sub Blit()
        ' Blit volumetric clouds to screen with post processing
        postProcessClouds.Use()
        postProcessClouds.SetInt("textureToDraw", 0)
        postProcessClouds.SetVec2("resolution", cloudsResolutionWidth, cloudsResolutionHeight)
        GL.ActiveTexture(TextureUnit.Texture0)
        GL.BindTexture(TextureTarget.Texture2D, temporalProjection.currentFrame)
        quadRenderer.Render()
    End Sub
End Class
