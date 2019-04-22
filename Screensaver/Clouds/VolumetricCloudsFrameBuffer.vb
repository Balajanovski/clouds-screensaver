Imports OpenTK
Imports OpenTK.Graphics.OpenGL4

Public Class VolumetricCloudsFrameBuffer
    Inherits FrameBufferComponentBase

    Private currentFrameTex As Integer
    Private lastFrameTex As Integer
    Private alphanessTex As Integer

    Public Sub New(resolutionWidth As Integer, resolutionHeight As Integer)
        resWidth = resolutionWidth
        resHeight = resolutionHeight

        ' Configure Volumetric Clouds Buffer
        ' -------------------------
        framebuffer = GL.GenFramebuffer()

        ' Create color buffers
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer)
        currentFrameTex = GL.GenTexture()
        GL.ActiveTexture(TextureUnit.Texture0)
        GL.BindTexture(TextureTarget.Texture2D, currentFrameTex)
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, resolutionWidth,
                      resolutionHeight, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, All.Nearest)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, All.Nearest)
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                                TextureTarget.Texture2D, currentFrameTex, 0)


        lastFrameTex = GL.GenTexture()
        GL.ActiveTexture(TextureUnit.Texture1)
        GL.BindTexture(TextureTarget.Texture2D, lastFrameTex)
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, resolutionWidth,
                      resolutionHeight, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, All.Nearest)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, All.Nearest)
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1,
                                TextureTarget.Texture2D, lastFrameTex, 0)

        alphanessTex = GL.GenTexture()
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1)
        GL.ActiveTexture(TextureUnit.Texture2)
        GL.BindTexture(TextureTarget.Texture2D, alphanessTex)
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, resolutionWidth,
                      resolutionHeight, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, All.Nearest)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, All.Nearest)
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2,
                                TextureTarget.Texture2D, alphanessTex, 0)

        Dim attachments() As Integer = {FramebufferAttachment.ColorAttachment0,
                                        FramebufferAttachment.ColorAttachment1,
                                        FramebufferAttachment.ColorAttachment2}
        GL.DrawBuffers(attachments.Length, attachments)
    End Sub

    Public ReadOnly Property lastFrame As Integer
        Get
            Return lastFrameTex
        End Get
    End Property

    Public ReadOnly Property currentFrame As Integer
        Get
            Return currentFrameTex
        End Get
    End Property

    Public ReadOnly Property lastFrameAlphaness As Integer
        Get
            Return alphanessTex
        End Get
    End Property
End Class
