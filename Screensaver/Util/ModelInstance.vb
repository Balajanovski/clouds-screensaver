Imports OpenTK

' An instance of a model with its own position, shape and other properties
' Creates so objects with the same model can share the model and just alter parameters for memory efficiency
Public Class ModelInstance
    Private model As Model

    Private modelMatrix As Matrix4
    Private position As Vector3

    Public ReadOnly Property ModelPosition As Vector3
        Get
            Return position
        End Get
    End Property

    Public Sub New(ByRef pos As Vector3,
                   scaleFactor As Single,
                   ByRef instanceModel As Model)
        model = instanceModel
        position = pos

        Dim translationMatrix = New Matrix4(1, 0, 0, 0,
                                  0, 1, 0, 0,
                                  0, 0, 1, 0,
                                  pos.X, pos.Y, pos.Z, 1)

        modelMatrix = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(-90.0))
        Dim scaledMatrix = Matrix4.CreateScale(scaleFactor)

        modelMatrix *= scaledMatrix
        modelMatrix *= translationMatrix
    End Sub

    Public Sub Draw(ByRef shader As Shader)
        shader.SetMat4("model", False, modelMatrix)
        model.Draw(shader)
    End Sub

    Public Sub Draw(ByRef shader As Shader, consistentModelMatrix As Matrix4)
        shader.SetMat4("model", False, consistentModelMatrix)
        model.Draw(shader)
    End Sub
End Class
