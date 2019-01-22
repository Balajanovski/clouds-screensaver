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
End Class
