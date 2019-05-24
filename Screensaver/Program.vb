Imports OpenTK
Imports System.Windows

Class Program

    Public Shared Property WinApp As Application

    <STAThread>
    Shared Sub Main(args As String())
        If (args.Length > 0) Then
            Dim command As String = args(0).ToLower().Trim()
            Dim secondArgument As String = Nothing

            ' Parse arguments
            If (command.Length > 2) Then
                secondArgument = command.Substring(3).Trim()
                command = command.Substring(0, 2)
            ElseIf (args.Length > 1) Then
                secondArgument = args(1)
            End If

            Dim screensaver As Screensaver
            If command = "/s" Then
                screensaver = New Screensaver(DisplayDevice.Default.Width,
                                                  DisplayDevice.Default.Height,
                                                  False, IntPtr.Zero, GameWindowFlags.Fullscreen)
                screensaver.Run(60.0)
            ElseIf command = "/p" Then
                Dim previewWindowHandle = New IntPtr(Long.Parse(secondArgument))

                screensaver = New Screensaver(DisplayDevice.Default.Width,
                                                  DisplayDevice.Default.Height,
                                                  True, previewWindowHandle, GameWindowFlags.Default)
                screensaver.Run(60.0)
            ElseIf command = "/c" Then
                InitializeConfigWindow()
            End If
        Else
            Dim screensaver = New Screensaver(DisplayDevice.Default.Width,
                                              DisplayDevice.Default.Height,
                                              False, IntPtr.Zero, GameWindowFlags.Fullscreen)
            screensaver.Run(60.0)
        End If

    End Sub

    Shared Sub InitializeConfigWindow()
        WinApp = New Application()
        WinApp.Run(New ConfigurationWindow())
    End Sub

End Class
