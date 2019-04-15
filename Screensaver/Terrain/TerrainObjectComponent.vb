﻿Imports System.Collections.Generic
Imports OpenTK
Imports Random

Public Class TerrainObjectComponent
    Private objectShader As Shader

    Private loader As Loader

    Private objects As New List(Of ModelInstance)

    Private models As New List(Of Model)

    Private terrAmplitude As Single
    Private terrModelMatrix As Matrix4

    Private sun As SunManager

    Private random As New Random()

    Private camera As Camera

    Private ReadOnly modelScaleFactors() As Double = {1.2, 0.012}

    Public Sub New(objectVertexShaderSrc As String,
                   objectFragmentShaderSrc As String,
                   terrainModelMatrix As Matrix4,
                   terrainAmplitude As Single,
                   ByRef sunManager As SunManager,
                   ByRef cam As Camera,
                   ByRef load As Loader)
        objectShader = New Shader(objectVertexShaderSrc, objectFragmentShaderSrc)
        loader = load
        camera = cam
        terrAmplitude = terrainAmplitude
        terrModelMatrix = terrainModelMatrix
        sun = sunManager

        models.Add(New Model("firtree1.3ds", loader))
        models.Add(New Model("bush01.obj", loader))
    End Sub

    ' Adds a tree at the specified location if it abides by a set of conditions for generation
    Public Sub AddObjectAtLocation(ByRef pos As Vector3)
        Dim objectDistributionChance = (random.Next() Mod 400.0)

        Dim objectPos = (New Vector4(pos, 1.0) * terrModelMatrix).Xyz
        Dim distFromCameraSquared = ((objectPos.X - camera.Position.X) * (objectPos.X - camera.Position.X)) + ((objectPos.Z - camera.Position.Z) * (objectPos.Z - camera.Position.Z))

        ' If the trees are below a certain altitude and the random number generator places them there then add a new tree
        If pos.Y < (0.7 * terrAmplitude) And
            objectDistributionChance <= 1.0 And
            distFromCameraSquared <= 25000.0 And
            distFromCameraSquared >= 2500.0 Then

            Dim modelChosen = (random.Next() Mod models.Count)
            Dim scaleFactor = (1.5 + (RandomFloatGenerator.instance.NextFloat() Mod 0.25)) * modelScaleFactors(modelChosen)
            objects.Add(New ModelInstance(objectPos, scaleFactor, models(modelChosen)))
        End If
    End Sub

    Public Sub DrawObjects()
        For Each obj In objects
            objectShader.Use()

            objectShader.SetMat4("view", False, camera.ViewMatrix)
            objectShader.SetMat4("projection", False, camera.ProjectionMatrix)

            ' Set sun and lighting related paramters
            objectShader.SetVec3("sunColor", sun.color)
            objectShader.SetVec3("sunDir", sun.lightDir)

            obj.Draw(objectShader)
        Next
    End Sub
End Class
