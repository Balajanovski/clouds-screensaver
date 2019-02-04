Imports OpenTK.Graphics.OpenGL4

Public Class NoiseGeneratorBase
    Protected shader As ComputeShader

    ' Uses compute shader
    Public Sub New(shaderSrc As String)
        shader = New ComputeShader(shaderSrc)
    End Sub

    Public Sub AwaitComputationEnd()
        ' Wait for computation to finish
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit)
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit)
        GL.MemoryBarrier(MemoryBarrierFlags.TextureFetchBarrierBit)
    End Sub

    Public Sub Seed(s As Single)
        shader.Use()
        shader.SetFloat("seed", s)
    End Sub
End Class
