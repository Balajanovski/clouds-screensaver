Imports OpenTK.Graphics.OpenGL4

Public MustInherit Class FrameBufferComponentBase
    Protected resWidth As Integer
    Protected resHeight As Integer

    Private prevViewportDimensions(4) As Integer

    Protected framebuffer As Integer

    Public Sub Bind()
        GL.GetInteger(GetPName.Viewport, prevViewportDimensions)

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer)
        GL.Viewport(0, 0, resWidth, resHeight)
        GL.Enable(EnableCap.DepthTest)
    End Sub

    Public Sub UnBind()
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0)
        GL.Viewport(prevViewportDimensions(0), prevViewportDimensions(1), prevViewportDimensions(2), prevViewportDimensions(3))
        GL.Disable(EnableCap.DepthTest)
    End Sub
End Class
