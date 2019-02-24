Imports System.IO
Imports System.Runtime.CompilerServices

Public Module StreamExtensions
    <Extension()>
    Public Function ReadAllBytes(inStream As Stream) As Byte()
        If TypeOf inStream Is MemoryStream Then
            Return CType(inStream, MemoryStream).ToArray()
        End If

        Using memoryStream = New MemoryStream()
            inStream.CopyTo(memoryStream)
            Return memoryStream.ToArray()
        End Using
    End Function
End Module
