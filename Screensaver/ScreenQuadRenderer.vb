Imports OpenTK.Graphics.OpenGL4
Imports System.Runtime.InteropServices

' Renders a quad which covers the whole screen
' Used in shader dependent components
Public Class ScreenQuadRenderer
    Private vao As Integer
    Private quadVbo As Integer

    Public Sub New()
        ' Generate vao & vbo
        GL.GenVertexArrays(1, vao)
        GL.GenBuffers(1, quadVbo)

        ' Bind vao to store vertex attrib calls
        GL.BindVertexArray(vao)

        ' Buffer data for rectangle which covers entire screen
        GL.BindBuffer(BufferTarget.ArrayBuffer, quadVbo)
        Dim vertices As Single() = {
            -1.0F, 1.0F, 0.0F, 1.0F,
            1.0F, 1.0F, 1.0F, 1.0F,
            1.0F, -1.0F, 1.0F, 0.0F,
            1.0F, -1.0F, 1.0F, 0.0F,
            -1.0F, -1.0F, 0.0F, 0.0F,
            -1.0F, 1.0F, 0.0F, 1.0F
        }
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * Len(New Single),
                      vertices, BufferUsageHint.StaticDraw)

        ' Set vertex attribute for position, so vertex shader may interpret data sent
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, False, 4 * Len(New Single), IntPtr.Zero)
        GL.EnableVertexAttribArray(0)

        ' Set vertex attribute for texcoords
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, False, 4 * Len(New Single), 2 * Len(New Single))
        GL.EnableVertexAttribArray(1)

        ' Unbind vao & vbo
        GL.BindVertexArray(0)
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0)
    End Sub

    Public Sub Render()
        GL.BindVertexArray(vao)

        GL.BindBuffer(BufferTarget.ArrayBuffer, quadVbo)
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6)

        GL.BindVertexArray(0)
    End Sub
End Class
