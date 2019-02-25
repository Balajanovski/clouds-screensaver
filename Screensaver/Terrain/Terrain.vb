﻿Imports SharpNoise
Imports SharpNoise.Builders
Imports SharpNoise.Modules
Imports OpenTK

Public Class Terrain
    Private ridgedMultiFractal As New RidgedMulti() With {
        .Seed = New Random().Next,
        .Lacunarity = 2.0,
        .OctaveCount = 5
    }

    Private amplitudeAdjustedRidgedMulti As New Multiply() With {
        .Source0 = ridgedMultiFractal
    }

    Private terrainNoise As New NoiseMap()
    Private terrainNoiseBuilder As New PlaneNoiseMapBuilder With {
        .DestNoiseMap = terrainNoise
    }

    Private terrainNoiseWidth As Integer
    Private terrainNoiseHeight As Integer
    Private terrainAmpl As Single
    Private terrainSmplDist As Single

    Private terrainPosition As Vector3

    Private terrainModel As RawModel

    Private loader As Loader

    Public Sub New(terrainWidth As Integer,
                   terrainLength As Integer,
                   terrainAmplitude As Single,
                   terrainSampleDistance As Single,
                   pos As Vector3,
                   ByRef loaderComponent As Loader)
        loader = loaderComponent
        terrainNoiseWidth = terrainWidth
        terrainNoiseHeight = terrainLength
        terrainAmpl = terrainAmplitude
        terrainSmplDist = terrainSampleDistance
        terrainPosition = pos

        amplitudeAdjustedRidgedMulti.Source1 = New Constant() With {
            .ConstantValue = terrainAmpl
        }
        terrainNoiseBuilder.SourceModule = amplitudeAdjustedRidgedMulti

        terrainNoiseBuilder.SetDestSize(terrainNoiseWidth, terrainNoiseHeight)
        terrainNoiseBuilder.SetBounds(-1.0, 1.0, -1.0, 1.0)
        terrainNoiseBuilder.Build()

        terrainModel = generateTerrain()
    End Sub

    Private Function generateTerrain() As RawModel
        Dim vertexWidthCount As Integer = terrainNoiseWidth / terrainSmplDist
        Dim vertexHeightCount As Integer = terrainNoiseHeight / terrainSmplDist

        Dim count As Integer = vertexWidthCount * vertexHeightCount

        Dim vertices(count * 3) As Single
        Dim normals(count * 3) As Single
        Dim textures(count * 2) As Single

        Dim indices(6 * (vertexWidthCount - 1) * (vertexHeightCount - 1)) As Integer

        Dim vertexPointer As Integer = 0
        For i As Integer = 0 To (vertexHeightCount - 1)
            For j As Integer = 0 To (vertexWidthCount - 1)
                vertices(vertexPointer * 3) = CType(j, Single) / CType(vertexWidthCount - 1, Single) * terrainNoiseWidth
                vertices(vertexPointer * 3 + 1) = getHeight(j, i)
                vertices(vertexPointer * 3 + 2) = CType(i, Single) / CType(vertexHeightCount - 1, Single) * terrainNoiseHeight

                Dim normal As Vector3 = calculateNormal(j, i)
                normals(vertexPointer * 3) = normal.X
                normals(vertexPointer * 3 + 1) = normal.Y
                normals(vertexPointer * 3 + 2) = normal.Z

                textures(vertexPointer * 2) = CType(j, Single) / CType(vertexWidthCount - 1, Single)
                textures(vertexPointer * 2 + 1) = CType(i, Single) / CType(vertexHeightCount - 1, Single)

                vertexPointer += 1
            Next
        Next

        Dim pointer As Integer = 0
        For gz As Integer = 0 To vertexHeightCount - 2
            For gx As Integer = 0 To vertexWidthCount - 2
                Dim topLeft As Integer = (gz * vertexHeightCount) + gx
                Dim topRight As Integer = topLeft + 1

                Dim bottomLeft As Integer = ((gz + 1) * vertexHeightCount) + gx
                Dim bottomRight As Integer = bottomLeft + 1

                indices(pointer) = topLeft
                pointer += 1
                indices(pointer) = bottomLeft
                pointer += 1
                indices(pointer) = topRight
                pointer += 1
                indices(pointer) = topRight
                pointer += 1
                indices(pointer) = bottomLeft
                pointer += 1
                indices(pointer) = bottomRight
                pointer += 1
            Next
        Next

        Return loader.LoadToVao(vertices, indices, normals, textures)
    End Function

    Public Property Position As Vector3
        Get
            Return terrainPosition
        End Get
        Set(value As Vector3)
            terrainPosition = value
        End Set
    End Property

    Public ReadOnly Property Model As RawModel
        Get
            Return terrainModel
        End Get
    End Property

    Private Function calculateNormal(x As Integer,
                                     z As Integer) As Vector3
        Dim heightL As Single = getHeight(x - 1, z)
        Dim heightR As Single = getHeight(x + 1, z)
        Dim heightD As Single = getHeight(x, z - 1)
        Dim heightU As Single = getHeight(x, z + 1)

        Dim normal As Vector3 = New Vector3(heightL - heightR, 2.0, heightD - heightU)
        normal.NormalizeFast()

        Return normal
    End Function

    Private Function getHeight(x As Integer,
                               z As Integer) As Single
        Return terrainNoise.Item(x, z)
    End Function
End Class