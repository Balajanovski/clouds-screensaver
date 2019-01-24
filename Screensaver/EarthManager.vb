Public Class EarthManager
    Private earthRadius As Single

    Public Property radius As Single
        Get
            Return earthRadius
        End Get
        Set(value As Single)
            earthRadius = value
        End Set
    End Property

    Public Sub New(rad As Single)
        earthRadius = rad
    End Sub
End Class
