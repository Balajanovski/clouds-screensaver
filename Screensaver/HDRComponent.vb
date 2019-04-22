Imports OpenTK.Graphics.OpenGL4
Imports OpenTK

Public Class HDRComponent
    Private hdrFBO As Integer
    Private rboDepth As Integer

    Private colorBuffer As Integer

    Private exposure As Single

    Private hdrShader As Shader

    Private quadRenderer As ScreenQuadRenderer

    Public Sub New(vertSrc As String, fragSrc As String, nExposure As Single,
                   ByRef quadRen As ScreenQuadRenderer)
        exposure = nExposure
        quadRenderer = quadRen
        hdrShader = New Shader(vertSrc, fragSrc)

        ' Configure HDR framebuffer
        ' -------------------------
        hdrFBO = GL.GenFramebuffer()
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, hdrFBO)
        ' Create floating point color buffer
        colorBuffer = GL.GenTexture()
        GL.BindTexture(TextureTarget.Texture2D, colorBuffer)
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, DisplayDevice.Default.Width,
                      DisplayDevice.Default.Height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, All.Linear)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, All.Linear)
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                                TextureTarget.Texture2D, colorBuffer, 0)
        Dim attachments() As Integer = {FramebufferAttachment.ColorAttachment0}
        GL.DrawBuffers(attachments.Length, attachments)

        ' Create depth buffer
        rboDepth = GL.GenRenderbuffer()
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rboDepth)
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent,
                               DisplayDevice.Default.Width, DisplayDevice.Default.Height)
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                                   RenderbufferTarget.Renderbuffer, rboDepth)


        If GL.CheckFramebufferStatus(All.Framebuffer) <> All.FramebufferComplete Then
            Throw New System.Exception("error: Framebuffer not complete!")
        End If
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0)

        hdrShader.Use()
    End Sub

    Public Sub Bind()
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, hdrFBO)
        GL.Viewport(0, 0, DisplayDevice.Default.Width, DisplayDevice.Default.Height)
        GL.Enable(EnableCap.DepthTest)
    End Sub

    Public Sub UnBind()
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0)
        GL.Viewport(0, 0, DisplayDevice.Default.Width, DisplayDevice.Default.Height)
        GL.Disable(EnableCap.DepthTest)
    End Sub

    Public Sub Render(godRaysTex As Integer)
        GL.Clear(ClearBufferMask.ColorBufferBit Or ClearBufferMask.DepthBufferBit)
        hdrShader.Use()

        ' Set uniforms
        hdrShader.SetInt("hdrBuffer", 0)
        hdrShader.SetInt("godRaysTex", 1)
        hdrShader.SetFloat("exposure", exposure)

        GL.ActiveTexture(TextureUnit.Texture0)
        GL.BindTexture(TextureTarget.Texture2D, colorBuffer)
        GL.ActiveTexture(TextureUnit.Texture1)
        GL.BindTexture(TextureTarget.Texture2D, godRaysTex)
        quadRenderer.Render()
    End Sub

    Protected Overrides Sub Finalize()
        hdrShader.FreeResources()
    End Sub

End Class
