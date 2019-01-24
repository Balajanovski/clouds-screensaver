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

    Private sunDir As Vector3

    Public Property lightDir As Vector3
        Get
            Return sunDir
        End Get
        Set(value As Vector3)
            sunDir = value
        End Set
    End Property

    Public Sub New(col As Vector3, lightDirection As Vector3)
        sunColor = col
        sunDir = lightDirection
    End Sub
End Class
