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

    Private grassTexture As Integer

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
                              New Vector3(0.0, earth.radius, 0.0),
                              loader)

        model = terrain.Model
        grassTexture = loader.LoadTexture("grass.jpg")
    End Sub

    Public Sub Render()
        shader.Use()
        prepareTerrain(terrain)

        ' Send transformation matrices to shader
        loadModelMatrix()
        shader.SetMat4("viewMatrix", False, camera.ViewMatrix)
        shader.SetMat4("projectionMatrix", False, camera.ProjectionMatrix)

        shader.SetInt("grassTexture", 0)
        GL.ActiveTexture(TextureUnit.Texture0)
        GL.BindTexture(TextureTarget.Texture2D, grassTexture)

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
                                      pos.X, pos.Y - (1.45 * amplitude), pos.Z, 1)

        shader.SetMat4("modelMatrix", False, modelMatrix)
    End Sub

End Class
