Imports OpenTK
Imports OpenTK.Graphics.OpenGL4

Public Class TerrainComponent

    Private camera As Camera

    Private loader As Loader

    Private terrain As Terrain

    Private earth As EarthManager

    Private sun As SunManager

    Private model As RawModel

    Private objectComponent As TerrainObjectComponent

    Private terrainFrameBufferComponent As TerrainFrameBufferComponent

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

    Private lightProjection As Matrix4
    Private lightView As Matrix4

    Private terrainModel As Matrix4

    ' For blitting the rendered textures to the screen
    Dim quadRenderer As ScreenQuadRenderer
    Dim textureBlitterShader As Shader

    Dim terrainResolutionWidth As Integer
    Dim terrainResolutionHeight As Integer

    Private fogFalloff As Single

    Private Const SHADOW_WIDTH As Integer = 4096
    Private Const SHADOW_HEIGHT As Integer = 4096

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
                   ByRef screenQuadRenderer As ScreenQuadRenderer,
                   ByRef loaderComponent As Loader)
        camera = cam
        earth = earthManager
        sun = sunManager
        loader = loaderComponent
        amplitude = terrainAmplitude
        terrainResolutionWidth = DisplayDevice.Default.Width
        terrainResolutionHeight = DisplayDevice.Default.Height
        terrainFrameBufferComponent = New TerrainFrameBufferComponent(terrainResolutionWidth, terrainResolutionHeight)
        quadRenderer = screenQuadRenderer
        textureBlitterShader = New Shader("ScreenQuadRenderer.vert", "BlitTextureToScreen.frag")
        fogFalloff = 1.5

        shader = New Shader(shaderVertSrc, shaderFragSrc)
        shadowShader = New Shader(shadowShaderVertexSrc, shadowShaderFragSrc)

        Dim terrainPos = New Vector3(-(terrainWidth * terrainSampleDistance) * 0.5, earth.radius - 20, -(terrainWidth * terrainSampleDistance) * 0.5)

        Dim modelMatrix = createModelMatrix(terrainPos)
        objectComponent = New TerrainObjectComponent("ObjectShader.vert", "ObjectShader.frag", modelMatrix, amplitude, sun, cam, loader)

        terrain = New Terrain(terrainWidth,
                              terrainLength,
                              terrainAmplitude,
                              terrainSampleDistance,
                              "heightmapMask.jpg",
                              terrainPos,
                              loader,
                              objectComponent)


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
        GL.ActiveTexture(TextureUnit.Texture0)
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

        If GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) <> All.FramebufferComplete Then
            Throw New Exception("error: Shadow Mapping Framebuffer Incomplete")
        End If

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0)
    End Sub

    Public Sub RenderShadowMap(colorPreset As Preset)
        GL.Viewport(0, 0, SHADOW_WIDTH, SHADOW_HEIGHT)
        GL.Enable(EnableCap.DepthTest)
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFBO)
        GL.Enable(EnableCap.CullFace)
        GL.CullFace(CullFaceMode.Front)
        GL.FrontFace(FrontFaceDirection.Ccw)
        GL.Clear(ClearBufferMask.DepthBufferBit)

        shadowShader.Use()

        Dim shadowShaderUniformBinding =
            Sub(shadowShader As Shader, preset As Preset)
                lightProjection = Matrix4.CreateOrthographic(240.0, 240.0, 0.2, 2500.0)

                Dim lookAtPos = camera.Position + New Vector3(50, 0, 80)
                Dim lightCamPos = (sun.position * 1000.0) + camera.Position + New Vector3(50, 0, 80)
                lightView = Matrix4.LookAt(lightCamPos, lookAtPos, New Vector3(0.0, 1.0, 0.0))
                lightView *= Matrix4.CreateTranslation(New Vector3(-50, 0, -80))
                shadowShader.SetMat4("lightSpaceProjection", False, lightProjection)
                shadowShader.SetMat4("lightSpaceView", False, lightView)
            End Sub

        ' Draw randomly placed objects
        objectComponent.DrawObjects(shadowShader, colorPreset, shadowShaderUniformBinding)

        GL.Disable(EnableCap.CullFace)
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0)
        GL.Viewport(0, 0, terrainResolutionWidth, terrainResolutionHeight)
    End Sub

    Public Sub Render(colorPreset As Preset)
        configureShaderAndMatrices()

        prepareTerrain(terrain)

        ' Set texture units
        shader.SetInt("grassTex", 0)
        shader.SetInt("healthyGrassTex", 1)
        shader.SetInt("patchyGrassTex", 2)
        shader.SetInt("rockTex", 3)
        shader.SetInt("snowTex", 4)
        shader.SetInt("rockNormalMap", 5)
        shader.SetInt("shadowMap", 6)

        ' Bind textures
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

        ' Set terrain texturing parameters
        shader.SetFloat("snowHeight", 80.0)
        shader.SetFloat("grassCoverage", 0.77)

        ' Set sun and lighting related paramters
        shader.SetVec3("sunColor", colorPreset.lightColor)
        shader.SetVec3("sunDir", sun.lightDir)

        ' Set fog parameters
        shader.SetFloat("fogFalloff", fogFalloff * 0.000001)
        shader.SetVec3("fogColor", colorPreset.fogColor)
        shader.SetFloat("dispFactor", 20.0)

        ' Set camera parameter
        shader.SetVec3("cameraPos", camera.Position)

        ' Draw terrain
        terrainFrameBufferComponent.Bind()
        GL.Enable(EnableCap.DepthTest)
        GL.DrawElements(BeginMode.Triangles, model.NumVertices, DrawElementsType.UnsignedInt, 0)

        unbindTexturedModel()

        ' Draw randomly placed objects
        objectComponent.DrawObjects(colorPreset)
        terrainFrameBufferComponent.UnBind()
        GL.Disable(EnableCap.DepthTest)
    End Sub

    Public Sub Blit()
        textureBlitterShader.Use()
        textureBlitterShader.SetInt("textureToDraw", 0)
        GL.ActiveTexture(TextureUnit.Texture0)
        GL.BindTexture(TextureTarget.Texture2D, terrainFrameBufferComponent.currentFrame)
        quadRenderer.Render()
    End Sub

    Private Sub prepareTerrain(ByRef terrain As Terrain)
        GL.BindVertexArray(terrain.Model.VAO)
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

    Private Function createModelMatrix(terrainPos As Vector3) As Matrix4
        Dim modelMatrix = New Matrix4(1, 0, 0, 0,
                                      0, 1, 0, 0,
                                      0, 0, 1, 0,
                                      terrainPos.X, terrainPos.Y, terrainPos.Z, 1)
        Return modelMatrix
    End Function

    Private Sub loadModelMatrix(ByRef loadShader As Shader)
        terrainModel = createModelMatrix(terrain.Position)

        loadShader.SetMat4("model", False, terrainModel)
    End Sub

    Private Sub configureShaderAndMatrices()
        shader.Use()

        ' Send transformation matrices to shader
        loadModelMatrix(shader)
        shader.SetMat4("viewMatrix", False, camera.ViewMatrix)
        shader.SetMat4("projectionMatrix", False, camera.ProjectionMatrix)
        shader.SetMat4("lightSpaceProjection", False, lightProjection)
        shader.SetMat4("lightSpaceView", False, lightView)
    End Sub

    ' Allow retrieval of terrain occlusion for God rays
    Public ReadOnly Property OcclusionTexture As Integer
        Get
            Return terrainFrameBufferComponent.occlusionBuffer
        End Get
    End Property
End Class
