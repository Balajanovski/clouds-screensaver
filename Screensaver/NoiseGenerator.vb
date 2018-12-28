Imports OpenTK.Graphics.OpenGL4

Public Class NoiseGenerator
    Private shader As ComputeShader

    ' Uses compute shader
    Public Sub New(shaderSrc As String)
        shader = New ComputeShader(shaderSrc)
    End Sub

    Public Function GenerateNoise(width As Integer, height As Integer, depth As Integer) As Integer
        ' Generate texture
        Dim noise = GL.GenTexture()
        GL.ActiveTexture(TextureUnit.Texture0)
        GL.BindTexture(TextureTarget.Texture3D, noise)
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1)
        GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, All.MirroredRepeat)
        GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, All.MirroredRepeat)
        GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, All.MirroredRepeat)
        GL.TexImage3D(TextureTarget.Texture3D, 0, PixelInternalFormat.Rgba8, width, height, depth, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero)
        GL.BindImageTexture(0, noise, 0, False, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba8)

        ' Dispatch shader
        shader.Dispatch(width, height, depth)

        Return noise
    End Function

    Public Sub AwaitComputationEnd()
        ' Wait for computation to finish
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit)
        GL.Finish()
    End Sub
End Class
