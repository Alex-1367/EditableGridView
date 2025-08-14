Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports MySqlGenericRepository
Imports Traveller.My

Public Class RestaurantsForm
    Dim RestaurantViewDB As GenericRepository(Of Restaurantswithtown)
    Dim RestaurantTableDB As GenericRepositoryOverViewy(Of Restaurants, Restaurantswithtown)
    Dim DatabaseRecordList As List(Of Restaurantswithtown)
    Dim GridviewBindingList As SortableBindingList(Of Restaurantswithtown)
    Dim DeletedRecords As New List(Of Restaurantswithtown)
    Dim TownsList As List(Of Towns)

    Private Sub RestaurantsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Configure DataGridView settings
        With dgvRestaurants
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
        dgvRestaurants.ContextMenuStrip = ContextMenuStrip1
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
            Application.DoEvents()
        Catch ex As Exception
            MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                      MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            splash.Close()
            splash.Dispose()
        End Try
    End Sub

    Private Async Sub ShowSplashAndLoadData()
        Dim splash As New SplashForm()
        splash.Show(Me)
        splash.UpdateProgress(0, "Initializing...")
        Application.DoEvents()

        Try
            ' Configure grid settings
            With dgvRestaurants
                .AllowUserToAddRows = True
                .AllowUserToDeleteRows = False
                .EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
                .AutoGenerateColumns = True
                .MultiSelect = False
            End With

            splash.UpdateProgress(10, "Loading towns...")
            Dim townsRepo As New GenericRepository(Of Towns)(MySettings.Default.MySqlConnectionString)
            TownsList = Await townsRepo.GetAllAsync()

            ' Load data with progress updates
            splash.UpdateProgress(10, "Loading data...")
            DatabaseRecordList = Await ReadAllDbRecords(splash)

            splash.UpdateProgress(90, "Preparing interface...")
            DeletedRecords.Clear()
            BindDataToGrid()
            InitializeContextMenu()

            splash.UpdateProgress(100, "Ready")
            Application.DoEvents()
            Await Task.Delay(500)
        Catch ex As Exception
            MessageBox.Show($"Error initializing: {ex.Message}", "Error",
                      MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            splash.Close()
            splash.Dispose()
        End Try
    End Sub

    Private Async Function ReadAllDbRecords(splash As SplashForm) As Task(Of List(Of Restaurantswithtown))
        Try
            RestaurantViewDB = New GenericRepository(Of Restaurantswithtown)(MySettings.Default.MySqlConnectionString,, "RestaurantID", {"CreatedAt", "UpdatedAt"})
            RestaurantTableDB = New GenericRepositoryOverViewy(Of Restaurants, Restaurantswithtown)(MySettings.Default.MySqlConnectionString,, "RestaurantID", {"CreatedAt", "UpdatedAt"})
            Return Await RestaurantViewDB.GetAllWithProgressAsync(
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
        GridviewBindingList = New SortableBindingList(Of Restaurantswithtown)(
        DatabaseRecordList.Select(Function(r)
                                      Return New Restaurantswithtown With {
                                    .RestaurantID = r.RestaurantID,
                                    .TownID = r.TownID,
                                    .RestaurantName = r.RestaurantName,
                                    .Description = r.Description,
                                    .Phone = r.Phone,
                                    .Email = r.Email,
                                    .CreatedAt = r.CreatedAt,
                                    .UpdatedAt = r.UpdatedAt,
                                    .IsActive = r.IsActive,
                                    .TownName = r.TownName
                                }
                                  End Function).ToList())

        dgvRestaurants.DataSource = Nothing
        dgvRestaurants.DataSource = GridviewBindingList

        ConfigureDataGridViewColumns()
    End Sub

    Private Sub ConfigureDataGridViewColumns()
        With dgvRestaurants
            ' Enable sorting for all columns
            .Columns.Cast(Of DataGridViewColumn).ToList().ForEach(Sub(c) c.SortMode = DataGridViewColumnSortMode.Automatic)

            ' Hide timestamp columns
            If .Columns.Contains("CreatedAt") Then .Columns("CreatedAt").Visible = False
            If .Columns.Contains("UpdatedAt") Then .Columns("UpdatedAt").Visible = False

            ' Make RestaurantID read-only
            If .Columns.Contains("RestaurantID") Then
                .Columns("RestaurantID").ReadOnly = True
                .Columns("RestaurantID").DefaultCellStyle.NullValue = "0"
            End If

            ' Configure TownName column to show dropdown
            If .Columns.Contains("TownName") Then
                .Columns("TownName").ReadOnly = False
                .Columns("TownName").HeaderText = "Town"

                ' Replace with a ComboBox column
                Dim townColIndex = .Columns("TownName").Index
                .Columns.Remove("TownName")

                Dim townComboCol As New DataGridViewComboBoxColumn()
                townComboCol.Name = "TownName"
                townComboCol.HeaderText = "Town"
                townComboCol.DataPropertyName = "TownName"
                townComboCol.DisplayMember = "TownName"
                townComboCol.ValueMember = "TownID"
                townComboCol.DataSource = TownsList
                townComboCol.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing

                .Columns.Insert(townColIndex, townComboCol)
            End If

            ' Hide TownID column (we'll use it for binding)
            If .Columns.Contains("TownID") Then
                .Columns("TownID").Visible = False
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
        If dgvRestaurants.SelectedRows.Count = 0 Then Return

        Dim selectedRow = dgvRestaurants.SelectedRows(0)
        If selectedRow.IsNewRow Then Return

        If MessageBox.Show("Delete selected row?", "Confirm",
                         MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.No Then Return

        Try
            dgvRestaurants.SuspendLayout()

            Dim restaurant = TryCast(selectedRow.DataBoundItem, Restaurantswithtown)
            If restaurant IsNot Nothing Then
                DatabaseRecordList.Remove(restaurant)
                DeleteProcessStart = True
                GridviewBindingList.Remove(restaurant)
                DeleteProcessStart = False

                If restaurant.RestaurantID > 0 Then
                    DeletedRecords.Add(restaurant)
                End If
            End If

            dgvRestaurants.Invalidate()

        Catch ex As Exception
            MessageBox.Show($"Error deleting row: {ex.Message}", "Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
            LoadData()
        Finally
            dgvRestaurants.ResumeLayout()
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
            For Each restaurant In DeletedRecords
                TmpDelList.Add(restaurant.RestaurantID)
                Await RestaurantTableDB.DeleteAsync(restaurant.RestaurantID)
            Next
            If TmpDelList.Any() Then ShowAutoCloseMessage(Me, $"Records deleted {String.Join(", ", TmpDelList)} successfully!", "Success")

            ' Process inserts
            splash.UpdateProgress(30, "Processing inserts...")
            Dim TmpAddList As New List(Of Integer)
            For Each newRestaurant In GridviewBindingList.Where(Function(x) x.RestaurantID <= 0)
                Try
                    newRestaurant.IsActive = If(newRestaurant.IsActive Is Nothing, True, newRestaurant.IsActive)

                    Dim newId = Await RestaurantTableDB.InsertAndReturnIdAsync(newRestaurant)
                    newRestaurant.RestaurantID = newId
                    TmpAddList.Add(newId)
                Catch ex As Exception
                    MessageBox.Show($"Error adding record: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End Try
            Next
            If TmpAddList.Any() Then ShowAutoCloseMessage(Me, $"Record inserted {String.Join(", ", TmpAddList)} successfully!", "Success")

            ' Process updates
            splash.UpdateProgress(60, "Processing updates...")
            Dim TmpUpdList As New List(Of Integer)
            For Each modifiedRestaurant In GridviewBindingList.Where(Function(x) x.RestaurantID > 0)
                Dim originalRestaurant = DatabaseRecordList.FirstOrDefault(Function(x) x.RestaurantID = modifiedRestaurant.RestaurantID)
                If originalRestaurant IsNot Nothing AndAlso Not DeletedRecords.Contains(modifiedRestaurant) Then
                    If DetectChanges(originalRestaurant, modifiedRestaurant) Then
                        Try
                            Await RestaurantTableDB.UpdateAsync(modifiedRestaurant)
                            TmpUpdList.Add(modifiedRestaurant.RestaurantID)
                        Catch ex As Exception
                            MessageBox.Show($"Error updating record {modifiedRestaurant.RestaurantID}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        End Try
                    End If
                End If
            Next
            If TmpUpdList.Any() Then ShowAutoCloseMessage(Me, $"Changes {String.Join(", ", TmpUpdList)} saved successfully!", "Success")

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
        Dim splash As New SplashForm()
        splash.Show(Me)
        splash.UpdateProgress(0, "Starting refresh...")
        Application.DoEvents()

        Try
            splash.UpdateProgress(10, "Loading records...")
            DatabaseRecordList = Await ReadAllDbRecords(splash)

            splash.UpdateProgress(80, "Updating display...")
            DeletedRecords.Clear()
            BindDataToGrid()

            splash.UpdateProgress(100, "Refresh complete!")
            Application.DoEvents()
            Await Task.Delay(500)

        Catch ex As Exception
            MessageBox.Show($"Refresh failed: {ex.Message}", "Error",
                      MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            splash.Close()
            splash.Dispose()
        End Try
    End Sub

    Private Function DetectChanges(originalRestaurant As Restaurantswithtown, modifiedRestaurant As Restaurantswithtown) As Boolean
        Return originalRestaurant.TownID <> modifiedRestaurant.TownID OrElse
           originalRestaurant.RestaurantName <> modifiedRestaurant.RestaurantName OrElse
           originalRestaurant.Description <> modifiedRestaurant.Description OrElse
           originalRestaurant.Phone <> modifiedRestaurant.Phone OrElse
           originalRestaurant.Email <> modifiedRestaurant.Email OrElse
           originalRestaurant.IsActive <> modifiedRestaurant.IsActive
    End Function

    Private Sub dgvRestaurants_CellEndEdit(sender As Object, e As DataGridViewCellEventArgs) Handles dgvRestaurants.CellEndEdit
        ' Update TownID when TownName is changed
        If e.ColumnIndex = dgvRestaurants.Columns("TownName").Index AndAlso e.RowIndex >= 0 Then
            Dim row = dgvRestaurants.Rows(e.RowIndex)
            If Not row.IsNewRow Then
                Dim comboCell = DirectCast(row.Cells("TownName"), DataGridViewComboBoxCell)
                If comboCell.Value IsNot Nothing Then
                    ' Get the selected town from the dropdown
                    Dim selectedTown = TownsList.FirstOrDefault(Function(t) t.TownID = CInt(comboCell.Value))
                    If selectedTown IsNot Nothing Then
                        ' Update both TownName and TownID in the bound object
                        Dim restaurant = DirectCast(row.DataBoundItem, Restaurantswithtown)
                        restaurant.TownID = selectedTown.TownID
                        restaurant.TownName = selectedTown.TownName
                    End If
                End If
            End If
        End If
    End Sub

    Private Sub dgvRestaurants_DataError(sender As Object, e As DataGridViewDataErrorEventArgs) Handles dgvRestaurants.DataError
        e.ThrowException = False
        Debug.WriteLine($"DataError at row {e.RowIndex}, column {e.ColumnIndex}: {e.Exception.Message}")
    End Sub

    Dim DeleteProcessStart As Boolean = False
    Private Sub dgvRestaurants_RowValidating(sender As Object, e As DataGridViewCellCancelEventArgs) Handles dgvRestaurants.RowValidating
        If e.RowIndex < 0 OrElse e.RowIndex >= dgvRestaurants.Rows.Count Then Return

        Dim row = dgvRestaurants.Rows(e.RowIndex)
        If row.IsNewRow Then Return
        If DeleteProcessStart Then Return

        ' Validate RestaurantName
        If row.Cells("RestaurantName").Value Is Nothing OrElse String.IsNullOrWhiteSpace(row.Cells("RestaurantName").Value.ToString()) Then
            MessageBox.Show("Restaurant name cannot be empty", "Validation Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Warning)
            e.Cancel = True
        End If

        ' Validate TownID
        If row.Cells("TownID").Value Is Nothing OrElse Not IsNumeric(row.Cells("TownID").Value) Then
            MessageBox.Show("Please select a valid town", "Validation Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Warning)
            e.Cancel = True
        End If
    End Sub

    Private Sub dgvRestaurants_ColumnHeaderMouseClick(sender As Object, e As DataGridViewCellMouseEventArgs) Handles dgvRestaurants.ColumnHeaderMouseClick
        If e.ColumnIndex < 0 Then Return

        Dim column = dgvRestaurants.Columns(e.ColumnIndex)
        If column.SortMode = DataGridViewColumnSortMode.NotSortable Then Return

        Dim direction As ListSortDirection
        If dgvRestaurants.SortedColumn Is column Then
            direction = If(dgvRestaurants.SortOrder = SortOrder.Ascending,
                          ListSortDirection.Descending,
                          ListSortDirection.Ascending)
        Else
            direction = ListSortDirection.Ascending
        End If

        ' Apply the sort
        Dim propertyName = column.DataPropertyName
        If Not String.IsNullOrEmpty(propertyName) Then
            GridviewBindingList.ApplySort(propertyName, direction)
            column.HeaderCell.SortGlyphDirection = If(direction = ListSortDirection.Ascending,
                                                   SortOrder.Ascending,
                                                   SortOrder.Descending)
        End If
    End Sub

End Class