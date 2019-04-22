Imports OpenTK
Imports OpenTK.Graphics.OpenGL4

Public Class Camera
    ' Camera position
    Private pos As Vector3
    Public Property Position() As Vector3
        Get
            Return pos
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

    ' Projection matrix
    Private projection As Matrix4
    Public ReadOnly Property ProjectionMatrix() As Matrix4
        Get
            Return projection
        End Get
    End Property

    ' Field of View
    Private fieldOfView As Single
    Public ReadOnly Property FOV() As Single
        Get
            Return fieldOfView
        End Get
    End Property

    ' Project world space coords into screen space
    Public Function Project(worldSpacePos As Vector3) As Vector2
        Dim v = New Vector4(worldSpacePos, 1.0)
        v = Vector4.Transform(v, view)
        v = Vector4.Transform(v, projection)

        ' Perspective division
        v /= v.Z

        ' Scale coords from range [-1, +1] to range [0, +1]
        v += New Vector4(1.0, 1.0, 0.0, 0.0)
        v *= 0.5

        Return New Vector2(v.X, v.Y)
    End Function

    Public Sub New(newPos As Vector3,
                   lookAtTarget As Vector3,
                   screenWidth As Single,
                   screenHeight As Single,
                   Optional newFov As Single = 60.0F)
        lookAt = lookAtTarget
        pos = newPos
        fieldOfView = newFov

        view = Matrix4.LookAt(pos, lookAt, New Vector3(0.0, 1.0, 0.0))
        projection = Matrix4.CreatePerspectiveFieldOfView(CType(Math.PI, Single) * (fieldOfView / 180.0),
                                                          screenWidth / screenHeight, 0.2, 256.0)
    End Sub
End Class
