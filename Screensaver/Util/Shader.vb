Imports OpenTK.Graphics.OpenGL4
Imports OpenTK
Imports System.Reflection
Imports System.IO

Public Class Shader
    Inherits ShaderBase

    Private vertexShader As Integer
    Private fragmentShader As Integer

    Public Sub New(vertexSrc As String, fragSrc As String)

        ' Compile vertex shader
        Dim vertexCode = ExportResource(Assembly.GetExecutingAssembly, "Mountains", vertexSrc)
        vertexShader = GL.CreateShader(ShaderType.VertexShader)
        GL.ShaderSource(vertexShader, vertexCode)
        GL.CompileShader(vertexShader)
        CheckShaderCompilationSuccess(vertexShader)

        ' Compile frag shader
        Dim fragCode = ExportResource(Assembly.GetExecutingAssembly, "Mountains", fragSrc)
        fragmentShader = GL.CreateShader(ShaderType.FragmentShader)
        GL.ShaderSource(fragmentShader, fragCode)
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
