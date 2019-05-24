Imports OpenTK
Imports OpenTK.Graphics.OpenGL4
Imports System.Threading

Public Class GodRaysComponent
    Private godRaysWidth As Integer
    Private godRaysHeight As Integer

    Private quadRenderer As ScreenQuadRenderer

    Private godRaysShader As Shader

    Private sun As SunManager

    Private godRaysFrameBufferComponent As GodRaysFrameBufferComponent

    Private camera As Camera

    Public Sub New(screenWidth As Integer,
                   screenHeight As Integer,
                   vertexShaderSrc As String,
                   fragmentShaderSrc As String,
                   ByRef sunmanager As SunManager,
                   ByRef cam As Camera,
                   ByRef screenQuadRenderer As ScreenQuadRenderer)
        godRaysWidth = screenWidth * 0.5
        godRaysHeight = screenHeight * 0.5
        quadRenderer = screenQuadRenderer
        sun = sunmanager
        camera = cam
        godRaysFrameBufferComponent = New GodRaysFrameBufferComponent(godRaysWidth, godRaysHeight)

        godRaysShader = New Shader(vertexShaderSrc, fragmentShaderSrc)
    End Sub

    Public Sub Render(occlusionTex As Integer)
        godRaysShader.Use()

        Dim sunPos = sun.position + camera.Position
        Dim sunScreenPos = camera.Project(sunPos)
        godRaysShader.SetVec2("lightPositionOnScreen", sunScreenPos)

        ' Send occlusion texture to shader
        godRaysShader.SetInt("occlusionTex", 0)
        GL.ActiveTexture(TextureUnit.Texture0)
        GL.BindTexture(TextureTarget.Texture2D, occlusionTex)

        godRaysFrameBufferComponent.Bind()
        GL.Clear(ClearBufferMask.ColorBufferBit)
        quadRenderer.Render()
        godRaysFrameBufferComponent.UnBind()
    End Sub

    Public ReadOnly Property CurrentFrame As Integer
        Get
            Return godRaysFrameBufferComponent.currentFrame
        End Get
    End Property
End Class
