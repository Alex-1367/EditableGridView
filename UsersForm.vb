Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class UsersForm
    Private DatabaseRecordList As List(Of Authorization)
    Private GridviewBindingList As SortableBindingList(Of Authorization)
    Private DeletedRecords As New List(Of Authorization)

    Private Sub UsersForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Configure DataGridView settings
        With dgvAuthorizations
            .AllowUserToAddRows = True
            .AllowUserToDeleteRows = False
            .EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
            .AutoGenerateColumns = True
            .MultiSelect = False
        End With
        ShowSplashAndLoadData()
    End Sub

    Private Sub InitializeContextMenu()
        ' Create delete menu item
        Dim deleteItem = New ToolStripMenuItem("Delete Row")
        AddHandler deleteItem.Click, AddressOf SafeDeleteRow
        ContextMenuStrip1.Items.Add(deleteItem)

        ' Assign to DataGridView
        dgvAuthorizations.ContextMenuStrip = ContextMenuStrip1
    End Sub

    Private Async Sub LoadData()
        Dim splash As New SplashForm()
        splash.Show(Me)
        splash.UpdateProgress(0, "Loading data...")

        Try
            ' Load data with progress updates
            DatabaseRecordList = Await ReadAllDbRecords(splash)

            ' Now update UI
            splash.UpdateProgress(90, "Updating display...")
            DeletedRecords.Clear()
            BindDataToGrid()

            splash.UpdateProgress(100, "Done!")
            Application.DoEvents() ' Ensure final update shows
        Catch ex As Exception
            MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                      MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            splash.Close()
            splash.Dispose()
        End Try
    End Sub

    Private Async Sub ShowSplashAndLoadData()
        Dim splash As New SplashForm() ' Don't use Using here
        splash.Show(Me)
        splash.UpdateProgress(0, "Initializing...")
        Application.DoEvents()

        Try
            ' Configure grid settings
            With dgvAuthorizations
                .AllowUserToAddRows = True
                .AllowUserToDeleteRows = False
                .EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
                .AutoGenerateColumns = True
                .MultiSelect = False
            End With

            ' Load data with progress updates
            splash.UpdateProgress(10, "Loading data...")
            DatabaseRecordList = Await ReadAllDbRecords(splash)

            splash.UpdateProgress(90, "Preparing interface...")
            DeletedRecords.Clear()
            BindDataToGrid()
            InitializeContextMenu()

            splash.UpdateProgress(100, "Ready")
            Application.DoEvents()
            Await Task.Delay(500) ' Let user see completion
        Catch ex As Exception
            MessageBox.Show($"Error initializing: {ex.Message}", "Error",
                      MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            splash.Close()
            splash.Dispose()
        End Try
    End Sub

    Private Async Function ReadAllDbRecords(splash As SplashForm) As Task(Of List(Of Authorization))
        Try
            Return Await AuthDB.GetAllWithProgressAsync(
            Sub(progress, count)
                Me.Invoke(Sub()
                              splash.UpdateProgress(10 + CInt(progress * 0.8), $"Loaded {count} records")
                          End Sub)
            End Sub)
        Catch ex As Exception
            Me.Invoke(Sub()
                          splash.UpdateProgress(0, $"Error: {ex.Message}")
                      End Sub)
            Throw
        End Try
    End Function

    Private Sub BindDataToGrid()
        GridviewBindingList = New SortableBindingList(Of Authorization)(
        DatabaseRecordList.Select(Function(a)
                                      Return New Authorization With {
                                                                        .LoginID = a.LoginID,
                                                                        .Login = a.Login,
                                                                        .Password = a.Password,
                                                                        .RoleName = a.RoleName,
                                                                        .Phone = a.Phone,
                                                                        .Email = a.Email,
                                                                        .IsActive = a.IsActive
                                                                    }
                                  End Function).ToList()
                                  )
        dgvAuthorizations.DataSource = Nothing
        dgvAuthorizations.DataSource = GridviewBindingList

        ' Configure columns
        ConfigureDataGridViewColumns()
    End Sub

    Private Sub ConfigureDataGridViewColumns()
        With dgvAuthorizations
            ' Enable sorting for all columns
            .Columns.Cast(Of DataGridViewColumn).ToList().ForEach(Sub(c) c.SortMode = DataGridViewColumnSortMode.Automatic)

            ' Hide timestamp columns
            If .Columns.Contains("CreatedAt") Then .Columns("CreatedAt").Visible = False
            If .Columns.Contains("UpdatedAt") Then .Columns("UpdatedAt").Visible = False

            ' Make LoginID read-only
            If .Columns.Contains("LoginID") Then
                .Columns("LoginID").ReadOnly = True
                .Columns("LoginID").DefaultCellStyle.NullValue = "0"
            End If

            ' Set default null values for other columns
            For Each col As DataGridViewColumn In .Columns
                If col.ValueType Is GetType(String) Then
                    col.DefaultCellStyle.NullValue = String.Empty
                End If
            Next
        End With
    End Sub

    Private Sub SafeDeleteRow(sender As Object, e As EventArgs)
        If dgvAuthorizations.SelectedRows.Count = 0 Then Return

        ' Get the first selected row
        Dim selectedRow = dgvAuthorizations.SelectedRows(0)

        ' Skip if it's a new row
        If selectedRow.IsNewRow Then Return

        ' Confirm deletion
        If MessageBox.Show("Delete selected row?", "Confirm",
                         MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.No Then Return

        Try
            ' Suspend drawing while we update
            dgvAuthorizations.SuspendLayout()

            ' Get the bound item
            Dim auth = TryCast(selectedRow.DataBoundItem, Authorization)
            If auth IsNot Nothing Then
                ' Remove from lists
                DatabaseRecordList.Remove(auth)
                DeleteProcessStart = True
                GridviewBindingList.Remove(auth)
                DeleteProcessStart = False

                ' Add to deleted records if it exists in DB
                If auth.LoginID > 0 Then
                    DeletedRecords.Add(auth)
                End If
            End If

            ' Force a complete refresh
            dgvAuthorizations.Invalidate()

        Catch ex As Exception
            MessageBox.Show($"Error deleting row: {ex.Message}", "Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
            ' Revert changes
            LoadData()
        Finally
            dgvAuthorizations.ResumeLayout()
        End Try
    End Sub

    Private Async Sub SaveButton_Click(sender As Object, e As EventArgs) Handles SaveButton.Click
        Dim splash As New SplashForm()
        splash.Show(Me)
        splash.UpdateProgress(0, "Starting save operation...")
        Application.DoEvents()

        Try
            ' Process deletions
            splash.UpdateProgress(10, "Processing deletions...")
            Dim TmpDelList As New List(Of Integer)
            For Each auth In DeletedRecords
                TmpDelList.Add(auth.LoginID)
                Await AuthDB.DeleteAsync(auth.LoginID)
            Next
            If TmpDelList.Any() Then ShowAutoCloseMessage(Me, $"Records deleted {String.Join(", ", TmpDelList)} successfully!", "Success")

            ' Process inserts
            splash.UpdateProgress(30, "Processing inserts...")
            Dim TmpAddList As New List(Of Integer)
            For Each newAuth In GridviewBindingList.Where(Function(x) x.LoginID <= 0)
                Try
                    newAuth.IsActive = If(newAuth.IsActive Is Nothing, True, newAuth.IsActive)
                    Dim newId = Await AuthDB.InsertAndReturnIdAsync(newAuth)
                    newAuth.LoginID = newId
                    TmpAddList.Add(newId)
                Catch ex As Exception
                    MessageBox.Show($"Error adding record: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End Try
            Next
            If TmpAddList.Any() Then ShowAutoCloseMessage(Me, $"Record inserted {String.Join(", ", TmpAddList)} successfully!", "Success")


            ' Process updates
            splash.UpdateProgress(60, "Processing updates...")
            Dim TmpUpdList As New List(Of Integer)
            For Each modifiedAuth In GridviewBindingList.Where(Function(x) x.LoginID > 0)
                Dim originalAuth = DatabaseRecordList.FirstOrDefault(Function(x) x.LoginID = modifiedAuth.LoginID)
                If originalAuth IsNot Nothing AndAlso Not DeletedRecords.Contains(modifiedAuth) Then
                    If DetectChanges(originalAuth, modifiedAuth) Then
                        Try
                            Await AuthDB.UpdateAsync(modifiedAuth)
                            TmpUpdList.Add(modifiedAuth.LoginID)
                        Catch ex As Exception
                            MessageBox.Show($"Error updating record {modifiedAuth.LoginID}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        End Try
                    End If
                End If
            Next
            If TmpUpdList.Any() Then ShowAutoCloseMessage(Me, $"Changes  {String.Join(", ", TmpUpdList)} saved successfully!", "Success")

            ' Refresh data
            splash.UpdateProgress(80, "Refreshing data...")
            DatabaseRecordList = Await ReadAllDbRecords(splash)
            DeletedRecords.Clear()
            BindDataToGrid()

            splash.UpdateProgress(100, "Save complete!")
            Application.DoEvents()
            Await Task.Delay(500)
        Catch ex As Exception
            MessageBox.Show($"Error saving data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            splash.Close()
            splash.Dispose()
        End Try
    End Sub

    Private Async Sub RefreshButton_Click(sender As Object, e As EventArgs) Handles RefreshButton.Click
        ' Create and show splash screen
        Dim splash As New SplashForm()
        splash.Show(Me)
        splash.UpdateProgress(0, "Starting refresh...")
        Application.DoEvents() ' Force the splash to render immediately

        Try
            ' Step 1: Load data with progress (0-80%)
            splash.UpdateProgress(10, "Loading records...")
            DatabaseRecordList = Await ReadAllDbRecords(splash) ' This should report progress internally

            ' Step 2: Update UI (80-100%)
            splash.UpdateProgress(80, "Updating display...")
            DeletedRecords.Clear()
            BindDataToGrid()

            ' Completion
            splash.UpdateProgress(100, "Refresh complete!")
            Application.DoEvents()
            Await Task.Delay(500) ' Let user see completion message

        Catch ex As Exception
            MessageBox.Show($"Refresh failed: {ex.Message}", "Error",
                      MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            splash.Close()
            splash.Dispose()
        End Try
    End Sub

    Private Function DetectChanges(originalAuth As Authorization, modifiedAuth As Authorization) As Boolean
        ' Compare only business fields, ignoring auto-updated timestamps
        Return originalAuth.Login <> modifiedAuth.Login OrElse
           originalAuth.Password <> modifiedAuth.Password OrElse
           originalAuth.RoleName <> modifiedAuth.RoleName OrElse
           originalAuth.Phone <> modifiedAuth.Phone OrElse
           originalAuth.Email <> modifiedAuth.Email OrElse
           originalAuth.IsActive <> modifiedAuth.IsActive
    End Function

    Private Sub dgvAuthorizations_DataError(sender As Object, e As DataGridViewDataErrorEventArgs) Handles dgvAuthorizations.DataError
        ' Suppress the error - we'll handle it through validation
        e.ThrowException = False

        ' Optional: Log the error if needed
        Debug.WriteLine($"DataError at row {e.RowIndex}, column {e.ColumnIndex}: {e.Exception.Message}")
    End Sub

    Dim DeleteProcessStart As Boolean = False
    Private Sub dgvAuthorizations_RowValidating(sender As Object, e As DataGridViewCellCancelEventArgs) Handles dgvAuthorizations.RowValidating
        ' Skip if we're not validating a valid row
        If e.RowIndex < 0 OrElse e.RowIndex >= dgvAuthorizations.Rows.Count Then Return

        Dim row = dgvAuthorizations.Rows(e.RowIndex)

        ' Skip validation for empty new row
        If row.IsNewRow Then Return
        If DeleteProcessStart Then Return

        ' Validate Login (username)
        If row.Cells("Login").Value Is Nothing OrElse String.IsNullOrWhiteSpace(row.Cells("Login").Value.ToString()) Then
            MessageBox.Show("Username cannot be empty", "Validation Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Warning)
            e.Cancel = True
        End If

        ' Validate Password
        If row.Cells("Password").Value Is Nothing OrElse String.IsNullOrWhiteSpace(row.Cells("Password").Value.ToString()) Then
            MessageBox.Show("Password cannot be empty", "Validation Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Warning)
            e.Cancel = True
        End If
    End Sub


End Class