
Public Class TShape
    <_Parent()> Public Parent As Object = Nothing
    <_Prev()> Public Prev As TShape = Nothing
    Public Center As New TPoint
    Public Radius As Double
    Public AbsCenter As New TPoint
    Public Size As New TPoint
    Public Rotation As Double = 0
    Public Velocity As New TPoint
    Public BoundingRectangle As New TRect
    Public Test As New TRect

    Sub SetBoundingRectangle(x As Double, y As Double, w As Double, h As Double)
        Center.X = x
        Center.Y = y
        Size.X = w
        Size.Y = h
        Radius = Math.Max(w, h) / 2
    End Sub

    Public Overridable Sub Draw(gr As TGraphics)
    End Sub

    Public Overridable Sub SetAbsCenter(gr As TGraphics)
    End Sub
End Class

Public Class TGroup
    Inherits TShape
    Public Children As New TList(Of TShape)

    Public Overrides Sub Draw(gr As TGraphics)
        Dim ctx As CanvasRenderingContext2D = gr.Context
        gr.save()
        AbsCenter = gr.Transform.MulPoint(Center)
        gr.translate(Center.X, Center.Y)
        gr.rotate(Rotation)
        For Each x In Children
            x.Draw(gr)
        Next
        gr.restore()
    End Sub
End Class

Public Class TPolygon
    Inherits TShape

    Public Overrides Sub Draw(gr As TGraphics)
        Dim ctx As CanvasRenderingContext2D = gr.Context
        ctx.beginPath()
        ctx.moveTo(Center.X, Center.Y)
        ctx.lineTo(Center.X + Size.X, Center.Y)
        ctx.lineTo(Center.X + Size.X, Center.Y + Size.Y)
        ctx.lineTo(Center.X, Center.Y + Size.Y)
        ctx.closePath()
        ctx.stroke()
    End Sub
End Class

Public Class TRectangle
    Inherits TShape
    Public BackgroundColor As String = Nothing
    Public BorderWidth As Double = 0
    Public BorderColor As String = Nothing

    Public Overrides Sub Draw(gr As TGraphics)
        Dim ctx As CanvasRenderingContext2D = gr.Context
        gr.save()
        ctx.beginPath()
        Dim dx = Size.X / 2
        Dim dy = Size.Y / 2
        AbsCenter = gr.Transform.MulPoint(Center)
        gr.translate(Center.X, Center.Y)
        gr.rotate(Rotation)
        If BackgroundColor <> Nothing Then
            ctx.fillStyle = BackgroundColor
            ctx.fillRect(-dx, -dy, Size.X, Size.Y)
        End If
        If BorderWidth <> 0 AndAlso BorderColor <> Nothing Then
            ctx.strokeStyle = BorderColor
            ctx.strokeRect(-dx, -dy, Size.X, Size.Y)
        End If
        ctx.closePath()
        gr.restore()
    End Sub
End Class

Public Class TLabel
    Inherits TShape
    Public BackgroundColor As String = Nothing
    Public BorderWidth As Double = 0
    Public BorderColor As String = Nothing
    Public TextColor As String = "#000000"
    Public Text As String = ""

    Public Overrides Sub Draw(gr As TGraphics)
        Dim ctx As CanvasRenderingContext2D = gr.Context
        gr.save()
        ctx.beginPath()
        Dim dx = Size.X / 2
        Dim dy = Size.Y / 2
        AbsCenter = gr.Transform.MulPoint(Center)
        gr.translate(Center.X, Center.Y)
        gr.rotate(Rotation)
        ctx.textBaseline = "top"
        ctx.font = "40px 'ＭＳ Ｐゴシック'"
        If BackgroundColor <> Nothing Then
            ctx.fillStyle = BackgroundColor
            ctx.fillRect(-dx, -dy, Size.X, Size.Y)
        End If
        If BorderWidth <> 0 AndAlso BorderColor <> Nothing Then
            ctx.strokeStyle = BorderColor
            ctx.strokeRect(-dx, -dy, Size.X, Size.Y)
        End If
        ctx.fillStyle = TextColor
        ctx.fillText(Text, -dx, -dy)
        ctx.closePath()
        gr.restore()
    End Sub
End Class

Public Class TEllipse
    Inherits TShape
    Public BackgroundColor As String = Nothing
    Public BorderWidth As Double = 0
    Public BorderColor As String = Nothing

    Public Overrides Sub Draw(gr As TGraphics)
        Dim ctx As CanvasRenderingContext2D = gr.Context
        gr.save()
        ctx.beginPath()
        Dim rx As Double = Size.X / 2
        If BackgroundColor <> Nothing Then
            ctx.fillStyle = BackgroundColor
        End If
        If BorderWidth <> 0 AndAlso BorderColor <> Nothing Then
            ctx.strokeStyle = BorderColor
        End If
        AbsCenter = gr.Transform.MulPoint(Center)
        gr.translate(Center.X, Center.Y)
        gr.rotate(Rotation)
        gr.scale(1, Size.Y / Size.X)
        ctx.arc(0, 0, rx, 0, Math.PI * 2, False)
        If BackgroundColor <> Nothing Then
            ctx.fill()
        End If
        If BorderWidth <> 0 AndAlso BorderColor <> Nothing Then
            ctx.stroke()
        End If
        ctx.closePath()
        gr.restore()
    End Sub
End Class

Public Class TPicture
    Inherits TShape
    Public Loaded As Boolean = False
    <_Weak()> Public ImageIm As Image
    <_Weak()> Public ImageList As New TList(Of Image)

    Sub Load(src_url As String)
        ImageIm = New Image()
        ImageIm.src = src_url
    End Sub

    Public Overrides Sub Draw(gr As TGraphics)
        Dim ctx As CanvasRenderingContext2D = gr.Context

        If Not ImageIm.complete Then
            Return
        End If

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
    End Sub
End Class

Public Class TShapeApplication
    Inherits TApplication
    Public ShapeList As New TList(Of TShape)
End Class
