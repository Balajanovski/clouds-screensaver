Imports OpenTK
Imports OpenTK.Graphics.OpenGL4

Public Class ScatteringFrameBufferComponent
    Inherits FrameBufferComponentBase

    Private currentFrameTex As Integer

    Public Sub New(resolutionWidth As Integer, resolutionHeight As Integer)
        resWidth = resolutionWidth
        resHeight = resolutionHeight

        ' Configure God Rays Buffer
        ' -------------------------
        framebuffer = GL.GenFramebuffer()

        ' Create color buffers
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer)
        currentFrameTex = GL.GenTexture()
        GL.ActiveTexture(TextureUnit.Texture0)
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1)
        GL.PixelStore(PixelStoreParameter.PackAlignment, 1)
        GL.BindTexture(TextureTarget.Texture2D, currentFrameTex)
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, resolutionWidth,
                      resolutionHeight, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, All.Repeat)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, All.Repeat)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, All.Nearest)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, All.Nearest)
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                                TextureTarget.Texture2D, currentFrameTex, 0)

        Dim attachments() As Integer = {FramebufferAttachment.ColorAttachment0}
        GL.DrawBuffers(attachments.Length, attachments)
    End Sub

    Public ReadOnly Property currentFrame As Integer
        Get
            Return currentFrameTex
        End Get
    End Property
End Class
