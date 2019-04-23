Imports OpenTK
Imports OpenTK.Graphics
Imports OpenTK.Graphics.OpenGL4
Imports OpenTK.Input

Public Class Screensaver
    Inherits GameWindow

    ' Loader utility
    ' Wraps verbose OpenGL commands in nice methods
    Private loader As New Loader()

    ' Used by entirely shader dependent components
    ' Draws a screen covering quad
    Private screenQuadRenderer As ScreenQuadRenderer

    ' Atmospheric scattering
    Private scatteringComponent As ScatteringComponent

    ' Volumetric cloud raymarching
    Private volumetricComponent As VolumetricComponent

    ' Rasteurized randomly generated terrain
    Private terrainComponent As TerrainComponent

    ' God / crepuscular rays
    Private godRaysComponent As GodRaysComponent

    ' High Dynamic Range
    Private hdrComponent As HDRComponent

    Private postProcessClouds As Shader

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
        GL.ClearColor(0.0, 0.0, 0.0, 0.0)
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha)

        earth = New EarthManager(600000.0)
        sun = New SunManager(Vector3d.Normalize(New Vector3(1.0, 1.0, 0.0)))

        screenQuadRenderer = New ScreenQuadRenderer(loader)
        camera = New Camera(New Vector3(0.0, earth.radius, 0.0),
                            New Vector3(1.0, earth.radius, 1.0),
                            DisplayDevice.Default.Width, DisplayDevice.Default.Height)
        scatteringComponent = New ScatteringComponent("ScreenQuadRenderer.vert", "scattering.frag", screenQuadRenderer, camera, earth, sun)
        volumetricComponent = New VolumetricComponent("ScreenQuadRenderer.vert", "volumetric.frag", screenQuadRenderer, camera, earth, sun)
        terrainComponent = New TerrainComponent("Terrain.vert", "Terrain.frag", "DepthShader.vert", "DepthShader.frag", 2000, 2000, 100.0, 1.5, camera, earth, sun, screenQuadRenderer, loader)
        hdrComponent = New HDRComponent("ScreenQuadRenderer.vert", "hdr.frag", 3.0, screenQuadRenderer)
        godRaysComponent = New GodRaysComponent("ScreenQuadRenderer.vert", "GodRays.frag", sun, camera, screenQuadRenderer)
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

    Private Shared Function Sigmoid(v As Single)
        Return 1 / (1.0 + Math.Exp(8.0 - v * 40.0))
    End Function

    Protected Overrides Sub OnRenderFrame(e As FrameEventArgs)
        MyBase.OnRenderFrame(e)

        time += e.Time

        Dim slowedTime As Single = time / 58
        Dim sunYPos As Single = Math.Abs(Math.Sin(2 * slowedTime))
        sun.position = New Vector3(Math.Sin(slowedTime Mod (Math.PI / 2)), sunYPos, Math.Cos(slowedTime Mod (Math.PI / 2)))

        Dim colorConfiguration As Preset = Presets.MixPresets(Sigmoid(-sun.lightDir.Y),
                                                              Presets.DefaultPreset,
                                                              Presets.SunsetPreset)

        ' Shadows not rendered if sun is too low in sky
        If sun.position.Y > 0.08 Then
            terrainComponent.RenderShadowMap(colorConfiguration)
        End If

        scatteringComponent.Render(time, colorConfiguration)
        terrainComponent.Render(colorConfiguration)
        volumetricComponent.Render(time, terrainComponent.OcclusionTexture,
                                   scatteringComponent.CurrentFrame, colorConfiguration)
        godRaysComponent.Render(volumetricComponent.OcclusionTexture)

        hdrComponent.Bind()
        GL.Clear(ClearBufferMask.ColorBufferBit Or ClearBufferMask.DepthBufferBit)
        GL.Enable(EnableCap.Blend)
        GL.Disable(EnableCap.DepthTest)
        volumetricComponent.Blit()
        terrainComponent.Blit()
        GL.Disable(EnableCap.Blend)
        GL.Enable(EnableCap.DepthTest)
        hdrComponent.UnBind()

        GL.Clear(ClearBufferMask.ColorBufferBit)
        hdrComponent.Render(godRaysComponent.CurrentFrame)

        SwapBuffers()
    End Sub

End Class

