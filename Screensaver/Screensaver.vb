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

    Private camera As Camera

    Private sun As SunManager
    Private earth As EarthManager

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
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha)

        earth = New EarthManager(700000.0)
        sun = New SunManager(New Vector3(1.0, 1.0, 1.0), Vector3d.Normalize(New Vector3(1.0, 1.0, 0.0)))

        screenQuadRenderer = New ScreenQuadRenderer()
        camera = New Camera(New Vector3(0.0, earth.radius, 0.0),
                            New Vector3(500.0, earth.radius, 500.0),
                            DisplayDevice.Default.Width, DisplayDevice.Default.Height)
        scatteringComponent = New ScatteringComponent("ScreenQuadRenderer.vert", "scattering.frag", screenQuadRenderer, camera, earth, sun)
        volumetricComponent = New VolumetricComponent("ScreenQuadRenderer.vert", "volumetric.frag", screenQuadRenderer, camera, earth, sun)
        hdrComponent = New HDRComponent("ScreenQuadRenderer.vert", "hdr.frag", -0.8, screenQuadRenderer)
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

    Private Shared Function Clamp(value As Single) As Single
        If (value > 1.0) Then
            Return 1.0
        ElseIf (value < 0.0) Then
            Return 0.0
        Else
            Return value
        End If
    End Function

    Protected Overrides Sub OnRenderFrame(e As FrameEventArgs)
        MyBase.OnRenderFrame(e)

        time += e.Time

        sun.position = New Vector3(Math.Abs(Math.Sin(time)), Math.Sin(2 * time), Math.Abs(Math.Cos(time)))

        hdrComponent.Bind()
        GL.Clear(ClearBufferMask.ColorBufferBit Or ClearBufferMask.DepthBufferBit)
        GL.Enable(EnableCap.Blend)
        GL.Disable(EnableCap.DepthTest)
        scatteringComponent.Render(time)
        volumetricComponent.Render(time)
        GL.Disable(EnableCap.Blend)
        GL.Enable(EnableCap.DepthTest)
        hdrComponent.UnBind()

        GL.Clear(ClearBufferMask.ColorBufferBit)
        hdrComponent.Render()

        SwapBuffers()
    End Sub

End Class

