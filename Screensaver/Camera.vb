Imports OpenTK

Public Class Camera
    ' Camera position
    Private pos As Vector3
    Public Property Position() As Vector3
        Get
            Return Position
        End Get
        Set(value As Vector3)
            pos = value

            ' Reset view matrix based on new value of position
            view = Matrix4.LookAt(pos, lookAt, New Vector3(0.0, 1.0, 0.0))
        End Set
    End Property

    ' Where camera is looking
    Private lookAt As Vector3
    Public Property LookAtTarget() As Vector3
        Get
            Return lookAt
        End Get
        Set(value As Vector3)
            lookAt = value

            view = Matrix4.LookAt(pos, lookAt, New Vector3(0.0, 1.0, 0.0))
        End Set
    End Property

    ' View matrix
    Private view As Matrix4
    Public ReadOnly Property ViewMatrix() As Matrix4
        Get
            Return view
        End Get
    End Property

    ' Field of View
    Private fieldOfView As Single
    Public ReadOnly Property FOV() As Single
        Get
            Return fieldOfView
        End Get
    End Property

    Public Sub New(newPos As Vector3, lookAtTarget As Vector3, Optional newFov As Single = 45.0F)
        lookAt = lookAtTarget
        pos = newPos
        fieldOfView = newFov

        view = Matrix4.LookAt(pos, lookAt, New Vector3(0.0, 1.0, 0.0))
    End Sub
End Class
