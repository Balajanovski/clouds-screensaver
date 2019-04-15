Imports OpenTK
Imports OpenTK.Graphics.OpenGL4
Imports System.Collections.Generic
Imports System.Runtime.InteropServices

Public Structure Vertex
    Public Position As Vector3
    Public Normal As Vector3
    Public TexCoords As Vector2
    Public Tangent As Vector3
    Public Bitangent As Vector3
End Structure

Public Structure Texture
    Public id As Integer
    Public type As String
    Public path As String
End Structure

Public Class Mesh
    Private vertices As List(Of Vertex)
    Private indices As List(Of Integer)
    Private textures As List(Of Texture)
    Private VAO As Integer

    ' Render data
    Private VBO As Integer
    Private EBO As Integer

    Public Sub New(nVertices As List(Of Vertex),
                    nIndices As List(Of Integer),
                    nTextures As List(Of Texture))
        vertices = nVertices
        indices = nIndices
        textures = nTextures

        setupMesh()
    End Sub

    Public Sub Draw(ByRef shader As Shader)
        ' Bind appropriate textures
        Dim diffuseNr As Integer = 1
        Dim specularNr As Integer = 1
        Dim normalNr As Integer = 1
        Dim heightNr As Integer = 1
        For i = 0 To textures.Count - 1
            GL.ActiveTexture(TextureUnit.Texture0 + i)

            Dim number As String
            Dim name As String = textures(i).type
            If name = "texture_diffuse" Then
                number = CStr(diffuseNr)
                diffuseNr += 1
            ElseIf name = "texture_normal" Then
                number = CStr(normalNr)
                normalNr += 1
            ElseIf name = "texture_height" Then
                number = CStr(heightNr)
                heightNr += 1
            Else
                Throw New Exception("error: Unknown texture name")
            End If

            shader.SetInt(name + number, i)
            GL.BindTexture(TextureTarget.Texture2D, textures(i).id)
        Next

        ' Draw mesh
        GL.BindVertexArray(VAO)
        GL.BindBuffer(BufferTarget.ArrayBuffer, VBO)
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO)
        GL.DrawElements(BeginMode.Triangles, indices.Count, DrawElementsType.UnsignedInt, 0)
        GL.BindVertexArray(0)

        GL.ActiveTexture(TextureUnit.Texture0) ' Reset to default
    End Sub

    ' Initialises all the buffer objects/arrays
    Private Sub setupMesh()
        ' Generate buffers / arrays
        VAO = GL.GenVertexArray()
        VBO = GL.GenBuffer()
        EBO = GL.GenBuffer()

        GL.BindVertexArray(VAO)
        ' Load data into vertex buffers
        GL.BindBuffer(BufferTarget.ArrayBuffer, VBO)

        ' Since structure memory layout is sequential for its items
        ' simply pass a pointer to the struct
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * Len(New Vertex), vertices.ToArray(), BufferUsageHint.StaticDraw)

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO)
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * Len(New Integer), indices.ToArray(), BufferUsageHint.StaticDraw)

        GL.EnableVertexAttribArray(0)
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, False, Len(New Vertex), IntPtr.Zero)

        GL.EnableVertexAttribArray(1)
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, False, Len(New Vertex), Marshal.OffsetOf(GetType(Vertex), "Normal"))

        GL.EnableVertexAttribArray(2)
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, False, Len(New Vertex), Marshal.OffsetOf(GetType(Vertex), "TexCoords"))

        GL.EnableVertexAttribArray(3)
        GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, False, Len(New Vertex), Marshal.OffsetOf(GetType(Vertex), "Tangent"))

        GL.EnableVertexAttribArray(4)
        GL.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, False, Len(New Vertex), Marshal.OffsetOf(GetType(Vertex), "Bitangent"))

        GL.BindVertexArray(0)
    End Sub

End Class
