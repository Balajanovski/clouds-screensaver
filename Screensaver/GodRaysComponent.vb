Imports OpenTK
Imports OpenTK.Graphics.OpenGL4

Public Class GodRaysComponent
    Private godRaysWidth As Integer
    Private godRaysHeight As Integer

    Private mergeShader As ComputeShader
    Private mergedOcclusion As Integer

    Private quadRenderer As ScreenQuadRenderer
    Private godRaysShader As Shader

    Private sun As SunManager

    Private camera As Camera

    Private Function mergeOcclusionTextures(terrainOcclusionTex As Integer,
                                            cloudOcclusionTex As Integer) As Integer
        ' Generate texture
        Dim merged = GL.GenTexture()
        GL.ActiveTexture(TextureUnit.Texture0)
        GL.BindTexture(TextureTarget.Texture2D, merged)
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, All.MirroredRepeat)
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, All.MirroredRepeat)
        GL.TexStorage2D(TextureTarget2d.Texture2D, 1, SizedInternalFormat.R16f, godRaysWidth, godRaysHeight)
        GL.BindImageTexture(0, merged, 0, False, 0, TextureAccess.WriteOnly, SizedInternalFormat.R16f)

        ' Dispatch shader
        mergeShader.Use()
        mergeShader.SetInt("terrainOcclusion", 1)
        mergeShader.SetInt("cloudOcclusion", 2)

        GL.ActiveTexture(TextureUnit.Texture1)
        GL.BindTexture(TextureTarget.Texture2D, terrainOcclusionTex)
        GL.ActiveTexture(TextureUnit.Texture2)
        GL.BindTexture(TextureTarget.Texture2D, cloudOcclusionTex)
        GL.ActiveTexture(TextureUnit.Texture0)

        mergeShader.Dispatch(godRaysWidth, godRaysHeight, 1)

        Return merged
    End Function

    Public Sub New(vertexShaderSrc As String,
                   fragmentShaderSrc As String,
                   ByRef sunmanager As SunManager,
                   ByRef cam As Camera,
                   ByRef screenQuadRenderer As ScreenQuadRenderer)
        godRaysWidth = DisplayDevice.Default.Width
        godRaysHeight = DisplayDevice.Default.Height
        quadRenderer = screenQuadRenderer
        sun = sunmanager
        camera = cam

        mergeShader = New ComputeShader("MixOcclusionLayers.comp")
        godRaysShader = New Shader(vertexShaderSrc, fragmentShaderSrc)
    End Sub

    Private Sub convertPointToScreenSpace(point As Vector3)
        Dim screenSpacePoint As Vector2 = camera.Project(point)


    End Sub

    Public Sub Render(terrainOcclusionTex As Integer, cloudOcclusionTex As Integer)
        mergedOcclusion = mergeOcclusionTextures(terrainOcclusionTex, cloudOcclusionTex)

        Dim sunPos = sun.position + camera.Position
        Dim sunScreenPos = camera.Project(sunPos)

    End Sub
End Class
