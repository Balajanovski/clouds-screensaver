Imports Microsoft.Win32
Imports System.Windows.Forms

Public Class ConfigurationWindow

    Private savedSettings As Boolean = False

    Public Sub New()
        ' This call is required by the designer.
        InitializeComponent()

        savedSettings = False
    End Sub

    Private Sub SaveSettings()
        savedSettings = True

        ' Create or get existing Registry subkey
        Dim key As RegistryKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Mountains_ScreenSaver")

        key.SetValue("weatherScale", weatherScaleSlider.Value)
        key.SetValue("cloudsCurliness", cloudsCurlinessSlider.Value)
        key.SetValue("resolutionRatio", CDbl(1.0 / resolutionSlider.Value))
        key.SetValue("cloudsSpeed", cloudsSpeedSlider.Value)
        key.SetValue("terrainFrequency", terrainFrequencySlider.Value)

        MessageBox.Show("Screensaver Settings Saved Successfully!", "Settings Saved!", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1)
    End Sub

    Private Sub Window_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs)
        If Not savedSettings Then
            Dim result As DialogResult = MessageBox.Show("You are about to exit without saving" _
                & vbCrLf & "Are you sure you want to proceed",
                                                      "Exit Without Saving?",
                                                      MessageBoxButtons.YesNo,
                                                      MessageBoxIcon.Question,
                                                      MessageBoxDefaultButton.Button1)

            If result = Windows.Forms.DialogResult.Yes Then
                e.Cancel = False
            Else
                e.Cancel = True
            End If
        End If
    End Sub

    Private Sub SaveButton_Click(sender As Object, e As Windows.RoutedEventArgs) Handles saveButton.Click
        SaveSettings()
    End Sub

    Private Sub ResolutionSlider_ValueChanged(sender As Object, e As Windows.RoutedPropertyChangedEventArgs(Of Double)) Handles resolutionSlider.ValueChanged
        savedSettings = False

        resolutionLabel.Content = "Resolution Ratio (Value: 1:" & String.Format("{0:0}", resolutionSlider.Value) & "):"
    End Sub

    Private Sub WeatherScaleSlider_ValueChanged(sender As Object, e As Windows.RoutedPropertyChangedEventArgs(Of Double)) Handles weatherScaleSlider.ValueChanged
        savedSettings = False

        weatherScaleLabel.Content = "Weather Scale (Value: " & String.Format("{0:0.00}", weatherScaleSlider.Value) & "):"
    End Sub

    Private Sub CloudsCurlinessSlider_ValueChanged(sender As Object, e As Windows.RoutedPropertyChangedEventArgs(Of Double)) Handles cloudsCurlinessSlider.ValueChanged
        savedSettings = False

        cloudCurlinessLabel.Content = "Cloud Curliness (Value: " & String.Format("{0:0.00}", cloudsCurlinessSlider.Value) & "):"
    End Sub

    Private Sub CloudsSpeedSlider_ValueChanged(sender As Object, e As Windows.RoutedPropertyChangedEventArgs(Of Double)) Handles cloudsSpeedSlider.ValueChanged
        savedSettings = False

        cloudsSpeedLabel.Content = "Cloud Speed (Value: " & String.Format("{0:0.0}", cloudsSpeedSlider.Value) & "):"
    End Sub
End Class
