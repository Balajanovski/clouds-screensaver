Imports Microsoft.Win32

Public Class ConfigManager
    Public ReadOnly Property ResolutionRatio As Double
    Public ReadOnly Property WeatherScale As Double
    Public ReadOnly Property CloudsCurliness As Double
    Public ReadOnly Property CloudsSpeed As Double
    Public ReadOnly Property TerrainFrequency As Double

    Private Shared inst As New ConfigManager()

    Public Shared ReadOnly Property Instance As ConfigManager
        Get
            Return inst
        End Get
    End Property

    Public Function ValueExists(key As RegistryKey, value As String) As Boolean
        Try
            Return Not IsNothing(key.GetValue(value))
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Sub New()
        ' Get the values stored in the Registry
        Dim key As RegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Mountains_ScreenSaver")
        If key Is Nothing Or
            Not ValueExists(key, "weatherScale") Or
            Not ValueExists(key, "cloudsCurliness") Or
            Not ValueExists(key, "cloudsSpeed") Or
            Not ValueExists(key, "terrainFrequency") Or
            Not ValueExists(key, "resolutionRatio") Then

            WeatherScale = 1.2
            ResolutionRatio = 0.5
            CloudsCurliness = 0.1
            CloudsSpeed = 450.0
            TerrainFrequency = 1.5
        Else
            WeatherScale = Double.Parse(key.GetValue("weatherScale"))
            CloudsCurliness = Double.Parse(key.GetValue("cloudsCurliness"))
            CloudsSpeed = Double.Parse(key.GetValue("cloudsSpeed"))
            TerrainFrequency = Double.Parse(key.GetValue("terrainFrequency"))
            ResolutionRatio = Double.Parse(key.GetValue("resolutionRatio"))
        End If
    End Sub
End Class
