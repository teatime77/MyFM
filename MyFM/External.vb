Imports System.IO
Imports System.Xml.Serialization
Imports System.Text
Imports System.Diagnostics

Partial Public Class TMap(Of T, U)
    Default Public ReadOnly Property Item(key As T) As List(Of U)
        Get
            Return dt(key)
        End Get
    End Property
End Class

Partial Public Class Sys
    Public Shared Iterator Function Union(Of T)(v1 As IEnumerable(Of T), v2 As IEnumerable(Of T)) As IEnumerable(Of T)
        For Each x In v1
            Yield x
        Next

        For Each x In v2
            If Not v1.Contains(x) Then

                Yield x
            End If
        Next
    End Function
End Class

Public Class TExternal
    Public Shared Sub SetParent(self As Object, _Parent As Object)
        If TypeOf self Is TProject Then
            CType(self, TProject).__SetParent(self, _Parent)
        ElseIf TypeOf self Is TFunction Then
            CType(self, TFunction).__SetParent(self, _Parent)
        Else
            Debug.Assert(False)
        End If
    End Sub
End Class
