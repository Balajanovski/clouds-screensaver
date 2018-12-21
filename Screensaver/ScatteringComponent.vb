Imports OpenTK.Graphics.OpenGL4
Imports OpenTK

Public Class ScatteringComponent
    Private scatteringShader As Shader

    Private quadRenderer As ScreenQuadRenderer

    Public Sub New(vertexSrc As String, fragSrc As String, ByRef quadRen As ScreenQuadRenderer)
        scatteringShader = New Shader(vertexSrc, fragSrc)
        quadRenderer = quadRen
    End Sub

    Public Sub Render(time As Single)
        scatteringShader.Use()
        scatteringShader.SetFloat("time", time)
        quadRenderer.Render()
    End Sub

    Protected Overrides Sub Finalize()
        scatteringShader.FreeResources()
    End Sub
End Class
