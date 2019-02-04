Imports OpenTK.Graphics.OpenGL4

Public Class NoiseGenerator3D
    Inherits NoiseGeneratorBase

    ' Uses compute shader
    Public Sub New(shaderSrc As String)
        MyBase.New(shaderSrc)
    End Sub

    Public Function GenerateNoise(width As Integer, height As Integer, depth As Integer, format As SizedInternalFormat) As Integer
        ' Generate texture
        Dim noise = GL.GenTexture()
        GL.ActiveTexture(TextureUnit.Texture0)
        GL.BindTexture(TextureTarget.Texture3D, noise)
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1)
        GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, All.MirroredRepeat)
        GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, All.MirroredRepeat)
        GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, All.MirroredRepeat)
        GL.TexStorage3D(TextureTarget3d.Texture3D, 1, format, width, height, depth)
        GL.BindImageTexture(0, noise, 0, True, 0, TextureAccess.WriteOnly, format)

        ' Dispatch shader
        shader.Use()
        shader.Dispatch(width, height, depth)

        Return noise
    End Function
End Class
