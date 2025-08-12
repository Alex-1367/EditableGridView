Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports System.Windows.Forms

Module PublicFunctions
    Public Sub ShowAutoCloseMessage(ownerForm As Form, message As String, title As String,
                              Optional timeoutMs As Integer = 1000)
        ownerForm.Invoke(Sub()
                             Dim popup As New Form With {
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .StartPosition = FormStartPosition.CenterScreen,
            .Size = New Size(400, 200),
            .Text = title,
            .ShowInTaskbar = False,
            .TopMost = True
        }

                             Dim lbl As New Label With {
            .Text = message,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Font = New Font("Segoe UI", 10)
        }
                             popup.Controls.Add(lbl)

                             ' Auto-close timer
                             Dim timer As New Timer With {.Interval = timeoutMs}
                             AddHandler timer.Tick, Sub(s, e)
                                                        timer.Stop()
                                                        popup.Close()
                                                    End Sub

                             popup.Show(ownerForm)
                             timer.Start()
                         End Sub)
    End Sub
End Module