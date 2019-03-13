Imports OpenTK
Imports OpenTK.Graphics.OpenGL4

Public Class TerrainComponent

    Private camera As Camera

    Private loader As Loader

    Private terrain As Terrain

    Private earth As EarthManager

    Private sun As SunManager

    Private model As RawModel

    Private shader As Shader
    Private shadowShader As Shader

    Private amplitude As Single

    ' Textures of terrain
    Private healthyGrassTexture As Integer
    Private grassTexture As Integer
    Private patchyGrassTexture As Integer
    Private rockTexture As Integer
    Private rnormalTexture As Integer
    Private snowTexture As Integer

    ' Used for shadow mapping
    Private depthMapFBO As Integer
    Private depthMap As Integer

    Private Const SHADOW_WIDTH As Integer = 1024
    Private Const SHADOW_HEIGHT As Integer = 1024

    Public Sub New(shaderVertSrc As String,
                   shaderFragSrc As String,
                   shadowShaderVertexSrc As String,
                   shadowShaderFragSrc As String,
                   terrainWidth As Integer,
                   terrainLength As Integer,
                   terrainAmplitude As Single,
                   terrainSampleDistance As Single,
                   ByRef cam As Camera,
                   ByRef earthManager As EarthManager,
                   ByRef sunManager As SunManager,
                   ByRef loaderComponent As Loader)
        camera = cam
        earth = earthManager
        sun = sunManager
        loader = loaderComponent
        amplitude = terrainAmplitude

        shader = New Shader(shaderVertSrc, shaderFragSrc)
        shadowShader = New Shader(shadowShaderVertexSrc, shadowShaderFragSrc)

        terrain = New Terrain(terrainWidth,
                              terrainLength,
                              terrainAmplitude,
                              terrainSampleDistance,
                              "heightmapMask.jpg",
                              New Vector3(-(terrainWidth * terrainSampleDistance) * 0.5, earth.radius - 20, -(terrainWidth * terrainSampleDistance) * 0.5),
                              loader)

        model = terrain.Model

        ' Load terrain textures
        grassTexture = loader.LoadTexture("grass.jpg")
        healthyGrassTexture = loader.LoadTexture("grass3.jpg")
        patchyGrassTexture = loader.LoadTexture("grass2.jpg")
        rockTexture = loader.LoadTexture("rdiffuse.jpg")
        rnormalTexture = loader.LoadTexture("rnormal.jpg")
        snowTexture = loader.LoadTexture("snow.jpg")

        ' Generate frame buffer for shadow mapping
        depthMapFBO = GL.GenFramebuffer()
        depthMap = GL.GenTexture()
        GL.BindTexture(TextureTarget.Texture2D, depthMap)
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent,
                      SHADOW_WIDTH, SHADOW_HEIGHT, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, All.Nearest)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, All.Nearest)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, All.Repeat)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, All.Repeat)

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFBO)
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthMap, 0)
        GL.DrawBuffer(All.None)
        GL.ReadBuffer(All.None)
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0)
    End Sub

    Public Sub RenderShadowMap()
        GL.Viewport(0, 0, SHADOW_WIDTH, SHADOW_HEIGHT)
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFBO)
        GL.Clear(ClearBufferMask.DepthBufferBit)
        configureShadowShaderAndMatrices()
        renderScene()
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0)
        GL.Viewport(0, 0, DisplayDevice.Default.Width, DisplayDevice.Default.Height)
    End Sub

    Public Sub Render()
        configureShaderAndMatrices()

        renderScene()
    End Sub

    Private Sub renderScene()
        prepareTerrain(terrain)

        shader.SetInt("grassTex", 0)
        shader.SetInt("healthyGrassTex", 1)
        shader.SetInt("patchyGrassTex", 2)
        shader.SetInt("rockTex", 3)
        shader.SetInt("snowTex", 4)
        shader.SetInt("rockNormalMap", 5)
        shader.SetInt("shadowMap", 6)

        GL.ActiveTexture(TextureUnit.Texture0)
        GL.BindTexture(TextureTarget.Texture2D, grassTexture)
        GL.ActiveTexture(TextureUnit.Texture1)
        GL.BindTexture(TextureTarget.Texture2D, healthyGrassTexture)
        GL.ActiveTexture(TextureUnit.Texture2)
        GL.BindTexture(TextureTarget.Texture2D, patchyGrassTexture)
        GL.ActiveTexture(TextureUnit.Texture3)
        GL.BindTexture(TextureTarget.Texture2D, rockTexture)
        GL.ActiveTexture(TextureUnit.Texture4)
        GL.BindTexture(TextureTarget.Texture2D, snowTexture)
        GL.ActiveTexture(TextureUnit.Texture5)
        GL.BindTexture(TextureTarget.Texture2D, rnormalTexture)
        GL.ActiveTexture(TextureUnit.Texture6)
        GL.BindTexture(TextureTarget.Texture2D, depthMap)

        shader.SetFloat("snowHeight", 80.0)
        shader.SetFloat("grassCoverage", 0.77)
        shader.SetVec3("sunColor", sun.color)
        shader.SetVec3("sunDir", sun.lightDir)

        GL.DrawElements(BeginMode.Triangles, model.NumVertices, DrawElementsType.UnsignedInt, 0)

        unbindTexturedModel()
    End Sub

    Private Sub prepareTerrain(ByRef terrain As Terrain)
        GL.BindVertexArray(model.VAO)
        GL.EnableVertexAttribArray(0)
        GL.EnableVertexAttribArray(1)
        GL.EnableVertexAttribArray(2)
    End Sub

    Private Sub unbindTexturedModel()
        GL.DisableVertexAttribArray(0)
        GL.DisableVertexAttribArray(1)
        GL.DisableVertexAttribArray(2)
        GL.BindVertexArray(0)
    End Sub

    Private Sub loadModelMatrix()
        Dim pos = terrain.Position
        Dim modelMatrix = New Matrix4(1, 0, 0, 0,
                                      0, 1, 0, 0,
                                      0, 0, 1, 0,
                                      pos.X, pos.Y, pos.Z, 1)

        shader.SetMat4("modelMatrix", False, modelMatrix)
    End Sub

    Private Sub configureShaderAndMatrices()
        shader.Use()

        ' Send transformation matrices to shader
        loadModelMatrix()
        shader.SetMat4("viewMatrix", False, camera.ViewMatrix)
        shader.SetMat4("projectionMatrix", False, camera.ProjectionMatrix)
    End Sub

    Private Sub configureShadowShaderAndMatrices()
        shadowShader.Use()

        ' Send transformation matrix to shader
        loadModelMatrix()
        Dim lightProjection = Matrix4.CreateOrthographic(DisplayDevice.Default.Width,
                      DisplayDevice.Default.Height, 1.0, 7.5)
        Dim lightView = Matrix4.LookAt(sun.position, New Vector3(0.0, 0.0, 0.0), New Vector3(0.0, 1.0, 0.0))
        Dim lightSpaceMatrix = lightProjection * lightView
        shader.SetMat4("lightSpaceMatrix", False, lightSpaceMatrix)
    End Sub
End Class
