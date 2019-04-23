Imports OpenTK

' Thank you to fede-vaccaro for the color presets
' Color presets found at: https://github.com/fede-vaccaro/TerrainEngine-OpenGL/blob/02c7049e6fa995c5cbdbf6c268bea93083272b80/DrawableObjects/CloudsModel.cpp

Public Structure Preset
    Public cloudColorBottom As Vector3
    Public skyColorTop As Vector3
    Public skyColorBottom As Vector3

    Public lightColor As Vector3
    Public fogColor As Vector3
End Structure

Class Presets
    Public Shared Function SunsetPreset() As Preset
        Dim preset As New Preset()

        preset.cloudColorBottom = New Vector3(89, 96, 109) / 255.0
        preset.skyColorTop = New Vector3(177, 174, 119) / 255.0
        preset.skyColorBottom = New Vector3(234, 125, 125) / 255.0

        preset.lightColor = New Vector3(255, 171, 125) / 255.0
        preset.fogColor = New Vector3(85, 97, 120) / 255.0

        Return preset
    End Function

    Public Shared Function DefaultPreset() As Preset
        Dim preset As New Preset()

        preset.cloudColorBottom = New Vector3(65.0, 70.0, 80.0) * (1.5 / 255.0)
        preset.skyColorTop = New Vector3(0.5, 0.7, 0.8) * 1.05
        preset.skyColorBottom = New Vector3(0.9, 0.9, 0.95)

        preset.lightColor = New Vector3(255, 255, 230) / 255.0
        preset.fogColor = New Vector3(0.5, 0.6, 0.7)

        Return preset
    End Function

    Public Shared Function MixPresets(v As Single, p1 As Preset, p2 As Preset) As Preset
        Dim preset As New Preset()

        Dim a As Single = Math.Min(Math.Max(v, 0.0), 1.0) ' Clamp Between 0.0 and 1.0
        Dim b As Single = 1.0 - a

        preset.cloudColorBottom = p1.cloudColorBottom * a + p2.cloudColorBottom * b
        preset.skyColorTop = p1.skyColorTop * a + p2.skyColorTop * b
        preset.skyColorBottom = p1.skyColorBottom * a + p2.skyColorBottom * b
        preset.lightColor = p1.lightColor * a + p2.lightColor * b
        preset.fogColor = p1.fogColor * a + p2.fogColor * b

        Return preset
    End Function
End Class
