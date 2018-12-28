Imports OpenTK.Graphics.OpenGL4
Imports System.Reflection
Imports System.IO

Public MustInherit Class ShaderBase
    Private ID As Integer
    Protected Property shaderID() As Integer
        Get
            Return ID
        End Get
        Set(value As Integer)
            ID = value
        End Set
    End Property

    Protected tempDir As String

    Public Sub Use()
        GL.UseProgram(shaderID)
    End Sub

    ' Exports shaders from exe into temp folder so they can be compiled
    Protected Sub ExportResource(ByRef assembly As Assembly, ByVal assemblyNamespace As String, ByVal mediaFile As String)
        Dim fullFileName As String = assemblyNamespace + "." + mediaFile
        Dim tmpFile = Path.Combine(Me.tempDir, fullFileName)

        Using input As Stream = assembly.GetManifestResourceStream(fullFileName)
            Using fileStream As Stream = File.OpenWrite(tmpFile)
                input.CopyTo(fileStream)
            End Using
        End Using
    End Sub

    Protected Shared Function CreateTempDirectory(ByVal Optional prefix As String = "") As String
        While True
            Dim folder As String = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString())
            If Not Directory.Exists(folder) Then
                Directory.CreateDirectory(folder)
                Return folder
            End If
        End While
    End Function

    Public Sub FreeResources()
        Dim files() As String = Directory.GetFiles(tempDir)
        For Each fileSrc In files
            File.Delete(fileSrc)
        Next

        While Directory.GetFiles(tempDir).Count > 0
            ' Loop till files are deleted
            ' Added for thread safety as file deletion is asynchronous
        End While
        Directory.Delete(tempDir)
    End Sub

    Protected Shared Sub CheckShaderCompilationSuccess(shader As Integer)
        Dim success As Integer
        GL.GetShader(shader, ShaderParameter.CompileStatus, success)
        If success <> All.True Then
            Dim infoLog = GL.GetShaderInfoLog(shader)
            Throw New Exception(infoLog)
        End If
    End Sub

    Protected Sub CheckProgramLinkingSuccess()
        Dim success As Integer
        GL.GetProgram(shaderID, GetProgramParameterName.LinkStatus, success)
        If success <> All.True Then
            Dim infoLog = GL.GetProgramInfoLog(shaderID)
            Throw New Exception(infoLog)
        End If
    End Sub
End Class
