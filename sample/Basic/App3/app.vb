Public Class TMyApplication
    Inherits TShapeApplication
    Public CircleR As Double = 80
    Public cnt As Double = 0
    <_Weak()> Public Mama As TPicture
    <_Weak()> Public Grandma As TPicture
    <_Weak()> Public Kitty1 As TPicture
    <_Weak()> Public Kitty2 As TPicture
    <_Weak()> Public Kitty3 As TPicture

    Public Overrides Sub AppInitialize()
        Dim x As Double = 100, y As Double = 100

        Mama = New TPicture()
        Mama.ImageList.push(MakeImage("../../../img/mama1.png"))
        Mama.ImageList.push(MakeImage("../../../img/mama2.png"))
        Mama.ImageIm = Mama.ImageList(0)
        Mama.SetBoundingRectangle(x, y, 100, 95)
        Mama.Velocity.X = 2
        Mama.Velocity.Y = 2
        ShapeList.push(Mama)
        y += 50

        Grandma = New TPicture()
        Grandma.ImageList.push(MakeImage("../../../img/grandma1.png"))
        Grandma.ImageList.push(MakeImage("../../../img/grandma2.png"))
        Grandma.ImageIm = Grandma.ImageList(0)
        Grandma.SetBoundingRectangle(x, y, 100, 95)
        Grandma.Velocity.X = 2
        Grandma.Velocity.Y = 2
        ShapeList.push(Grandma)
        y += 50

        Kitty1 = New TPicture()
        Kitty1.ImageList.push(MakeImage("../../../img/kitty1_1.png"))
        Kitty1.ImageList.push(MakeImage("../../../img/kitty1_2.png"))
        Kitty1.ImageIm = Kitty1.ImageList(0)
        Kitty1.SetBoundingRectangle(x, y, 100, 80)
        Kitty1.Velocity.X = 2
        Kitty1.Velocity.Y = 2
        ShapeList.push(Kitty1)
        y += 50

        Kitty2 = New TPicture()
        Kitty2.ImageList.push(MakeImage("../../../img/kitty2_1.png"))
        Kitty2.ImageList.push(MakeImage("../../../img/kitty2_2.png"))
        Kitty2.ImageIm = Kitty2.ImageList(0)
        Kitty2.SetBoundingRectangle(x, y, 100, 80)
        Kitty2.Velocity.X = 2
        Kitty2.Velocity.Y = 2
        ShapeList.push(Kitty2)
        y += 50

        Kitty3 = New TPicture()
        Kitty3.ImageList.push(MakeImage("../../../img/kitty3_1.png"))
        Kitty3.ImageList.push(MakeImage("../../../img/kitty3_2.png"))
        Kitty3.ImageIm = Kitty3.ImageList(0)
        Kitty3.SetBoundingRectangle(x, y, 100, 83)
        Kitty3.Velocity.X = 2
        Kitty3.Velocity.Y = 2
        ShapeList.push(Kitty3)
        y += 50

        Dim rc1 = New TRectangle()
        rc1.SetBoundingRectangle(x, y, 80, 40)
        rc1.Velocity.X = 1
        rc1.Velocity.Y = 1
        rc1.BackgroundColor = "rgb(192, 80, 77)"
        rc1.BorderColor = "#0000FF"
        rc1.BorderWidth = 10
        ShapeList.push(rc1)
        y += 50

        Dim rc2 = New TRectangle()
        rc2.SetBoundingRectangle(x, y, 80, 40)
        rc2.Velocity.X = 2
        rc2.Velocity.Y = 2
        rc2.BorderColor = "#0000FF"
        rc2.BorderWidth = 10
        ShapeList.push(rc2)
        Dim ell1 = New TEllipse()
        ell1.SetBoundingRectangle(x + 5, y + 5, 70, 30)
        ell1.Velocity.X = 3
        ell1.Velocity.Y = 3
        ell1.BackgroundColor = "#FF0000"
        ell1.BorderColor = "#0000FF"
        ell1.BorderWidth = 10
        ShapeList.push(ell1)
        y += 50

        Dim txt1 = New TLabel()
        txt1.SetBoundingRectangle(x, y, 80, 40)
        txt1.Velocity.X = 4
        txt1.Velocity.Y = 4
        txt1.BackgroundColor = "rgb(192, 80, 77)"
        txt1.BorderColor = "#0000FF"
        txt1.BorderWidth = 10
        txt1.TextColor = "#00FF00"
        txt1.Text = "こんにちは4"
        ShapeList.push(txt1)
        y += 50

        Dim razania = New TPicture()
        razania.Load("../img/food_lasagna_razania.png")
        razania.SetBoundingRectangle(x, y, 100, 741 / 8.0)
        razania.Velocity.X = 2
        razania.Velocity.Y = 2
        ShapeList.push(razania)
        y += 50

        Dim pizza = New TPicture()
        pizza.Load("../img/food_pizza_takuhai.png")
        pizza.SetBoundingRectangle(x, y, 100, 712 / 8.0)
        pizza.Velocity.X = 2
        pizza.Velocity.Y = 2
        ShapeList.push(pizza)
        y += 50

        Dim grp = New TGroup()
        grp.SetBoundingRectangle(x, y, 100, 100)
        grp.Velocity.X = 2
        grp.Velocity.Y = 2
        ShapeList.push(grp)

        Dim rc3 = New TRectangle()
        rc3.SetBoundingRectangle(-2, -2, 50, 50)
        rc3.Velocity.X = 1
        rc3.Velocity.Y = 1
        rc3.BackgroundColor = "rgb(0, 255, 0)"
        rc3.BorderColor = "#0000FF"
        rc3.BorderWidth = 10
        rc3.Parent = grp
        grp.Children.push(rc3)

        Dim txt2 = New TLabel()
        txt2.SetBoundingRectangle(2, 2, 50, 50)
        txt2.Velocity.X = 4
        txt2.Velocity.Y = 4
        txt2.BackgroundColor = "rgb(192, 80, 77)"
        txt2.BorderColor = "#0000FF"
        txt2.BorderWidth = 10
        txt2.TextColor = "#FF0000"
        txt2.Text = "今日"
        txt2.Parent = grp
        grp.Children.push(txt2)
    End Sub

    <_Invariant()> Public Overrides Sub Rule(self As Object, app As TMyApplication)
        If TypeOf self Is TShape Then
            With CType(self, TShape)
                If .Center.X - .Radius < 0 OrElse app.Size.X < .Center.X + .Radius Then
                    .Velocity.X = - .Velocity.X
                End If
                If .Center.Y - .Radius < 0 OrElse app.Size.Y < .Center.Y + .Radius Then
                    .Velocity.Y = - .Velocity.Y
                End If
                .Center.X += .Velocity.X
                .Center.Y += .Velocity.Y

                If .Parent IsNot Nothing AndAlso TypeOf .Parent Is TShape Then

                    Dim p As Double = CType(.Parent, TShape).Test.Position.X
                Else

                    .Test.Position = New TPoint()
                End If

                If .AbsCenter.Distance(app.MousePosition) <= .Radius Then
                    If TypeOf self Is TPicture Then
                        .Rotation += 19 * Math.PI / 180
                    Else
                        .Rotation -= 19 * Math.PI / 180
                    End If
                Else
                    .Rotation += 5 * Math.PI / 180
                End If

                If TypeOf self Is TPicture Then
                    With CType(self, TPicture)
                        app.cnt += 1
                        If self Is app.Mama OrElse self Is app.Grandma OrElse self Is app.Kitty1 OrElse self Is app.Kitty2 OrElse self Is app.Kitty3 Then
                            .ImageIm = .ImageList(Math.Floor(app.cnt / 40) Mod 2)
                            .Rotation = 0
                        End If
                        'console.log("pos:" + .AbsCenter.X + " " + .AbsCenter.Y + " " + .ImageIm.src)
                        If .ImageIm.src = "http://localhost:17623/img/circle.png" Then
                            .Size.X = app.CircleR + 20 * Math.Sin(Math.PI * app.cnt / 180)
                            .Size.Y = app.CircleR + 20 * Math.Cos(Math.PI * app.cnt / 180)
                        End If

                    End With

                ElseIf TypeOf self Is TGroup Then
                    With CType(self, TGroup)
                        Dim v1 = (From x In .Children Select x.Center.X).ToArray()
                        'console.log("Center X:" + v1)

                        Dim vshape = From x In .Children Where TypeOf x Is TPicture
                        .BoundingRectangle.Position.X = Aggregate img In vshape Into Min(img.BoundingRectangle.Position.X)
                        Dim vx = From x In .Children Where TypeOf x Is TPicture Select x.BoundingRectangle.Position.X
                        .BoundingRectangle.Position.X = Aggregate x In vx Into Min(x)
                        .BoundingRectangle.Position.X = Aggregate img In (From x In .Children Where TypeOf x Is TPicture) Into Min(img.BoundingRectangle.Position.X)
                        .BoundingRectangle.Position.X = Aggregate img In .Children Where TypeOf img Is TPicture Into Min(img.BoundingRectangle.Position.X)

                    End With
                End If

            End With
        End If
    End Sub
End Class
