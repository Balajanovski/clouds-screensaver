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

    Private amplitude As Single

    Private healthyGrassTexture As Integer
    Private grassTexture As Integer
    Private patchyGrassTexture As Integer
    Private rockTexture As Integer
    Private rnormalTexture As Integer
    Private rparallaxTexture As Integer
    Private snowTexture As Integer

    Public Sub New(shaderVertSrc As String,
                   shaderFragSrc As String,
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

        terrain = New Terrain(terrainWidth,
                              terrainLength,
                              terrainAmplitude,
                              terrainSampleDistance,
                              "heightmapMask.jpg",
                              New Vector3(-(terrainWidth * terrainSampleDistance) * 0.5, earth.radius - 20, -(terrainWidth * terrainSampleDistance) * 0.5),
                              loader)

        model = terrain.Model
        grassTexture = loader.LoadTexture("grass.jpg")
        healthyGrassTexture = loader.LoadTexture("grass3.jpg")
        patchyGrassTexture = loader.LoadTexture("grass2.jpg")
        rockTexture = loader.LoadTexture("rdiffuse.jpg")
        rnormalTexture = loader.LoadTexture("rnormal.jpg")
        rparallaxTexture = loader.LoadTexture("rdisp.png")
        snowTexture = loader.LoadTexture("snow.jpg")
    End Sub

    Public Sub Render()
        shader.Use()
        prepareTerrain(terrain)

        ' Send transformation matrices to shader
        loadModelMatrix()
        shader.SetMat4("viewMatrix", False, camera.ViewMatrix)
        shader.SetMat4("projectionMatrix", False, camera.ProjectionMatrix)

        shader.SetInt("grassTex", 0)
        shader.SetInt("healthyGrassTex", 1)
        shader.SetInt("patchyGrassTex", 2)
        shader.SetInt("rockTex", 3)
        shader.SetInt("snowTex", 4)
        shader.SetInt("rockNormalMap", 5)
        shader.SetInt("rockParallaxMap", 6)

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
        GL.BindTexture(TextureTarget.Texture2D, rparallaxTexture)

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

End Class
