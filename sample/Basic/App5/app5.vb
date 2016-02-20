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
        Kitty1.SetBoundingRectangle(0.5 * Size.X, 0.5 * Size.Y, 80, 64)
        Kitty1.Velocity.X = 2
        Kitty1.Velocity.Y = 2
        Mama.Children.push(Kitty1)
        y += 50

        Kitty2 = New TCat()
        Kitty2.ImageList.push(MakeImage("../../../img/kitty2_1.png"))
        Kitty2.ImageList.push(MakeImage("../../../img/kitty2_2.png"))
        Kitty2.ImageIm = Kitty2.ImageList(0)
        Kitty2.SetBoundingRectangle(0.5 * Size.X, 0.5 * Size.Y, 80, 64)
        Kitty2.Velocity.X = 2
        Kitty2.Velocity.Y = 2
        Mama.Children.push(Kitty2)
        y += 50

        Kitty3 = New TCat()
        Kitty3.ImageList.push(MakeImage("../../../img/kitty3_1.png"))
        Kitty3.ImageList.push(MakeImage("../../../img/kitty3_2.png"))
        Kitty3.ImageIm = Kitty3.ImageList(0)
        Kitty3.SetBoundingRectangle(0.5 * Size.X, 0.5 * Size.Y, 80, 64)
        Kitty3.Velocity.X = 2
        Kitty3.Velocity.Y = 2
        Mama.Children.push(Kitty3)
    End Sub

    <_Invariant()> Public Overrides Sub Rule(self As Object, app As TMyApplication)
        If TypeOf self Is TCat Then
            With CType(self, TCat)

                app.cnt += 1
                .ImageIm = .ImageList(Math.Floor(app.cnt / 40) Mod 2)

                If .Children.length = 0 Then
                    ' 子供がない場合

                    If .Prev Is Nothing Then
                        ' 先頭の場合

                        ' 位置はマウスの位置
                        .Center.X = app.MousePosition.X
                        .Center.Y = app.MousePosition.Y
                    Else
                        ' 先頭でない場合

                        ' X位置 = 直前のX位置 + 100
                        .Center.X = .PrevCat.Center.X + 100

                        ' Y位置 = 直前のY位置
                        .Center.Y = .PrevCat.Center.Y
                    End If
                Else
                    ' 子供がある場合

                    ' X位置 = 子供のX位置の平均値
                    .Center.X = Aggregate x In .Children Into Average(x.Center.X)

                    ' Y位置 = 子供のY位置の平均値 - 100
                    .Center.Y = (Aggregate x In .Children Into Average(x.Center.Y)) - 100
                End If

                .Rotation = 0
            End With
        End If
    End Sub
End Class


Public Class TNode
    Public Parent As TNode
    Public Prev As TNode

    Public Data As Integer
End Class

Public Class TSample
    Public Function C() As Integer
        Return 0
    End Function

    Public Function F(x As Integer) As Integer
        Return 0
    End Function

    Public Function G(x As Integer, y As Integer) As Integer
        Return 0
    End Function

    Public Sub Rule(self As Object, app As TMyApplication)
        If TypeOf self Is TNode Then
            With CType(self, TNode)
                If .Parent Is Nothing Then
                    ' ルートの場合

                    ' Dataの値は他のノードの値に依存せずに決まる。
                    .Data = C()
                Else
                    ' ルートでない場合

                    If .Prev Is Nothing Then
                        ' 子の最初の場合

                        ' Dataの値は、親のDataの値に依存する。
                        .Data = F(.Parent.Data)
                    Else
                        ' 子の最初でない場合

                        ' Dataの値は、親または直前のDataの値に依存する。
                        .Data = G(.Parent.Data, .Prev.Data)
                    End If
                End If
            End With
        End If
    End Sub
End Class

Public Class A
    Public Overridable Sub Rule(self As Object, app As TMyApplication)
        ' Aクラスの処理
    End Sub
End Class

Public Class B
    Inherits A
End Class

Public Class C
    Inherits A

    Public Overrides Sub Rule(self As Object, app As TMyApplication)
        ' Aクラスの処理
        ' Cクラスの処理
    End Sub
End Class
Public Class D
    Inherits C

    Public Overrides Sub Rule(self As Object, app As TMyApplication)
        ' Aクラスの処理
        ' Cクラスの処理
        ' Dクラスの処理
    End Sub
End Class
Public Class E
    Inherits C

    Public Overrides Sub Rule(self As Object, app As TMyApplication)
        If TypeOf self Is A Then
            With CType(self, A)
                ' Aクラスの処理
                If TypeOf self Is B Then
                    With CType(self, B)
                        ' Bクラスの処理
                    End With
                ElseIf TypeOf self Is C Then
                    With CType(self, C)
                        ' Cクラスの処理
                        If TypeOf self Is D Then
                            With CType(self, D)
                                ' Dクラスの処理
                            End With
                        ElseIf TypeOf self Is E Then
                            With CType(self, E)
                                ' Eクラスの処理
                            End With
                        End If
                    End With
                End If
            End With
        End If
    End Sub

End Class
