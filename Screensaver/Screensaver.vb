Imports OpenTK
Imports OpenTK.Graphics
Imports OpenTK.Graphics.OpenGL4
Imports OpenTK.Input

Imports System.Runtime.InteropServices
Imports System.Drawing

Public Class Screensaver
    Inherits GameWindow

    <DllImport("user32.dll")>
    Shared Function SetParent(hWndChild As IntPtr, hWndNewParent As IntPtr) As IntPtr
    End Function

    <DllImport("user32.dll")>
    Shared Function SetWindowLong(hWnd As IntPtr, nIndex As Integer, dwNewLong As IntPtr) As Integer
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Shared Function GetWindowLong(hWnd As IntPtr, nIndex As Integer) As Integer
    End Function

    <DllImport("user32.dll")>
    Shared Function GetClientRect(hWnd As IntPtr, ByRef lpRect As Rectangle) As Boolean
    End Function

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

    ' Rasterized randomly generated terrain
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

    Private screensaverPreviewMode As Boolean

    Public Sub New(w As Integer, h As Integer,
                   previewMode As Boolean,
                   previewWinHandle As IntPtr,
                   Optional gameWindowFlags As GameWindowFlags = GameWindowFlags.Default)

        MyBase.New(w, h,
                   GraphicsMode.Default,
                   "Screensaver",
                   gameWindowFlags,
                   DisplayDevice.Default, 4, 4,
                   GraphicsContextFlags.Default)
        VSync = VSyncMode.On
        time = 0
        screensaverPreviewMode = previewMode
        WindowBorder = WindowBorder.Hidden

        If gameWindowFlags = GameWindowFlags.Fullscreen Then
            CursorVisible = False
        End If

        If previewMode And previewWinHandle <> IntPtr.Zero Then
            PreparePreviewMode(previewWinHandle)
        End If
    End Sub

    Private Sub PreparePreviewMode(previewWinHandle As IntPtr)
        ' Set the preview window as the parent of this window
        SetParent(WindowInfo.Handle, previewWinHandle)

        ' Make this a child window so that it will close when the parent dialog closes
        ' GWL_STYLE = -16, WS_CHILD = 0x40000000
        SetWindowLong(WindowInfo.Handle, -16, New IntPtr(GetWindowLong(WindowInfo.Handle, -16) Or &H40000000))

        ' Place our window inside the parent
        Dim parentRectangle As Rectangle
        GetClientRect(previewWinHandle, parentRectangle)
        Width = parentRectangle.Width
        Height = parentRectangle.Height
        Size = parentRectangle.Size
        Location = New Point(0, 0)
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        GL.Enable(EnableCap.DepthTest)
        GL.ClearColor(0.0, 0.0, 0.0, 0.0)
        GL.Enable(EnableCap.Blend)
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha)

        earth = New EarthManager(600000.0)
        sun = New SunManager(Vector3d.Normalize(New Vector3(1.0, 1.0, 0.0)))

        Dim screenWidth As Integer = Width * ConfigManager.Instance.ResolutionRatio
        Dim screenHeight As Integer = Height * ConfigManager.Instance.ResolutionRatio

        screenQuadRenderer = New ScreenQuadRenderer(loader)
        camera = New Camera(New Vector3(0.0, earth.radius, 0.0),
                            New Vector3(1.0, earth.radius, 1.0),
                            screenWidth, screenHeight)
        scatteringComponent = New ScatteringComponent(screenWidth, screenHeight, "ScreenQuadRenderer.vert", "scattering.frag", screenQuadRenderer, camera, earth, sun)
        volumetricComponent = New VolumetricComponent(screenWidth, screenHeight, "ScreenQuadRenderer.vert", "volumetric.frag", screenQuadRenderer, camera, earth, sun)
        terrainComponent = New TerrainComponent(screenWidth, screenHeight, "Terrain.vert", "Terrain.frag", "DepthShader.vert", "DepthShader.frag", 2000, 2000, 100.0, 1.5, camera, earth, sun, screenQuadRenderer, loader)
        hdrComponent = New HDRComponent(Width, Height, "ScreenQuadRenderer.vert", "hdr.frag", 3.0, screenQuadRenderer)
        godRaysComponent = New GodRaysComponent(screenWidth, screenHeight, "ScreenQuadRenderer.vert", "GodRays.frag", sun, camera, screenQuadRenderer)
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        GL.Viewport(0, 0, Width, Height)
    End Sub

    Private mouseLoc As New Vector2()
    Dim mouseLocInitialized As Boolean = False
    Protected Overrides Sub OnUpdateFrame(e As FrameEventArgs)
        MyBase.OnUpdateFrame(e)

        Dim cursorState = Mouse.GetCursorState

        ' If mouse is moved, close screensaver
        If (Math.Abs(mouseLoc.X - cursorState.X) > 5 Or Math.Abs(mouseLoc.Y - cursorState.Y) > 5) _
                   And mouseLocInitialized And Not screensaverPreviewMode Then
            [Exit]()
        End If
        mouseLoc.X = cursorState.X
        mouseLoc.Y = cursorState.Y
        mouseLocInitialized = True

        ' If any key is pressed, close screensaver
        HandleKeyboard()
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
        terrainComponent.RenderShadowMap(colorConfiguration)

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

