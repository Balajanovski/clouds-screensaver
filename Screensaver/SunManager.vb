Imports OpenTK

Public Class SunManager
    Private sunColor As Vector3

    Public Property color As Vector3
        Get
            Return sunColor
        End Get
        Set(value As Vector3)
            sunColor = value
        End Set
    End Property

    Public ReadOnly Property lightDir As Vector3
        Get
            Return Vector3d.Normalize(New Vector3(0, 0, 0) - sunPos)
        End Get
    End Property

    Private sunPos As Vector3
    Public Property position As Vector3
        Get
            Return sunPos
        End Get
        Set(value As Vector3)
            sunPos = value
        End Set
    End Property

    Public Sub New(col As Vector3, pos As Vector3)
        sunColor = col
        sunPos = pos
    End Sub
End Class
