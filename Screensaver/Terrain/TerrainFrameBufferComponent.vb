Imports OpenTK
Imports OpenTK.Graphics.OpenGL4

Public Class TerrainFrameBufferComponent
    Private terrainFrameBuffer As Integer
    Private currentFrameTex As Integer
    Private occlusionTex As Integer

    Private resWidth As Integer
    Private resHeight As Integer

    Public Sub New(resolutionWidth As Integer, resolutionHeight As Integer)
        resWidth = resolutionWidth
        resHeight = resolutionHeight

        ' Configure Volumetric Clouds Buffer
        ' -------------------------
        terrainFrameBuffer = GL.GenFramebuffer()

        ' Create color buffers
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, terrainFrameBuffer)
        currentFrameTex = GL.GenTexture()
        GL.ActiveTexture(TextureUnit.Texture0)
        GL.BindTexture(TextureTarget.Texture2D, currentFrameTex)
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, resolutionWidth,
                      resolutionHeight, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, All.Nearest)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, All.Nearest)
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                                TextureTarget.Texture2D, currentFrameTex, 0)

        occlusionTex = GL.GenTexture()
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1)
        GL.ActiveTexture(TextureUnit.Texture2)
        GL.BindTexture(TextureTarget.Texture2D, occlusionTex)
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, resolutionWidth,
                      resolutionHeight, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, All.Nearest)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, All.Nearest)
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1,
                                TextureTarget.Texture2D, occlusionTex, 0)

        Dim attachments() As Integer = {FramebufferAttachment.ColorAttachment0,
                                        FramebufferAttachment.ColorAttachment1}
        GL.DrawBuffers(attachments.Length, attachments)
    End Sub

    Public Sub Bind()
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, terrainFrameBuffer)
        GL.Viewport(0, 0, resWidth, resHeight)
        GL.Enable(EnableCap.DepthTest)
    End Sub

    Public Sub UnBind()
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0)
        GL.Viewport(0, 0, DisplayDevice.Default.Width, DisplayDevice.Default.Height)
        GL.Disable(EnableCap.DepthTest)
    End Sub

    Public ReadOnly Property currentFrame As Integer
        Get
            Return currentFrameTex
        End Get
    End Property

    Public ReadOnly Property occlusionBuffer As Integer
        Get
            Return occlusionTex
        End Get
    End Property
End Class
