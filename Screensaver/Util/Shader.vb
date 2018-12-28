Imports OpenTK.Graphics.OpenGL4
Imports OpenTK
Imports System.Reflection
Imports System.IO

Public Class Shader
    Inherits ShaderBase

    Private vertexShader As Integer
    Private fragmentShader As Integer

    Public Sub New(vertexSrc As String, fragSrc As String)
        tempDir = CreateTempDirectory("temp-")

        ' Compile vertex shader
        ExportResource(Assembly.GetExecutingAssembly, "Screensaver", vertexSrc)
        vertexShader = GL.CreateShader(ShaderType.VertexShader)
        GL.ShaderSource(vertexShader, File.ReadAllText(Path.Combine(tempDir, "Screensaver." & vertexSrc)))
        GL.CompileShader(vertexShader)
        CheckShaderCompilationSuccess(vertexShader)

        ' Compile frag shader
        ExportResource(Assembly.GetExecutingAssembly, "Screensaver", fragSrc)
        fragmentShader = GL.CreateShader(ShaderType.FragmentShader)
        GL.ShaderSource(fragmentShader, File.ReadAllText(Path.Combine(tempDir, "Screensaver." & fragSrc)))
        GL.CompileShader(fragmentShader)
        CheckShaderCompilationSuccess(fragmentShader)

        ' Link shaders into shader program
        shaderID = GL.CreateProgram()
        GL.AttachShader(shaderID, vertexShader)
        GL.AttachShader(shaderID, fragmentShader)
        GL.LinkProgram(shaderID)
        CheckProgramLinkingSuccess()

        ' Cleanup
        GL.DetachShader(shaderID, vertexShader)
        GL.DetachShader(shaderID, fragmentShader)
        GL.DeleteShader(vertexShader)
        GL.DeleteShader(fragmentShader)
    End Sub

    Public Sub SetInt(name As String, value As Integer)
        GL.Uniform1(GL.GetUniformLocation(shaderID, name), value)
    End Sub

    Public Sub SetFloat(name As String, value As Single)
        GL.Uniform1(GL.GetUniformLocation(shaderID, name), value)
    End Sub

    Public Sub SetVec2(name As String, x As Single, y As Single)
        GL.Uniform2(GL.GetUniformLocation(shaderID, name), x, y)
    End Sub

    Public Sub SetVec2(name As String, v As Vector2)
        GL.Uniform2(GL.GetUniformLocation(shaderID, name), v.X, v.Y)
    End Sub

    Public Sub SetVec3(name As String, x As Single, y As Single, z As Single)
        GL.Uniform3(GL.GetUniformLocation(shaderID, name), x, y, z)
    End Sub

    Public Sub SetVec3(name As String, v As Vector3)
        GL.Uniform3(GL.GetUniformLocation(shaderID, name), v.X, v.Y, v.Z)
    End Sub

    Public Sub SetMat4(name As String, transpose As Boolean, m As Matrix4)
        GL.UniformMatrix4(GL.GetUniformLocation(shaderID, name), transpose, m)
    End Sub


End Class
