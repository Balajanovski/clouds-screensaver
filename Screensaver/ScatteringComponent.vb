Imports OpenTK.Graphics.OpenGL4
Imports OpenTK

Public Class ScatteringComponent
    Private scatteringShader As Shader

    Private quadRenderer As ScreenQuadRenderer
    Private camera As Camera
    Private earth As EarthManager
    Private sun As SunManager

    Public Sub New(vertexSrc As String, fragSrc As String, ByRef quadRen As ScreenQuadRenderer, ByRef cam As Camera, ByRef earthManager As EarthManager, ByRef sunManager As SunManager)
        scatteringShader = New Shader(vertexSrc, fragSrc)
        quadRenderer = quadRen
        camera = cam
        earth = earthManager
        sun = sunManager
    End Sub

    Public Sub Render(time As Single, colorPreset As Preset)
        scatteringShader.Use()
        scatteringShader.SetFloat("time", time)
        scatteringShader.SetVec2("resolution", DisplayDevice.Default.Width, DisplayDevice.Default.Height)
        scatteringShader.SetMat4("inverseView", False, Matrix4.Invert(camera.ViewMatrix))
        scatteringShader.SetMat4("inverseProjection", False, Matrix4.Invert(camera.ProjectionMatrix))
        scatteringShader.SetVec3("sunColor", colorPreset.lightColor)
        scatteringShader.SetVec3("sunPos", sun.position)
        scatteringShader.SetVec3("skyColorTop", colorPreset.skyColorTop)
        scatteringShader.SetVec3("skyColorBottom", colorPreset.skyColorBottom)
        scatteringShader.SetFloat("EARTH_RADIUS", earth.radius)
        quadRenderer.Render()
    End Sub

    Protected Overrides Sub Finalize()
        scatteringShader.FreeResources()
    End Sub
End Class
