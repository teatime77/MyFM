
Public Class 売上明細
    <_Parent()> Public 親 As 売上伝票
    <_Prev()> Public 兄 As 売上明細
    Public 明細番号 As Integer
    Public 単価 As Integer
    Public 数量 As Integer
    Public 小計 As Integer
End Class

Public Class 売上伝票
    Public 売上明細リスト As New TList(Of 売上明細)
    Public 合計 As Integer
End Class

Public Class 売上明細View
    Inherits TControl
    <_Parent()> Public 親 As 売上伝票
    <_Prev()> Public 兄 As 売上明細
    <_Weak()> Public Data As 売上明細
    Public 明細番号 As New TLabel
    Public 単価 As New TLabel
    Public 数量 As New TLabel
    Public 小計 As New TLabel

    Public Sub New(data1 As 売上明細)
        Data = data1
    End Sub
End Class

Public Class 売上伝票View
    Inherits TControl
    <_Weak()> Public Data As 売上伝票
    Public 売上明細リストView As New TStackPanel
    Public 合計 As TLabel
End Class

Public Class TMyApplication
    Inherits TWindowApplication

    Public Overrides Sub AppInitialize()
    End Sub

    <_Invariant()> Public Sub ViewRule(self As Object, app As TMyApplication)
        If TypeOf self Is 売上明細View Then
            With CType(self, 売上明細View)
                .明細番号.Text = String.Format(.Data.明細番号)
                .単価.Text = .Data.単価.ToString()
                .数量.Text = .Data.数量.ToString()
                .小計.Text = .Data.小計.ToString()
            End With

        ElseIf TypeOf self Is 売上伝票View Then
            With CType(self, 売上伝票View)
                .売上明細リストView.Children = New TList(Of TControl)(From x In .Data.売上明細リスト Select New 売上明細View(x))
                .合計.Text = .Data.合計.ToString()
            End With
        End If
    End Sub
End Class
