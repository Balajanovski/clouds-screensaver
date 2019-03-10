Imports OpenTK
Imports OpenTK.Graphics.OpenGL4

Public Class TerrainComponent

    Private camera As Camera

    Private loader As Loader

    Private terrain As Terrain

    Private earth As EarthManager

    Private model As RawModel

    Private shader As Shader

    Private amplitude As Single

    Private healthyGrassTexture As Integer
    Private grassTexture As Integer
    Private patchyGrassTexture As Integer
    Private rockTexture As Integer
    Private snowTexture As Integer

    Public Sub New(shaderVertSrc As String,
                   shaderFragSrc As String,
                   terrainWidth As Integer,
                   terrainLength As Integer,
                   terrainAmplitude As Single,
                   terrainSampleDistance As Single,
                   ByRef cam As Camera,
                   ByRef earthManager As EarthManager,
                   ByRef loaderComponent As Loader)
        camera = cam
        earth = earthManager
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
        snowTexture = loader.LoadTexture("snow.jpg")
    End Sub

    Public Sub Render()
        shader.Use()
        prepareTerrain(terrain)

        ' Send transformation matrices to shader
        loadModelMatrix()
        shader.SetMat4("viewMatrix", False, camera.ViewMatrix)
        shader.SetMat4("projectionMatrix", False, camera.ProjectionMatrix)

        shader.SetInt("grass", 0)
        shader.SetInt("healthyGrass", 1)
        shader.SetInt("patchyGrass", 2)
        shader.SetInt("rock", 3)
        shader.SetInt("snow", 4)

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

        shader.SetFloat("snowHeight", 80.0)
        shader.SetFloat("grassCoverage", 0.77)

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
