Imports System
Imports OpenTK
Imports OpenTK.Graphics
Imports OpenTK.Graphics.OpenGL4
Imports OpenTK.Input

Public Class Screensaver
    Inherits GameWindow

    ' Used by entirely shader dependent components
    ' Draws a screen covering quad
    Private screenQuadRenderer As ScreenQuadRenderer

    ' Atmospheric scattering
    Private scatteringComponent As ScatteringComponent

    ' Volumetric cloud raymarching
    Private volumetricComponent As VolumetricComponent

    ' High Dynamic Range
    Private hdrComponent As HDRComponent

    Private time As Double

    Public Sub New()
        MyBase.New(DisplayDevice.Default.Width, DisplayDevice.Default.Height,
                   GraphicsMode.Default,
                   "Screensaver",
                   GameWindowFlags.Default,
                   DisplayDevice.Default, 4, 4,
                   GraphicsContextFlags.Debug)
        VSync = VSyncMode.On
        prevMouseX = 0
        prevMouseY = 0
        time = 0
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        GL.Enable(EnableCap.DepthTest)
        GL.ClearColor(0.0, 0.0, 0.0, 1.0)

        screenQuadRenderer = New ScreenQuadRenderer()
        scatteringComponent = New ScatteringComponent("scattering.vert", "scattering.frag", screenQuadRenderer)
        hdrComponent = New HDRComponent("hdr.vert", "hdr.frag", -1.0, screenQuadRenderer)
        volumetricComponent = New VolumetricComponent("volumetric.vert", "volumetric.frag", screenQuadRenderer)

    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        GL.Viewport(0, 0, Width, Height)
    End Sub

    Private prevMouseX As Integer
    Private prevMouseY As Integer
    Protected Overrides Sub OnUpdateFrame(e As FrameEventArgs)
        MyBase.OnUpdateFrame(e)

        Dim cursorState = Mouse.GetCursorState

        ' If mouse is moved, close screensaver
        'If prevMouseX <> cursorState.X Or prevMouseY <> cursorState.Y _
        '       And prevMouseX <> 0 Or prevMouseY <> 0 Then
        '   [Exit]()
        'End If
        prevMouseX = cursorState.X
        prevMouseY = cursorState.Y

        ' If any key is pressed, close screensaver
        'HandleKeyboard()
    End Sub

    Private Sub HandleKeyboard()
        Dim keyState = Keyboard.GetState()

        If keyState.IsAnyKeyDown Then
            [Exit]()
        End If
    End Sub

    Protected Overrides Sub OnRenderFrame(e As FrameEventArgs)
        MyBase.OnRenderFrame(e)

        time += e.Time

        hdrComponent.Bind()
        GL.Clear(ClearBufferMask.ColorBufferBit Or ClearBufferMask.DepthBufferBit)
        GL.Enable(EnableCap.Blend)
        'scatteringComponent.Render(time)
        volumetricComponent.Render(time)
        GL.Disable(EnableCap.Blend)
        hdrComponent.UnBind()

        GL.Clear(ClearBufferMask.ColorBufferBit)
        hdrComponent.Render()

        SwapBuffers()
    End Sub

End Class

