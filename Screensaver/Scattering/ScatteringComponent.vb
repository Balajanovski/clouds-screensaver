Imports OpenTK.Graphics.OpenGL4
Imports OpenTK

' Atmospheric scattering
Public Class ScatteringComponent
    Private scatteringShader As Shader

    Private scatteringFrameBufferComponent As ScatteringFrameBufferComponent

    Private textureBlitterShader As Shader

    Private quadRenderer As ScreenQuadRenderer
    Private camera As Camera
    Private earth As EarthManager
    Private sun As SunManager

    Private scatteringResWidth As Integer
    Private scatteringResHeight As Integer

    Public Sub New(screenWidth As Integer, screenHeight As Integer, vertexSrc As String, fragSrc As String, ByRef quadRen As ScreenQuadRenderer, ByRef cam As Camera, ByRef earthManager As EarthManager, ByRef sunManager As SunManager)
        quadRenderer = quadRen
        camera = cam
        earth = earthManager
        sun = sunManager
        scatteringResWidth = screenWidth
        scatteringResHeight = screenHeight

        scatteringShader = New Shader(vertexSrc, fragSrc)
        scatteringFrameBufferComponent = New ScatteringFrameBufferComponent(scatteringResWidth, scatteringResHeight)
    End Sub

    Public Sub Render(time As Single, colorPreset As Preset)
        scatteringShader.Use()

        ' Set uniforms
        scatteringShader.SetFloat("time", time)
        scatteringShader.SetVec2("resolution", scatteringResWidth, scatteringResHeight)
        scatteringShader.SetMat4("inverseView", False, Matrix4.Invert(camera.ViewMatrix))
        scatteringShader.SetMat4("inverseProjection", False, Matrix4.Invert(camera.ProjectionMatrix))
        scatteringShader.SetVec3("sunColor", colorPreset.lightColor)
        scatteringShader.SetVec3("sunPos", sun.position)
        scatteringShader.SetVec3("skyColorTop", colorPreset.skyColorTop)
        scatteringShader.SetVec3("skyColorBottom", colorPreset.skyColorBottom)
        scatteringShader.SetFloat("EARTH_RADIUS", earth.radius)

        ' Draw
        scatteringFrameBufferComponent.Bind()
        quadRenderer.Render()
        scatteringFrameBufferComponent.UnBind()
    End Sub

    Public ReadOnly Property CurrentFrame As Integer
        Get
            Return scatteringFrameBufferComponent.currentFrame
        End Get
    End Property
End Class
