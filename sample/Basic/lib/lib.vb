Imports System.Xml.Serialization

Public Class TPoint
    Public X As Double = 0
    Public Y As Double = 0

    Function Distance(p As TPoint) As Double
        Dim dx = p.X - X
        Dim dy = p.Y - Y
        Return Math.Sqrt(dx * dx + dy * dy)
    End Function
End Class

Public Class TMat2D
    Public m11 As Double = 1
    Public m12 As Double = 0
    Public m21 As Double = 0
    Public m22 As Double = 1
    Public dx As Double = 0
    Public dy As Double = 0

    Function Copy() As TMat2D
        Dim A As New TMat2D
        A.m11 = m11
        A.m12 = m12
        A.m21 = m21
        A.m22 = m22
        A.dx = dx
        A.dy = dy
        Return A
    End Function

    Function Mul(A As TMat2D) As TMat2D
        Dim B As New TMat2D
        B.m11 = m11 * A.m11 + m21 * A.m12
        B.m21 = m11 * A.m21 + m21 * A.m22
        B.dx = m11 * A.dx + m21 * A.dy + dx
        B.m12 = m12 * A.m11 + m22 * A.m12
        B.m22 = m12 * A.m21 + m22 * A.m22
        B.dy = m12 * A.dx + m22 * A.dy + dy
        Return B
    End Function

    Function MulPoint(p1 As TPoint) As TPoint
        Dim p2 As New TPoint
        p2.X = m11 * p1.X + m21 * p1.Y + dx
        p2.Y = m12 * p1.X + m22 * p1.Y + dy
        Return p2
    End Function

    Sub transform(A As TMat2D)
        Dim m11_ As Double, m12_ As Double, m21_ As Double, m22_ As Double, dx_ As Double, dy_ As Double
        m11_ = m11 * A.m11 + m21 * A.m12
        m21_ = m11 * A.m21 + m21 * A.m22
        dx_ = m11 * A.dx + m21 * A.dy + dx
        m12_ = m12 * A.m11 + m22 * A.m12
        m22_ = m12 * A.m21 + m22 * A.m22
        dy_ = m12 * A.dx + m22 * A.dy + dy
        m11 = m11_
        m12 = m12_
        m21 = m21_
        m22 = m22_
        dx = dx_
        dy = dy_
    End Sub

    Sub scale(x As Double, y As Double)
        m11 *= x
        m21 *= y
        m12 *= x
        m22 *= y
    End Sub

    Sub translate(x As Double, y As Double)
        dx += m11 * x + m21 * y
        dy += m12 * x + m22 * y
    End Sub

    Sub rotate(r As Double)
        Dim m11_ As Double, m12_ As Double, m21_ As Double, m22_ As Double
        Dim cos_r As Double = Math.Cos(r)
        Dim sin_r As Double = Math.Sin(r)
        m11_ = m11 * cos_r + m21 * sin_r
        m21_ = m11 * -sin_r + m21 * cos_r
        m12_ = m12 * cos_r + m22 * sin_r
        m22_ = m12 * -sin_r + m22 * cos_r
        m11 = m11_
        m12 = m12_
        m21 = m21_
        m22 = m22_
    End Sub
End Class

Public Class TRect
    Public Position As New TPoint
    Public Size As New TPoint
End Class

'-------------------------------------------------------------------------------- TFont
Public Class TFont
    Public EmSize As Double = 24
    Public FontString As String = "24px 'monospace'"

    'Public FontTypeFace As Typeface
End Class

Public Class TGraphics
    Public Context As CanvasRenderingContext2D
    Public Canvas As HTMLCanvasElement
    Public Transform As New TMat2D
    Public TransformStack As New TList(Of TMat2D)

    Sub New(canvas1 As HTMLCanvasElement)
        Canvas = canvas1
        Context = Canvas.getContext("2d")
        Dim A As TMat2D = Transform
        Context.setTransform(A.m11, A.m12, A.m21, A.m22, A.dx, A.dy)
    End Sub

    Public Function MeasureText(fnt As TFont, text As String) As TPoint
        Dim sz As New TPoint, font_sv As String

        font_sv = Context.font
        Context.font = fnt.FontString

        sz.X = Context.measureText(text).width

        Context.font = font_sv

        sz.Y = fnt.EmSize

        Return sz
        'Dim formatted_text As FormattedText

        'formatted_text = New FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, FontTypeFace, EmSize, Brushes.Black)

        'Return New Size(formatted_text.Width, formatted_text.Height)
    End Function

    Sub save()
        TransformStack.push(Transform.Copy())
    End Sub

    Sub restore()
        Transform = TransformStack.pop()
        Dim A As TMat2D = Transform
        Context.setTransform(A.m11, A.m12, A.m21, A.m22, A.dx, A.dy)
    End Sub

    Sub scale(x As Double, y As Double)
        Transform.scale(x, y)
        Dim A As TMat2D = Transform
        Context.setTransform(A.m11, A.m12, A.m21, A.m22, A.dx, A.dy)
    End Sub

    Sub translate(x As Double, y As Double)
        Transform.translate(x, y)
        Dim A As TMat2D = Transform
        Context.setTransform(A.m11, A.m12, A.m21, A.m22, A.dx, A.dy)
    End Sub

    Sub rotate(r As Double)
        Transform.rotate(r)
        Dim A As TMat2D = Transform
        Context.setTransform(A.m11, A.m12, A.m21, A.m22, A.dx, A.dy)
    End Sub

    'Sub draw()
    '    Dim rct = New TPolygon()
    '    rct.SetBoundingRectangle(20, 20, 100, 50)
    '    rct.Draw(Me)
    'End Sub

    Sub draw3()
        Context.beginPath()
        Context.fillRect(20, 20, 100, 100)
        Context.beginPath()
        Context.clearRect(50, 70, 60, 30)
    End Sub

    Sub draw4()
        Context.beginPath()
        Context.fillStyle = "rgb(192, 80, 77)"
        Context.arc(70, 445, 35, 0, Math.PI * 2, False)
        Context.fill()
        Context.beginPath()
        Context.fillStyle = "rgb(155, 187, 89)"
        Context.arc(45, 495, 35, 0, Math.PI * 2, False)
        Context.fill()
        Context.beginPath()
        Context.fillStyle = "rgb(128, 100, 162)"
        Context.arc(95, 495, 35, 0, Math.PI * 2, False)
        Context.fill()
    End Sub

    Sub draw5()
        Context.beginPath()
        Context.font = "48px 'ＭＳ Ｐゴシック'"
        Context.strokeStyle = "blue"
        Context.strokeText("青色でstrokText", 10, 525, 80)
        Context.strokeText("maxLengthをセット", 10, 550, 80)
        Context.fillStyle = "red"
        Context.fillText("赤色でfillText", 10, 75, 580)
        Context.fillText("maxLengthをセット", 10, 600, 80)
    End Sub

    Sub Clear()
        Context.clearRect(0, 0, Canvas.width, Canvas.height)
    End Sub
End Class

Public Class TApplication
    Public Size As New TPoint
    Public MousePosition As New TPoint
    Public Graphics As TGraphics

    Public Overridable Sub AppInitialize()
    End Sub

    Public Overridable Sub Rule(self As Object, app As TMyApplication)
    End Sub

    Public Function MakeImage(src_url As String) As Image
        Dim img As New Image
        img.src = src_url

        Return img
    End Function
End Class
