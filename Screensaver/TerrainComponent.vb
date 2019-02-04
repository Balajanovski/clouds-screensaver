Imports OpenTK
Imports OpenTK.Graphics.OpenGL4

Public Class TerrainComponent
    Private terrainNoiseGen As NoiseGenerator2D

    Private terrainNoise As Integer

    Private camera As Camera

    Private terrainNoiseWidth As Integer
    Private terrainNoiseHeight As Integer
    Private terrainAmpl As Single
    Private terrainSmplDist As Single

    Public Sub New(terrainWidth As Integer,
                   terrainLength As Integer,
                   terrainAmplitude As Single,
                   terrainSampleDistance As Single,
                   ByRef cam As Camera)
        camera = cam
        terrainNoiseWidth = terrainWidth
        terrainNoiseHeight = terrainLength
        terrainAmpl = terrainAmplitude
        terrainSampleDistance = terrainSampleDistance

        terrainNoiseGen = New NoiseGenerator2D("GenerateTerrainNoise.comp")
        terrainNoiseGen.Seed(RandomFloatGenerator.instance().NextFloat())
        terrainNoise = terrainNoiseGen.GenerateNoise(terrainNoiseWidth,
                                                     terrainNoiseHeight,
                                                     SizedInternalFormat.R8)
        terrainNoiseGen.AwaitComputationEnd()
    End Sub
End Class
