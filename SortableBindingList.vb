Imports System.Collections.Generic
Imports System.ComponentModel

Public Class SortableBindingList(Of T)
    Inherits BindingList(Of T)

    Private _isSorted As Boolean
    Private _sortDirection As ListSortDirection
    Private _sortProperty As PropertyDescriptor

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(list As IList(Of T))
        MyBase.New(list)
    End Sub

    Protected Overrides ReadOnly Property SupportsSortingCore() As Boolean
        Get
            Return True
        End Get
    End Property

    Protected Overrides ReadOnly Property IsSortedCore() As Boolean
        Get
            Return _isSorted
        End Get
    End Property

    Protected Overrides ReadOnly Property SortDirectionCore() As ListSortDirection
        Get
            Return _sortDirection
        End Get
    End Property

    Protected Overrides ReadOnly Property SortPropertyCore() As PropertyDescriptor
        Get
            Return _sortProperty
        End Get
    End Property

    Public Sub ApplySort(propertyName As String, direction As ListSortDirection)
        Dim prop As PropertyDescriptor = TypeDescriptor.GetProperties(GetType(T))(propertyName)
        If prop Is Nothing Then
            Throw New ArgumentException($"Property {propertyName} not found")
        End If
        ApplySortCore(prop, direction)
    End Sub

    Protected Overrides Sub ApplySortCore(prop As PropertyDescriptor, direction As ListSortDirection)
        Dim items As List(Of T) = DirectCast(Me.Items, List(Of T))

        If items IsNot Nothing Then
            Dim pc As New PropertyComparer(Of T)(prop, direction)
            items.Sort(pc)
            _isSorted = True
            _sortDirection = direction
            _sortProperty = prop
        Else
            _isSorted = False
        End If

        Me.OnListChanged(New ListChangedEventArgs(ListChangedType.Reset, -1))
    End Sub

    Protected Overrides Sub RemoveSortCore()
        _isSorted = False
        _sortProperty = Nothing
    End Sub

    Private Class PropertyComparer(Of T2)
        Implements IComparer(Of T2)

        Private ReadOnly _property As PropertyDescriptor
        Private ReadOnly _direction As ListSortDirection

        Public Sub New(propertyDescriptor As PropertyDescriptor, direction As ListSortDirection)
            _property = propertyDescriptor
            _direction = direction
        End Sub

        Public Function Compare(x As T2, y As T2) As Integer Implements IComparer(Of T2).Compare
            Dim valueX = _property.GetValue(x)
            Dim valueY = _property.GetValue(y)

            If valueX Is Nothing AndAlso valueY Is Nothing Then
                Return 0
            ElseIf valueX Is Nothing Then
                Return If(_direction = ListSortDirection.Ascending, -1, 1)
            ElseIf valueY Is Nothing Then
                Return If(_direction = ListSortDirection.Ascending, 1, -1)
            End If

            If TypeOf valueX Is IComparable Then
                Return If(_direction = ListSortDirection.Ascending,
                         DirectCast(valueX, IComparable).CompareTo(valueY),
                         DirectCast(valueY, IComparable).CompareTo(valueX))
            End If

            If valueX.Equals(valueY) Then
                Return 0
            Else
                Return If(_direction = ListSortDirection.Ascending,
                         valueX.ToString().CompareTo(valueY.ToString()),
                         valueY.ToString().CompareTo(valueX.ToString()))
            End If
        End Function
    End Class
End Class