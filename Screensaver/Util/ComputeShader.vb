Imports OpenTK.Graphics.OpenGL4
Imports System.Reflection
Imports System.IO

Public Class ComputeShader
    Inherits ShaderBase

    Public Sub New(shaderSrc As String)
        ' Compile compute shader
        Dim computeCode = ExportResource(Assembly.GetExecutingAssembly, "Screensaver", shaderSrc)
        Dim computeShader = GL.CreateShader(ShaderType.ComputeShader)
        GL.ShaderSource(computeShader, computeCode)
        GL.CompileShader(computeShader)
        CheckShaderCompilationSuccess(computeShader)

        ' Link shaders into shader program
        shaderID = GL.CreateProgram()
        GL.AttachShader(shaderID, computeShader)
        GL.LinkProgram(shaderID)
        CheckProgramLinkingSuccess()

        ' Cleanup
        GL.DetachShader(shaderID, computeShader)
        GL.DeleteShader(computeShader)
    End Sub

    Public Sub Dispatch(width As Integer, height As Integer, depth As Integer)
        Use()
        GL.DispatchCompute(width, height, depth)
    End Sub
End Class
