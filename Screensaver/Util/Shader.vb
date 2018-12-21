Imports OpenTK.Graphics.OpenGL4
Imports OpenTK
Imports System.Reflection
Imports System.IO

Public Class Shader
    Private shaderID As Integer

    Private vertexShader As Integer
    Private fragmentShader As Integer

    Private tempDir As String

    Public Sub New(vertexSrc As String, fragSrc As String)
        tempDir = CreateTempDirectory("temp-")

        ' Compile vertex shader
        ExportResource(Assembly.GetExecutingAssembly, "Screensaver", vertexSrc)
        vertexShader = GL.CreateShader(ShaderType.VertexShader)
        GL.ShaderSource(vertexShader, File.ReadAllText(Path.Combine(tempDir, "Screensaver." & vertexSrc)))
        GL.CompileShader(vertexShader)

        ' Check success
        Dim success As Integer
        GL.GetShader(vertexShader, ShaderParameter.CompileStatus, success)
        If success <> All.True Then
            Dim infoLog = GL.GetShaderInfoLog(vertexShader)
            Throw New Exception(infoLog)
        End If

        ' Compile frag shader
        ExportResource(Assembly.GetExecutingAssembly, "Screensaver", fragSrc)
        fragmentShader = GL.CreateShader(ShaderType.FragmentShader)
        GL.ShaderSource(fragmentShader, File.ReadAllText(Path.Combine(tempDir, "Screensaver." & fragSrc)))
        GL.CompileShader(fragmentShader)

        ' Check success
        GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, success)
        If success <> All.True Then
            Dim infoLog = GL.GetShaderInfoLog(fragmentShader)
            Throw New Exception(infoLog)
        End If

        ' Link shaders into shader program
        shaderID = GL.CreateProgram()
        GL.AttachShader(shaderID, vertexShader)
        GL.AttachShader(shaderID, fragmentShader)
        GL.LinkProgram(shaderID)

        ' Check success
        GL.GetProgram(shaderID, GetProgramParameterName.LinkStatus, success)
        If success <> All.True Then
            Dim infoLog = GL.GetProgramInfoLog(shaderID)
            Throw New Exception(infoLog)
        End If

        ' Cleanup
        GL.DetachShader(shaderID, vertexShader)
        GL.DetachShader(shaderID, fragmentShader)
        GL.DeleteShader(vertexShader)
        GL.DeleteShader(fragmentShader)
    End Sub

    Public Sub Use()
        GL.UseProgram(shaderID)
    End Sub

    Public Sub SetInt(name As String, value As Integer)
        GL.Uniform1(GL.GetUniformLocation(shaderID, name), value)
    End Sub

    Public Sub SetFloat(name As String, value As Single)
        GL.Uniform1(GL.GetUniformLocation(shaderID, name), value)
    End Sub

    ' Setting vectors in OpenTK is bugged, so this hack must be used
    Public Sub SetVec2(name As String, x As Single, y As Single)
        GL.Uniform2(GL.GetUniformLocation(shaderID, name), x, y)
    End Sub

    ' Setting vectors in OpenTK is bugged, so this hack must be used
    Public Sub SetVec3(name As String, x As Single, y As Single, z As Single)
        GL.Uniform3(GL.GetUniformLocation(shaderID, name), x, y, z)
    End Sub

    ' Exports shaders from exe into temp folder so they can be compiled
    Private Sub ExportResource(ByRef assembly As Assembly, ByVal assemblyNamespace As String, ByVal mediaFile As String)
        Dim fullFileName As String = assemblyNamespace + "." + mediaFile
        Dim tmpFile = Path.Combine(Me.tempDir, fullFileName)

        Using input As Stream = assembly.GetManifestResourceStream(fullFileName)
            Using fileStream As Stream = File.OpenWrite(tmpFile)
                input.CopyTo(fileStream)
            End Using
        End Using
    End Sub

    Private Shared Function CreateTempDirectory(ByVal Optional prefix As String = "") As String
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
End Class
