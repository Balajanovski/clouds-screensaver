Imports Assimp
Imports OpenTK.Graphics.OpenGL4
Imports OpenTK
Imports System.Collections.Generic
Imports System.Reflection
Imports System.IO

Public Class Model
    Private texturesLoaded As New List(Of Texture)
    Private meshes As New List(Of Mesh)
    Private directory As String
    Private gammaCorrection As Boolean
    Private loader As Loader

    Public Sub New(ByRef path As String, ByRef nLoader As Loader, Optional gamma As Boolean = False)
        loader = nLoader
        gammaCorrection = gamma

        loadModel(path)
    End Sub

    Public Sub New(ByRef path As String, ByRef nLoader As Loader, formatHint As String, Optional gamma As Boolean = False)
        loader = nLoader
        gammaCorrection = gamma

        loadModel(path, formatHint)
    End Sub

    Public Sub Draw(ByRef shader As Shader)
        For Each mesh In meshes
            mesh.Draw(shader)
        Next
    End Sub

    Private Sub loadModel(path As String)
        Dim currentAssembly = Assembly.GetExecutingAssembly

        Dim modelStream As Stream _
            = currentAssembly.GetManifestResourceStream("Screensaver." + path)

        Dim importer As AssimpContext = New AssimpContext()
        Dim scene As Scene = importer.ImportFileFromStream(modelStream, PostProcessSteps.Triangulate Or
                                                                        PostProcessSteps.FlipUVs Or
                                                                        PostProcessSteps.GenerateNormals Or
                                                                        PostProcessSteps.CalculateTangentSpace)

        directory = path

        processNode(scene.RootNode, scene)
    End Sub

    Private Sub loadModel(path As String, formatHint As String)
        Dim currentAssembly = Assembly.GetExecutingAssembly

        Dim modelStream As Stream _
            = currentAssembly.GetManifestResourceStream("Screensaver." + path)

        Dim importer As AssimpContext = New AssimpContext()
        Dim scene As Scene = importer.ImportFileFromStream(modelStream, PostProcessSteps.Triangulate Or
                                                                        PostProcessSteps.FlipUVs Or
                                                                        PostProcessSteps.GenerateNormals Or
                                                                        PostProcessSteps.CalculateTangentSpace, formatHint)

        directory = path

        processNode(scene.RootNode, scene)
    End Sub

    Private Sub processNode(ByRef node As Node, ByRef scene As Scene)
        ' Process each mesh located at the current node
        For i = 0 To node.MeshCount - 1
            Dim mesh As Assimp.Mesh = scene.Meshes(node.MeshIndices(i))
            meshes.Add(processMesh(mesh, scene))
        Next

        For i = 0 To node.ChildCount - 1
            processNode(node.Children(i), scene)
        Next
    End Sub

    Private Function processMesh(ByRef mesh As Assimp.Mesh, ByRef scene As Scene) As Mesh
        ' Data to fill
        Dim vertices As New List(Of Vertex)
        Dim indices As New List(Of Integer)
        Dim textures As New List(Of Texture)

        ' Walk through each of the mesh's vertices
        For i = 0 To mesh.VertexCount - 1
            Dim vertex As Vertex
            Dim vector As Vector3

            ' Positions
            vector.X = mesh.Vertices(i).X
            vector.Y = mesh.Vertices(i).Y
            vector.Z = mesh.Vertices(i).Z
            vertex.Position = vector

            ' Normals
            vector.X = mesh.Normals(i).X
            vector.Y = mesh.Normals(i).Y
            vector.Z = mesh.Normals(i).Z
            vertex.Normal = vector

            ' Texture coordinates
            If mesh.HasTextureCoords(0) Then
                Dim vec As Vector2
                ' Models can have multiple channels of texture coordinates
                ' We only take the first one
                vec.X = mesh.TextureCoordinateChannels(0)(i).X
                vec.Y = mesh.TextureCoordinateChannels(0)(i).Y
                vertex.TexCoords = vec
            Else
                vertex.TexCoords = New Vector2(0.0, 0.0)
            End If

            ' Tangent
            vector.X = mesh.Tangents(i).X
            vector.Y = mesh.Tangents(i).Y
            vector.Z = mesh.Tangents(i).Z
            vertex.Tangent = vector

            ' Bitangent
            vector.X = mesh.BiTangents(i).X
            vector.Y = mesh.BiTangents(i).Y
            vector.Z = mesh.BiTangents(i).Z
            vertex.Bitangent = vector
            vertices.Add(vertex)
        Next

        ' Walk through each of the mesh's faces and retrieve the corresponding vertex indices
        For i = 0 To mesh.FaceCount - 1
            Dim face As Face = mesh.Faces(i)
            ' Retrieve all indices of the face and store them in the indices vector
            For j = 0 To face.IndexCount - 1
                indices.Add(face.Indices(j))
            Next
        Next

        ' Process materials
        Dim material As Material = scene.Materials(mesh.MaterialIndex)

        ' Diffuse maps
        Dim diffuseMaps As List(Of Texture) = loadMaterialTextures(material, TextureType.Diffuse, "texture_diffuse")
        textures.AddRange(diffuseMaps)
        ' Specular maps
        Dim specularMaps As List(Of Texture) = loadMaterialTextures(material, TextureType.Specular, "texture_specular")
        textures.AddRange(specularMaps)
        ' Normal maps
        Dim normalMaps As List(Of Texture) = loadMaterialTextures(material, TextureType.Height, "texture_normal")
        textures.AddRange(normalMaps)
        ' Height maps
        Dim heightMaps As List(Of Texture) = loadMaterialTextures(material, TextureType.Ambient, "texture_height")
        textures.AddRange(heightMaps)
        ' Light emission maps
        Dim emissionMaps As List(Of Texture) = loadMaterialTextures(material, TextureType.Emissive, "texture_emissive")
        textures.AddRange(emissionMaps)

        ' Return a mesh object from the extracted data
        Return New Mesh(vertices, indices, textures)
    End Function

    Private Function loadMaterialTextures(ByRef mat As Assimp.Material,
                                          type As TextureType,
                                          typeName As String) As List(Of Texture)
        Dim textures As New List(Of Texture)

        Dim tex As New TextureSlot()
        Dim i As Integer = 0
        While mat.GetMaterialTexture(type, i, tex)

            ' Check if texture was loaded before and if so, continue to next iteration: skip loading a new texture
            Dim skip As Boolean = False
            For j = 0 To texturesLoaded.Count - 1
                If texturesLoaded(j).path = tex.FilePath Then
                    textures.Add(texturesLoaded(j))
                    skip = True
                    Exit For
                End If
            Next

            If Not skip Then
                Dim texture As Texture
                texture.id = loader.LoadTexture(tex.FilePath)
                texture.type = typeName
                texture.path = tex.FilePath
                textures.Add(texture)
                texturesLoaded.Add(texture)
            End If

            i += 1
        End While

        Return textures
    End Function
End Class
