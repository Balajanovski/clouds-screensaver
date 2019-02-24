Public Class RawModel
    Private vertexArrayObject As Integer
    Private vertexCount As Integer

    Public Sub New(vao As Integer, vertCount As Integer)
        vertexArrayObject = vao
        vertexCount = vertCount
    End Sub

    Public ReadOnly Property VAO As Integer
        Get
            Return vertexArrayObject
        End Get
    End Property

    Public ReadOnly Property NumVertices As Integer
        Get
            Return vertexCount
        End Get
    End Property
End Class
