
Public Class TMyApplication
    Inherits TWindowApplication

    Public Overrides Sub AppInitialize()
        Dim lbl1 As New TLabel

        lbl1.Text = "こんにちは"
        lbl1.Font.EmSize = 24
        lbl1.Font.FontString = "24px 'monospace'"
        lbl1.Position.X = 200
        lbl1.Position.Y = 50
        lbl1.AutoSize = True

        lbl1.BorderWidth = 10
        ViewList.push(lbl1)

        Dim btn1 As New TButton

        btn1.Text = "はじめまして"
        btn1.Position.X = 200
        btn1.Position.Y = 100
        btn1.AutoSize = True

        btn1.BorderWidth = 10
        ViewList.push(btn1)

        Dim cnv1 As New TCanvas

        cnv1.Position.X = 200
        cnv1.Position.Y = 150
        cnv1.Width = 300
        cnv1.Height = 100

        cnv1.BorderWidth = 10
        ViewList.push(cnv1)

        Dim cnv2 As New TCanvas

        cnv2.Position.X = 200
        cnv2.Position.Y = 300
        cnv2.Width = 300
        cnv2.Height = 100

        cnv2.BorderWidth = 10
        ViewList.push(cnv2)

        Dim lbl2 As New TLabel

        lbl2.Text = "どうぞよろしく"
        lbl2.Position.X = 10
        lbl2.Position.Y = 10
        lbl2.AutoSize = True

        lbl2.BorderWidth = 10

        cnv2.Children.push(lbl2)


    End Sub

    <_Invariant()> Public Sub SizeRule(self As Object, app As TMyApplication)
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
                                Dim children_height_sum As Double, children_width_max As Double

                                If .ChildrenTVI.Count <> 0 AndAlso .Expanded Then
                                    ' 子があり、展開している場合

                                    children_height_sum = Aggregate a_ctrl In .ChildrenTVI Into Sum(a_ctrl.DesiredHeight)
                                    .ActualHeight = .MarginTop + .TextHeight + .MarginMiddleVertical * .ChildrenTVI.Count + children_height_sum + .MarginBottom

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

                Else

                    .DesiredWidth = .Width
                    .DesiredHeight = .Height
                End If

                If TypeOf .ParentControl Is TCanvas Then
                    Dim canvas As TCanvas

                    canvas = CType(.ParentControl, TCanvas)
                    If Not Double.IsNaN(.MarginLeft) Then
                        ' 左のマージンが有効の場合

                        '.Left = .MarginLeft

                        If Not Double.IsNaN(.MarginRight) Then
                            ' 右のマージンが有効の場合

                            '.ActualWidth = .ParentControl.Width - .MarginRight - .Left
                        Else
                            ' 右のマージンが無効の場合

                            '.ActualWidth = .DesiredWidth
                        End If
                    Else
                        ' 左のマージンが無効の場合

                        'Debug.Assert(Not Double.IsNaN(.MarginRight), "x margin error")

                        '.ActualWidth = .DesiredWidth

                        '.Left = .ParentControl.ActualWidth - .MarginRight - .ActualWidth
                    End If

                    If Not Double.IsNaN(.MarginTop) Then
                        ' 上のマージンが有効の場合

                        '.Top = .MarginTop

                        If Not Double.IsNaN(.MarginBottom) Then
                            ' 下のマージンが有効の場合

                            '.ActualHeight = .ParentControl.Height - .MarginBottom - .Top
                        Else
                            ' 下のマージンが無効の場合

                            '.ActualHeight = .DesiredHeight
                        End If
                    Else
                        ' 上のマージンが無効の場合

                        'Debug.Assert(Not Double.IsNaN(.MarginBottom), "y margin error")

                        '.ActualHeight = .DesiredHeight

                        '.Top = .ParentControl.ActualHeight - .MarginBottom - .ActualHeight
                    End If
                    .ActualWidth = .DesiredWidth
                    .ActualHeight = .DesiredHeight

                ElseIf TypeOf .ParentControl Is TStackPanel Then
                    Dim stack_panel As TStackPanel

                    stack_panel = CType(.ParentControl, TStackPanel)

                    Select Case stack_panel.Orientation

                        Case EOrientation.Horizontal
                            ' 水平方向に並べる場合

                            .ActualWidth = stack_panel.ChildrenScale * .DesiredWidth

                            If .Prev Is Nothing Then
                                ' 最初の場合

                                .Left = stack_panel.ClientLeft
                            Else
                                ' 最初でない場合

                                .Left = .Prev.Left + .Prev.ActualWidth + stack_panel.HorizontalPadding
                            End If
                        Case EOrientation.Vertical
                            ' 垂直方向に並べる場合

                            .ActualHeight = stack_panel.ChildrenScale * .DesiredHeight

                            If .Prev Is Nothing Then
                                ' 最初の場合

                                .Top = .ClientTop
                            Else
                                ' 最初でない場合

                                .Top = .Prev.Top + .Prev.ActualHeight + stack_panel.VerticalPadding
                            End If
                    End Select
                Else

                    .ActualWidth = .DesiredWidth
                    .ActualHeight = .DesiredHeight
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

                        If TypeOf self Is TTreeView Then

                            With CType(self, TTreeView)

                            End With
                        End If
                    End With

                ElseIf TypeOf self Is TStackPanel Then
                    With CType(self, TStackPanel)
                        Dim children_width_sum As Double, children_height_sum As Double

                        Select Case .Orientation
                            Case EOrientation.Horizontal
                                ' 水平方向に並べる場合

                                children_width_sum = Aggregate ctrl In .Children Into Sum(ctrl.DesiredWidth)

                                If children_width_sum <= .ClientWidth Then

                                    .HorizontalPadding = (.ClientWidth - children_width_sum) / (.Children.Count - 1)

                                    .ChildrenScale = 1
                                Else

                                    .HorizontalPadding = 0

                                    .ChildrenScale = .ClientWidth / children_width_sum
                                End If
                            Case EOrientation.Vertical
                                ' 垂直方向に並べる場合

                                children_height_sum = Aggregate a_ctrl In .Children Into Sum(a_ctrl.DesiredHeight)

                                If children_height_sum <= .ClientHight Then

                                    .VerticalPadding = (.ClientHight - children_height_sum) / (.Children.Count - 1)

                                    .ChildrenScale = 1
                                Else

                                    .VerticalPadding = 0

                                    .ChildrenScale = .ClientHight / children_height_sum
                                End If
                        End Select

                    End With
                ElseIf TypeOf self Is TTextBlock Then
                ElseIf TypeOf self Is TTreeView Then
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
            End With
        End If
    End Sub

    <_Invariant()> Public Sub PositionRule(self As Object, app As TMyApplication)
        If TypeOf self Is TControl Then
            With CType(self, TControl)
                If TypeOf .ParentControl Is TCanvas Then
                    Dim canvas As TCanvas = CType(.ParentControl, TCanvas)

                    If Not Double.IsNaN(.MarginLeft) Then
                        ' 左のマージンが有効の場合

                    Else
                        ' 左のマージンが無効の場合

                    End If

                    If Not Double.IsNaN(.MarginTop) Then
                        ' 上のマージンが有効の場合


                        If Not Double.IsNaN(.MarginBottom) Then
                            ' 下のマージンが有効の場合

                        Else
                            ' 下のマージンが無効の場合

                        End If
                    Else
                        ' 上のマージンが無効の場合

                    End If

                    .AbsPosition.X = .ParentControl.AbsPosition.X + .Position.X
                    .AbsPosition.Y = .ParentControl.AbsPosition.Y + .Position.Y

                ElseIf TypeOf .ParentControl Is TStackPanel Then
                    Dim stack_panel As TStackPanel = CType(.ParentControl, TStackPanel)

                    Select Case stack_panel.Orientation

                        Case EOrientation.Horizontal
                            ' 水平方向に並べる場合


                            If .Prev Is Nothing Then
                                ' 最初の場合

                            Else
                                ' 最初でない場合

                            End If
                        Case EOrientation.Vertical
                            ' 垂直方向に並べる場合

                            If .Prev Is Nothing Then
                                ' 最初の場合

                            Else
                                ' 最初でない場合

                            End If
                    End Select

                Else

                    .AbsPosition.X = .Position.X
                    .AbsPosition.Y = .Position.Y
                End If

                If TypeOf self Is TScrollView Then

                    With CType(self, TScrollView)

                        If TypeOf self Is TTreeView Then

                            With CType(self, TTreeView)

                            End With
                        End If
                    End With

                ElseIf TypeOf self Is TStackPanel Then
                    With CType(self, TStackPanel)

                        Select Case .Orientation
                            Case EOrientation.Horizontal
                                ' 水平方向に並べる場合

                            Case EOrientation.Vertical
                                ' 垂直方向に並べる場合

                        End Select

                    End With
                ElseIf TypeOf self Is TTextBlock Then
                    With CType(self, TTextBlock)

                        If TypeOf self Is TTreeViewItem Then
                            With CType(self, TTreeViewItem)

                                If .ChildrenTVI.Count <> 0 AndAlso .Expanded Then
                                    ' 子があり、展開している場合


                                    If .Prev Is Nothing Then
                                        ' 最初の場合

                                    Else
                                        ' 最初でない場合

                                    End If
                                Else
                                    ' 子がないか、折りたたまれている場合

                                End If

                            End With

                        Else
                            If .AutoSize Then

                            Else

                            End If

                        End If
                    End With

                ElseIf TypeOf self Is TTreeView Then
                    With CType(self, TTreeView)
                    End With

                ElseIf TypeOf self Is TScrollBar Then

                    With CType(self, TScrollBar)

                        Select Case .Orientation
                            Case EOrientation.Horizontal
                            Case EOrientation.Vertical
                        End Select


                        If True Then
                            ' Thumbの位置・サイズからLowValue・HighValueを求める場合

                            Select Case .Orientation
                                Case EOrientation.Horizontal

                                Case EOrientation.Vertical
                            End Select

                        Else
                            ' LowValue・HighValueからThumbの位置・サイズを求める場合


                            Select Case .Orientation
                                Case EOrientation.Horizontal


                                Case EOrientation.Vertical

                            End Select
                        End If
                    End With

                ElseIf TypeOf self Is TForm Then

                    With CType(self, TForm)

                    End With
                End If
            End With
        End If
    End Sub
End Class
