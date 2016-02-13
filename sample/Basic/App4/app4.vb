Public Class TCat
    Inherits TPicture
    <_Parent()> Public ParentCat As TCat = Nothing
    <_Prev()> Public PrevCat As TCat = Nothing
    Public Children As New TList(Of TCat)

    Public Overrides Sub Draw(gr As TGraphics)
        Dim ctx As CanvasRenderingContext2D = gr.Context

        If ImageIm.complete Then
            gr.save()
            ctx.beginPath()
            Dim rx = Size.X / ImageIm.width
            Dim ry = Size.Y / ImageIm.height
            Dim dx = Size.X / 2
            Dim dy = Size.Y / 2
            AbsCenter = gr.Transform.MulPoint(Center)
            gr.translate(Center.X, Center.Y)
            gr.scale(rx, ry)
            gr.rotate(Rotation)
            ctx.drawImage(ImageIm, -dx / rx, -dy / ry)
            ctx.closePath()
            gr.restore()
        End If

        For Each x In Children
            x.Draw(gr)
        Next
    End Sub

End Class

Public Class TMyApplication
    Inherits TShapeApplication
    Public CircleR As Double = 80
    Public cnt As Double = 0
    <_Weak()> Public Mama As TCat
    <_Weak()> Public Grandma As TCat
    <_Weak()> Public Kitty1 As TCat
    <_Weak()> Public Kitty2 As TCat
    <_Weak()> Public Kitty3 As TCat

    Public Overrides Sub AppInitialize()
        Dim x As Double = 100, y As Double = 100

        Grandma = New TCat()
        Grandma.ImageList.push(MakeImage("../../../img/grandma1.png"))
        Grandma.ImageList.push(MakeImage("../../../img/grandma2.png"))
        Grandma.ImageIm = Grandma.ImageList(0)
        Grandma.SetBoundingRectangle(x, y, 100, 95)
        Grandma.Velocity.X = 2
        Grandma.Velocity.Y = 2
        ShapeList.push(Grandma)
        y += 50

        Mama = New TCat()
        Mama.ImageList.push(MakeImage("../../../img/mama1.png"))
        Mama.ImageList.push(MakeImage("../../../img/mama2.png"))
        Mama.ImageIm = Mama.ImageList(0)
        Mama.SetBoundingRectangle(x, y, 100, 95)
        Mama.Velocity.X = 2
        Mama.Velocity.Y = 2
        Grandma.Children.push(Mama)
        y += 50

        Kitty1 = New TCat()
        Kitty1.ImageList.push(MakeImage("../../../img/kitty1_1.png"))
        Kitty1.ImageList.push(MakeImage("../../../img/kitty1_2.png"))
        Kitty1.ImageIm = Kitty1.ImageList(0)
        Kitty1.SetBoundingRectangle(x, y, 80, 64)
        Kitty1.Velocity.X = 2
        Kitty1.Velocity.Y = 2
        Mama.Children.push(Kitty1)
        y += 50

        Kitty2 = New TCat()
        Kitty2.ImageList.push(MakeImage("../../../img/kitty2_1.png"))
        Kitty2.ImageList.push(MakeImage("../../../img/kitty2_2.png"))
        Kitty2.ImageIm = Kitty2.ImageList(0)
        Kitty2.SetBoundingRectangle(x, y, 80, 64)
        Kitty2.Velocity.X = 2
        Kitty2.Velocity.Y = 2
        Mama.Children.push(Kitty2)
        y += 50

        Kitty3 = New TCat()
        Kitty3.ImageList.push(MakeImage("../../../img/kitty3_1.png"))
        Kitty3.ImageList.push(MakeImage("../../../img/kitty3_2.png"))
        Kitty3.ImageIm = Kitty3.ImageList(0)
        Kitty3.SetBoundingRectangle(x, y, 80, 64)
        Kitty3.Velocity.X = 2
        Kitty3.Velocity.Y = 2
        Mama.Children.push(Kitty3)
    End Sub

    Public Sub NotUsed(self As Object, app As TMyApplication)
        If TypeOf self Is TCat Then

            With CType(self, TCat)
                'If .AbsCenter.X < app.MousePosition.X Then
                '    .Velocity.X = 2
                'Else
                '    .Velocity.X = -2
                'End If

                'If .AbsCenter.Y < app.MousePosition.Y Then
                '    .Velocity.Y = 2
                'Else
                '    .Velocity.Y = -2
                'End If

                '.Center.X += .Velocity.X
                '.Center.Y += .Velocity.Y


                'If .AbsCenter.Distance(app.MousePosition) <= .Radius Then
                '    If TypeOf self Is TPicture Then
                '        .Rotation += 19 * Math.PI / 180
                '    Else
                '        .Rotation -= 19 * Math.PI / 180
                '    End If
                'End If


                'app.cnt += 1
                '.ImageIm = .ImageList(Math.Floor(app.cnt / 40) Mod 2)
                'If .PrevCat Is Nothing Then
                '    .Rotation = 0
                'Else
                '    .Rotation += 5 * Math.PI / 180
                'End If
            End With
        End If

    End Sub

    <_Invariant()> Public Overrides Sub Rule(self As Object, app As TMyApplication)
        If TypeOf self Is TCat Then
            With CType(self, TCat)
                app.cnt += 1
                .ImageIm = .ImageList(Math.Floor(app.cnt / 40) Mod 2)

                If .ParentCat Is Nothing Then
                    ' 親がいない場合

                    ' 位置はマウスの位置
                    .Center.X = app.MousePosition.X
                    .Center.Y = app.MousePosition.Y
                Else
                    ' 親がいない場合

                    ' Y位置 = 親のY位置 + 100
                    .Center.Y = .ParentCat.Center.Y + 100

                    If .PrevCat Is Nothing Then
                        ' 子の先頭の場合

                        ' X位置 = 親のX位置
                        .Center.X = .ParentCat.Center.X
                    Else
                        ' 子の先頭の場合

                        ' X位置 = 直前のX位置 + 100
                        .Center.X = .PrevCat.Center.X + 100
                    End If
                End If

            End With
        End If
    End Sub
End Class

