Imports OpenTK.Graphics.OpenGL4

Public Class NoiseGenerator2D
    Private shader As ComputeShader

    ' Uses compute shader
    Public Sub New(shaderSrc As String)
        shader = New ComputeShader(shaderSrc)
    End Sub

    Public Function GenerateNoise(width As Integer, height As Integer, format As SizedInternalFormat) As Integer
        ' Generate texture
        Dim noise = GL.GenTexture()
        GL.ActiveTexture(TextureUnit.Texture0)
        GL.BindTexture(TextureTarget.Texture2D, noise)
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, All.MirroredRepeat)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, All.MirroredRepeat)
        GL.TexStorage2D(TextureTarget2d.Texture2D, 1, format, width, height)
        GL.BindImageTexture(0, noise, 0, False, 0, TextureAccess.WriteOnly, format)

        ' Dispatch shader
        shader.Use()
        shader.Dispatch(width, height, 1)

        Return noise
    End Function

    Public Sub AwaitComputationEnd()
        ' Wait for computation to finish
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit)
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit)
        GL.MemoryBarrier(MemoryBarrierFlags.TextureFetchBarrierBit)
    End Sub
End Class
