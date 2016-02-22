﻿
Public Class TMyApplication
    Inherits TWindowApplication

    Public Overrides Sub AppInitialize()

        '---------------------------------------- 垂直スタックパネル
        Dim vstc As New TStackPanel

        vstc.Position.X = 10
        vstc.Position.Y = 230
        vstc.Width = 300
        vstc.Height = 100
        vstc.Orientation = EOrientation.Vertical

        ViewList.push(vstc)

        '------------------------------ ラベル
        Dim lbl4 As New TLabel

        lbl4.Text = "さよなら"
        lbl4.AutoSize = True

        vstc.Children.push(lbl4)

        '------------------------------ ラベル
        Dim lbl5 As New TLabel

        lbl5.Text = "こんばんは"
        lbl5.AutoSize = True

        vstc.Children.push(lbl5)

        '---------------------------------------- 水平スタックパネル
        Dim hstc As New TStackPanel

        hstc.Position.X = 10
        hstc.Position.Y = 340
        hstc.Width = 300
        hstc.Height = 100
        hstc.Orientation = EOrientation.Horizontal

        ViewList.push(hstc)

        '------------------------------ ラベル
        Dim lbl6 As New TLabel

        lbl6.Text = "おはよう"
        lbl6.AutoSize = True

        hstc.Children.push(lbl6)

        '------------------------------ ラベル
        Dim lbl7 As New TLabel

        lbl7.Text = "元気ですか"
        lbl7.AutoSize = True

        hstc.Children.push(lbl7)

        '---------------------------------------- キャンバス
        Dim cnv1 As New TCanvas

        cnv1.Position.X = 10
        cnv1.Position.Y = 10
        cnv1.Width = 300
        cnv1.Height = 100

        ViewList.push(cnv1)

        '------------------------------ ボタン
        Dim btn1 As New TButton

        btn1.Text = "はじめまして"
        btn1.MarginLeft = 10
        btn1.MarginTop = 10
        btn1.AutoSize = True

        cnv1.Children.push(btn1)

        '------------------------------ ラベル
        Dim lbl1 As New TLabel

        lbl1.Text = "こんにちは"
        lbl1.Font.EmSize = 24
        lbl1.Font.FontString = "24px 'monospace'"
        lbl1.MarginRight = 10
        lbl1.MarginBottom = 10
        lbl1.Position.X = 200
        lbl1.Position.Y = 20
        lbl1.AutoSize = True

        cnv1.Children.push(lbl1)

        '---------------------------------------- キャンバス
        Dim cnv2 As New TCanvas

        cnv2.Position.X = 10
        cnv2.Position.Y = 120
        cnv2.Width = 300
        cnv2.Height = 100

        ViewList.push(cnv2)

        '------------------------------ ラベル
        Dim lbl2 As New TLabel

        lbl2.Text = "どうぞよろしく"
        lbl2.MarginLeft = 20
        lbl2.MarginRight = 100
        lbl2.Position.Y = 10
        lbl2.AutoSize = True

        cnv2.Children.push(lbl2)

        '------------------------------ ラベル
        Dim lbl3 As New TLabel

        lbl3.Text = "またね"
        lbl3.MarginTop = 20
        lbl3.MarginBottom = 20
        lbl3.MarginRight = 10
        lbl3.AutoSize = True

        cnv2.Children.push(lbl3)

    End Sub

    <_Invariant()> Public Sub DesiredSizeRule(self As Object, app As TMyApplication)
        If TypeOf self Is TControl Then
            With CType(self, TControl)
                If TypeOf self Is TTextBlock Then
                    With CType(self, TTextBlock)
                        Dim sz As TPoint

                        sz = app.Graphics.MeasureText(.Font, .Text)
                        .TextWidth = sz.X
                        .TextHeight = sz.Y

                        If TypeOf self Is TTreeViewItem Then
                            With CType(self, TTreeViewItem)

                                .DesiredWidth = .Width
                                .DesiredHeight = .Height
                            End With
                        Else
                            If .AutoSize Then

                                .DesiredWidth = .LeftPadding + .TextWidth + .RightPadding
                                .DesiredHeight = .TextHeight
                            Else

                                .DesiredWidth = .Width
                                .DesiredHeight = .Height
                            End If
                        End If
                    End With

                ElseIf TypeOf self Is TStackPanel Then
                    With CType(self, TStackPanel)
                        Select Case .Orientation
                            Case EOrientation.Horizontal
                                ' 水平方向に並べる場合

                                .DesiredWidth = Aggregate ctrl In .Children Into Sum(ctrl.DesiredWidth)
                                .DesiredHeight = .Height

                            Case EOrientation.Vertical
                                ' 垂直方向に並べる場合

                                .DesiredWidth = .Width
                                .DesiredHeight = Aggregate a_ctrl In .Children Into Sum(a_ctrl.DesiredHeight)
                        End Select
                    End With

                Else
                    .DesiredWidth = .Width
                    .DesiredHeight = .Height
                End If
            End With
        End If
    End Sub

    <_Invariant()> Public Sub ActualSizeRule(self As Object, app As TMyApplication)
        If TypeOf self Is TControl Then
            With CType(self, TControl)

                If TypeOf .ParentControl Is TCanvas Then

                    If Not Double.IsNaN(.MarginLeft) AndAlso Not Double.IsNaN(.MarginRight) Then
                        ' 左右のマージンが有効の場合

                        .ActualWidth = .ParentControl.ActualWidth - (.MarginLeft + .MarginRight)
                    Else
                        ' 左右のマージンが有効でないの場合

                        .ActualWidth = .DesiredWidth
                    End If

                    If Not Double.IsNaN(.MarginTop) AndAlso Not Double.IsNaN(.MarginBottom) Then
                        ' 上下のマージンが有効の場合

                        .ActualHeight = .ParentControl.ActualHeight - (.MarginTop + .MarginBottom)
                    Else
                        ' 上下のマージンが有効でない場合

                        .ActualHeight = .DesiredHeight
                    End If

                ElseIf TypeOf .ParentControl Is TStackPanel Then
                    Dim stack_panel As TStackPanel

                    stack_panel = CType(.ParentControl, TStackPanel)

                    Select Case stack_panel.Orientation
                        Case EOrientation.Horizontal
                            ' 水平方向に並べる場合

                            .ActualWidth = stack_panel.ChildrenScale * .DesiredWidth
                            .ActualHeight = .ParentControl.ActualHeight

                        Case EOrientation.Vertical
                            ' 垂直方向に並べる場合

                            .ActualWidth = .ParentControl.ActualWidth
                            .ActualHeight = stack_panel.ChildrenScale * .DesiredHeight
                    End Select
                Else

                    .ActualWidth = .DesiredWidth
                    .ActualHeight = .DesiredHeight
                End If

                If TypeOf self Is TStackPanel Then
                    With CType(self, TStackPanel)
                        Select Case .Orientation
                            Case EOrientation.Horizontal
                                ' 水平方向に並べる場合

                                If .DesiredWidth <= .ActualWidth Then

                                    If .Children.length <= 1 Then
                                        .HorizontalPadding = 0
                                    Else
                                        .HorizontalPadding = (.ActualWidth - .DesiredWidth) / (.Children.length - 1)
                                    End If

                                    .ChildrenScale = 1
                                Else

                                    .HorizontalPadding = 0

                                    .ChildrenScale = .ActualWidth / .DesiredWidth
                                End If
                            Case EOrientation.Vertical
                                ' 垂直方向に並べる場合

                                If .DesiredHeight <= .ActualHeight Then

                                    If .Children.length <= 1 Then
                                        .VerticalPadding = 0
                                    Else
                                        .VerticalPadding = (.ActualHeight - .DesiredHeight) / (.Children.length - 1)
                                    End If

                                    .ChildrenScale = 1
                                Else

                                    .VerticalPadding = 0

                                    .ChildrenScale = .ActualHeight / .DesiredHeight
                                End If
                        End Select
                    End With
                End If
            End With
        End If
    End Sub

    <_Invariant()> Public Sub PositionRule(self As Object, app As TMyApplication)
        If TypeOf self Is TControl Then
            With CType(self, TControl)
                If TypeOf .ParentControl Is TCanvas Then
                    Dim canvas As TCanvas = CType(.ParentControl, TCanvas)
                    Dim x As Double, y As Double

                    If Not Double.IsNaN(.MarginLeft) Then
                        ' 左のマージンが有効の場合

                        x = .MarginLeft

                    ElseIf Not Double.IsNaN(.MarginRight) Then
                        ' 右のマージンが有効の場合

                        x = .ParentControl.ActualWidth - (.ActualWidth + .MarginRight)
                    Else
                        ' 左右のマージンが無効の場合

                        x = .Position.X
                    End If

                    .AbsPosition.X = .ParentControl.AbsPosition.X + x

                    If Not Double.IsNaN(.MarginTop) Then
                        ' 上のマージンが有効の場合

                        y = .MarginTop

                    ElseIf Not Double.IsNaN(.MarginBottom) Then
                        ' 下のマージンが有効の場合

                        y = .ParentControl.ActualHeight - (.ActualHeight + .MarginBottom)
                    Else
                        ' 上下のマージンが無効の場合

                        y = .Position.Y
                    End If

                    .AbsPosition.Y = .ParentControl.AbsPosition.Y + y

                ElseIf TypeOf .ParentControl Is TStackPanel Then
                    Dim stack_panel As TStackPanel = CType(.ParentControl, TStackPanel)

                    Select Case stack_panel.Orientation
                        Case EOrientation.Horizontal
                            ' 水平方向に並べる場合

                            If .Prev Is Nothing Then
                                ' 最初の場合

                                .AbsPosition.X = .ParentControl.AbsPosition.X
                            Else
                                ' 最初でない場合

                                .AbsPosition.X = .Prev.AbsPosition.X + .Prev.ActualWidth + stack_panel.HorizontalPadding
                            End If

                            .AbsPosition.Y = .ParentControl.AbsPosition.Y

                        Case EOrientation.Vertical
                            ' 垂直方向に並べる場合

                            .AbsPosition.X = .ParentControl.AbsPosition.X

                            If .Prev Is Nothing Then
                                ' 最初の場合

                                .AbsPosition.Y = .ParentControl.AbsPosition.Y
                            Else
                                ' 最初でない場合

                                .AbsPosition.Y = .Prev.AbsPosition.Y + .Prev.ActualHeight + stack_panel.VerticalPadding
                            End If
                    End Select
                Else

                    .AbsPosition.X = .Position.X
                    .AbsPosition.Y = .Position.Y
                End If

            End With
        End If
    End Sub

    Public Sub NotUsedRule(self As Object, app As TMyApplication)
        If TypeOf self Is TTreeViewItem Then
            With CType(self, TTreeViewItem)
                Dim children_desired_height_sum As Double, children_width_max As Double

                If .ChildrenTVI.Count <> 0 AndAlso .Expanded Then
                    ' 子があり、展開している場合

                    children_desired_height_sum = Aggregate a_ctrl In .ChildrenTVI Into Sum(a_ctrl.DesiredHeight)
                    .ActualHeight = .MarginTop + .TextHeight + .MarginMiddleVertical * .ChildrenTVI.Count + children_desired_height_sum + .MarginBottom

                    children_width_max = Aggregate a_ctrl In .ChildrenTVI Into Max(a_ctrl.ActualWidth)
                    .ActualWidth = Math.Max(.TextWidth, children_width_max)

                    .Left = .Indent

                    If .Prev Is Nothing Then
                        ' 最初の場合

                        .Top = .ParentControl.ClientTop + .TextHeight
                    Else
                        ' 最初でない場合

                        .Top = .Prev.Top + .Prev.ActualHeight + .MarginMiddleVertical
                    End If
                Else
                    ' 子がないか、折りたたまれている場合

                    .ActualHeight = .MarginTop + .TextHeight + .MarginBottom

                    .ActualWidth = .TextWidth
                End If
            End With
        End If

        If TypeOf self Is TScrollView Then

            With CType(self, TScrollView)
                Dim box_size As Double = 10

                .HorizontalScrollBar.Left = 0
                .HorizontalScrollBar.Top = .Height - box_size

                .HorizontalScrollBar.Width = .Width
                .HorizontalScrollBar.Height = box_size

                .HorizontalScrollBar.Minimum = 0
                .HorizontalScrollBar.Maximum = .ContentWidth

                .VerticalScrollBar.Left = .Width - box_size
                .VerticalScrollBar.Top = 0

                .VerticalScrollBar.Width = box_size
                .VerticalScrollBar.Height = .Height

                .VerticalScrollBar.Minimum = 0
                .VerticalScrollBar.Maximum = .ContentHeight

                .ViewOffsetX = .HorizontalScrollBar.LowValue
                .ViewOffsetY = .VerticalScrollBar.LowValue

            End With

        End If

        If TypeOf self Is TTreeView Then

            With CType(self, TTreeView)

            End With
        End If

        If TypeOf self Is TTreeView Then
            With CType(self, TTreeView)
                .ContentWidth = .Root.ActualWidth
                .ContentHeight = .Root.ActualHeight
            End With

        ElseIf TypeOf self Is TScrollBar Then

            With CType(self, TScrollBar)
                Dim button_size As Double, movable_size As Double, thumb_pos As Double, thumb_size As Double

                Select Case .Orientation
                    Case EOrientation.Horizontal
                        button_size = .Height
                        movable_size = .ActualWidth - 2 * button_size

                        .NextButton.Left = .Width - button_size
                        .NextButton.Top = 0

                        .Thumb.Top = 0
                        .Thumb.Height = button_size
                    Case EOrientation.Vertical
                        button_size = .Width
                        movable_size = .ActualHeight - 2 * button_size

                        .NextButton.Left = 0
                        .NextButton.Top = .Height - button_size

                        .Thumb.Left = 0
                        .Thumb.Width = button_size
                End Select

                .PrevButton.Width = button_size
                .PrevButton.Height = button_size

                .NextButton.Width = button_size
                .NextButton.Height = button_size

                .PrevButton.Left = 0
                .PrevButton.Top = 0

                If True Then
                    ' Thumbの位置・サイズからLowValue・HighValueを求める場合

                    Select Case .Orientation
                        Case EOrientation.Horizontal
                            thumb_pos = .Thumb.Left - button_size
                            thumb_size = .Thumb.Width

                        Case EOrientation.Vertical
                            thumb_pos = .Thumb.Top - button_size
                            thumb_size = .Thumb.Height
                    End Select

                    .LowValue = .Minimum + (.Maximum - .Minimum) * thumb_pos / movable_size
                    .HighValue = .LowValue + (.Maximum - .Minimum) * thumb_size / movable_size
                Else
                    ' LowValue・HighValueからThumbの位置・サイズを求める場合

                    thumb_pos = movable_size * (.LowValue - .Minimum) * (.Maximum - .Minimum)
                    thumb_size = movable_size * (.HighValue - .LowValue) / (.Maximum - .Minimum)

                    Select Case .Orientation
                        Case EOrientation.Horizontal

                            .Thumb.Left = button_size + thumb_pos
                            .Thumb.Width = thumb_size

                        Case EOrientation.Vertical

                            .Thumb.Top = button_size + thumb_pos
                            .Thumb.Height = thumb_size
                    End Select
                End If
            End With

        ElseIf TypeOf self Is TForm Then

            With CType(self, TForm)

            End With
        End If
    End Sub
End Class
