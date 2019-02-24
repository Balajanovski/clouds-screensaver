Imports OpenTK.Graphics.OpenGL4
Imports System.Collections.Generic
Imports System.Reflection
Imports System.IO
Imports System.Drawing

Public Class Loader

    Public Function LoadToVao(ByRef positions() As Single,
                              ByRef indices() As Integer,
                              ByRef normals() As Single,
                              ByRef texture() As Single) As RawModel
        Dim id As Integer = CreateVAO()
        bindIndexBuffer(indices)

        storeDataInAttributeList(0, 3, 0, 0, positions)
        storeDataInAttributeList(1, 3, 0, 0, normals)
        storeDataInAttributeList(2, 2, 0, 0, texture)

        ' Unbind VAO
        GL.BindVertexArray(0)

        Return New RawModel(id, indices.Length())
    End Function

    Public Function LoadTexture(fileName As String)
        Dim currentAssembly = Assembly.GetExecutingAssembly

        Dim imageStream As Stream _
            = currentAssembly.GetManifestResourceStream("Screensaver." + fileName)
        Dim image = New Bitmap(imageStream)

        Dim textureID As Integer = GL.GenTexture()

        GL.BindTexture(TextureTarget.Texture2D, textureID)

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, All.Linear)
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, All.Linear)

        GL.TexImage2D(TextureTarget.Texture2D,
                      0, PixelInternalFormat.Rgba,
                      image.Width,
                      image.Height, 0,
                      PixelFormat.Bgra,
                      PixelType.UnsignedByte,
                      IntPtr.Zero)

        ' Lock pixel data to memory and prepare for pass through
        Dim imageData As Imaging.BitmapData = image.LockBits(New Rectangle(0, 0, image.Width, image.Height),
                                                             Imaging.ImageLockMode.ReadOnly, Imaging.PixelFormat.Format32bppArgb)

        ' Tell opengl to write the data from the image data to the bound texture
        GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0,
                         image.Width, image.Height, PixelFormat.Bgra,
                         PixelType.UnsignedByte, imageData.Scan0)

        ' Unlock the pixel data. Release from memory
        image.UnlockBits(imageData)

        Return textureID
    End Function

    ' Generate a vertex array object
    Public Function CreateVAO() As Integer
        Dim vao As Integer = GL.GenVertexArray()
        GL.BindVertexArray(vao)

        Return vao
    End Function

    ' Create a vertex buffer object
    Public Function CreateVBO(type As BufferTarget) As Integer
        Dim vbo As Integer = GL.GenBuffer()
        GL.BindBuffer(type, vbo)

        Return vbo
    End Function

    Private Sub bindIndexBuffer(ByRef indices() As Integer)
        CreateVBO(BufferTarget.ElementArrayBuffer)
        GL.BufferData(BufferTarget.ElementArrayBuffer,
                      indices.Length() * Len(New Integer),
                      indices,
                      BufferUsageHint.StaticDraw)
    End Sub

    Private Sub storeDataInAttributeList(attributeID As Integer,
                                         coordCount As Integer,
                                         stride As Integer,
                                         offset As Integer,
                                         ByRef data() As Integer)
        CreateVBO(BufferTarget.ArrayBuffer)

        ' Store data in OpenGL buffer
        GL.BufferData(BufferTarget.ArrayBuffer,
                      data.Length * Len(New Integer),
                      data,
                      BufferUsageHint.StaticDraw)

        ' Store VBO in vertex array
        GL.VertexAttribPointer(attributeID, coordCount, VertexAttribPointerType.Int, False, stride, offset)
    End Sub

    Private Sub storeDataInAttributeList(attributeID As Integer,
                                         coordCount As Integer,
                                         stride As Integer,
                                         offset As Integer,
                                         ByRef data() As Single)
        CreateVBO(BufferTarget.ArrayBuffer)

        ' Store data in OpenGL buffer
        GL.BufferData(BufferTarget.ArrayBuffer,
                      data.Length * Len(New Single),
                      data,
                      BufferUsageHint.StaticDraw)

        ' Store VBO in vertex array
        GL.VertexAttribPointer(attributeID, coordCount, VertexAttribPointerType.Float, False, stride, offset)
    End Sub
End Class
