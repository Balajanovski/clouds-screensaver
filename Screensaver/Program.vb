Module Program

    Sub Main(args As String())
        If (args.Length > 0) Then
            If args(0).ToLower().Trim().Substring(0, 2) = "/s" Then
                Dim screensaver = New Screensaver()
                screensaver.Run(60.0)
            End If
        Else
            Dim screensaver = New Screensaver()
            screensaver.Run(60.0)
        End If

    End Sub

End Module
