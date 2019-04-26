Imports Random
Imports Math

Public Class RandomFloatGenerator
    Private Shared inst As New RandomFloatGenerator()
    Private Shared random As Random

    Public Shared Function instance() As RandomFloatGenerator
        Return inst
    End Function

    Public Function NextFloat() As Single
        Dim mantissa As Single = (random.NextDouble() * 2.0) - 1.0

        Return mantissa
    End Function

    Private Sub New()
        random = New Random()
    End Sub
End Class
