Imports OpenTK.Graphics.OpenGL4
Imports OpenTK

Public Class ScatteringComponent
    Private scatteringShader As Shader

    Private quadRenderer As ScreenQuadRenderer
    Private camera As Camera

    Public Sub New(vertexSrc As String, fragSrc As String, ByRef quadRen As ScreenQuadRenderer, ByRef cam As Camera)
        scatteringShader = New Shader(vertexSrc, fragSrc)
        quadRenderer = quadRen
        camera = cam
    End Sub

    Public Sub Render(time As Single)
        scatteringShader.Use()
        scatteringShader.SetFloat("time", time)
        'scatteringShader.SetMat4("view", False, camera.ViewMatrix)
        'scatteringShader.SetVec3("cameraPos", camera.Position)
        quadRenderer.Render()
    End Sub

    Protected Overrides Sub Finalize()
        scatteringShader.FreeResources()
    End Sub
End Class
