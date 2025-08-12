Imports System.Drawing
Imports System.Windows.Forms

Public Class SplashForm
    Public Sub New()
        ' This call is required by the designer
        InitializeComponent()
        Me.StartPosition = FormStartPosition.CenterScreen
        ' Remove title bar and borders
        Me.FormBorderStyle = FormBorderStyle.FixedToolWindow
        Me.ControlBox = False
        Me.ShowInTaskbar = False
        Me.Text = ""
    End Sub

    Public Sub New(Owner As Form)
        'always on top
        InitializeComponent()
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedToolWindow
        Me.ControlBox = False
        Me.ShowInTaskbar = False
        Me.Text = ""
        Me.TopMost = True
        Me.TopLevel = True
        Me.Show(Owner)
    End Sub

    Public Sub UpdateProgress(percent As Integer, message As String)
        ' Ensure thread-safe updates to UI controls
        If InvokeRequired Then
            Invoke(Sub() UpdateProgress(percent, message))
            Return
        End If

        ' Center the percentage label
        lblPercentage.Location = New Point(
        ProgressBar1.Left + (ProgressBar1.Width \ 2) - (lblPercentage.Width \ 2),
        ProgressBar1.Top + (ProgressBar1.Height \ 2) - (lblPercentage.Height \ 2)
        )

        ' Update controls
        ProgressBar1.Value = percent
        lblPercentage.Text = $"{percent}%"

        ' Force immediate UI update
        ProgressBar1.Refresh()
        lblPercentage.Refresh()
        Application.DoEvents()
    End Sub



End Class