
'-------------------------------------------------------------------------------- EOrientation
Public Enum EOrientation
    Horizontal
    Vertical
End Enum

'-------------------------------------------------------------------------------- TColor
Public Class TColor
End Class

'-------------------------------------------------------------------------------- TAnchorStyle
Public Class TAnchorStyle
    Public Left As Boolean
    Public Top As Boolean
    Public Right As Boolean
    Public Bottom As Boolean
End Class

'-------------------------------------------------------------------------------- TBitmap
Public Class TBitmap
    Public Width As Integer
    Public Height As Integer

    Public Sub New()
    End Sub

    Public Sub New(width As Integer, height As Integer)
    End Sub
End Class

'-------------------------------------------------------------------------------- TView
Public Class TView
    <_Parent()> Public ParentControl As TControl
    <_Prev()> Public Prev As TControl

    Public Left As Double
    Public Top As Double

    Public Width As Double
    Public Height As Double

    Public ActualWidth As Double
    Public ActualHeight As Double

    Public DesiredWidth As Double
    Public DesiredHeight As Double

    Public Position As New TPoint
    Public AbsPosition As New TPoint

    Public Visible As Boolean

    Public MarginLeft As Double
    Public MarginTop As Double
    Public MarginRight As Double
    Public MarginBottom As Double
    Public MarginMiddleHorizontal As Double
    Public MarginMiddleVertical As Double

    Public Bitmap As TBitmap
    Public BackgroundBitmap As TBitmap
    Public BackgroundImage As TBitmap
    Public BackgroundColor As String = "cornsilk"

    Public BorderWidth As Double = 1
    Public BorderColor As String = "#0000FF"

    Public Overridable Sub Draw(gr As TGraphics)
        Dim ctx As CanvasRenderingContext2D = gr.Context
        gr.save()
        ctx.beginPath()

        If BackgroundColor <> Nothing Then
            ctx.fillStyle = BackgroundColor
            ctx.fillRect(AbsPosition.X, AbsPosition.Y, ActualWidth, ActualHeight)
        End If

        If BorderWidth <> 0 AndAlso BorderColor <> Nothing Then
            ctx.lineWidth = BorderWidth
            ctx.strokeStyle = BorderColor
            ctx.strokeRect(AbsPosition.X, AbsPosition.Y, ActualWidth, ActualHeight)
        End If

        ctx.closePath()
        gr.restore()
    End Sub
End Class

'-------------------------------------------------------------------------------- TControl
Public Class TControl
    Inherits TView
    Public MousePressBorderColor As TColor
    Public MouseOverBorderColor As TColor

    Public MousePressBackgroundColor As TColor
    Public MouseOverBackgroundColor As TColor

    Public Anchor As TAnchorStyle
    Public ClientLeft As Double
    Public ClientTop As Double

    Public LeftPadding As Double = 0
    Public TopPadding As Double = 0
    Public RightPadding As Double = 0
    Public BottomPadding As Double = 0

    'Public Data As Object
End Class

'-------------------------------------------------------------------------------- TViewGroup
Public Class TViewGroup
    Inherits TView
End Class

'-------------------------------------------------------------------------------- TPanel
Public Class TPanel
    Inherits TControl
    Public Children As New TList(Of TControl)

    Public HorizontalPadding As Double
    Public VerticalPadding As Double
    Public ChildrenScale As Double

    Public Overrides Sub Draw(gr As TGraphics)
        Dim ctx As CanvasRenderingContext2D = gr.Context
        gr.save()
        ctx.beginPath()

        If BackgroundColor <> Nothing Then
            ctx.fillStyle = BackgroundColor
            ctx.fillRect(AbsPosition.X, AbsPosition.Y, ActualWidth, ActualHeight)
        End If

        If BorderWidth <> 0 AndAlso BorderColor <> Nothing Then
            ctx.lineWidth = BorderWidth
            ctx.strokeStyle = BorderColor
            ctx.strokeRect(AbsPosition.X, AbsPosition.Y, ActualWidth, ActualHeight)
        End If

        ctx.closePath()

        gr.restore()

        For Each x In Children
            x.Draw(gr)
        Next
    End Sub
End Class

'-------------------------------------------------------------------------------- TCanvas
Public Class TCanvas
    Inherits TPanel

End Class

'-------------------------------------------------------------------------------- TStackPanel
Public Class TStackPanel
    Inherits TPanel

    Public Orientation As EOrientation
End Class

'-------------------------------------------------------------------------------- TPopup
Public Class TPopup

End Class

'-------------------------------------------------------------------------------- TForm
Public Class TForm
    Inherits TControl
    Public Container As TPanel
End Class

'-------------------------------------------------------------------------------- TTextBlock
Public Class TTextBlock
    Inherits TControl
    Public AutoSize As Boolean = True
    Public Font As New TFont
    Public Text As String
    Public TextWidth As Double
    Public TextHeight As Double
    Public TextColor As String = "#000000"

    Public DataFormat As String


    Public Overrides Sub Draw(gr As TGraphics)
        Dim ctx As CanvasRenderingContext2D = gr.Context
        gr.save()
        ctx.beginPath()

        If BackgroundColor <> Nothing Then
            ctx.fillStyle = BackgroundColor
            ctx.fillRect(AbsPosition.X, AbsPosition.Y, ActualWidth, ActualHeight)
        End If

        If BorderWidth <> 0 AndAlso BorderColor <> Nothing Then
            ctx.lineWidth = BorderWidth
            ctx.strokeStyle = BorderColor
            ctx.strokeRect(AbsPosition.X, AbsPosition.Y, ActualWidth, ActualHeight)
        End If

        ctx.font = Font.FontString
        ctx.fillStyle = TextColor
        ctx.fillText(Text, AbsPosition.X, AbsPosition.Y + ActualHeight - BorderWidth)

        ctx.closePath()
        gr.restore()
    End Sub


End Class

'-------------------------------------------------------------------------------- TLabel
Public Class TLabel
    Inherits TTextBlock
End Class

'-------------------------------------------------------------------------------- TButton
Public Class TButton
    Inherits TTextBlock
End Class

'-------------------------------------------------------------------------------- TTextBox
Public Class TTextBox
    Inherits TTextBlock
End Class

'-------------------------------------------------------------------------------- TRadioButton
Public Class TRadioButton
    Inherits TControl
End Class

'-------------------------------------------------------------------------------- TScrollBar
Public Class TScrollBar
    Inherits TControl

    Public Orientation As EOrientation
    Public PrevButton As TButton
    Public NextButton As TButton
    Public Thumb As TThumb

    Public Maximum As Double
    Public Minimum As Double

    Public LowValue As Double
    Public HighValue As Double
End Class

'-------------------------------------------------------------------------------- TScrollView
Public Class TScrollView
    Inherits TControl
    Public HorizontalScrollBar As TScrollBar
    Public VerticalScrollBar As TScrollBar

    Public ContentWidth As Double
    Public ContentHeight As Double

    Public ViewOffsetX As Double
    Public ViewOffsetY As Double
End Class

'-------------------------------------------------------------------------------- TListBox
Public Class TListBox
    Public Items As New TList(Of TView)
End Class

'-------------------------------------------------------------------------------- TTreeViewItem
Public Class TTreeViewItem
    Inherits TControl
    Public Header As New TLabel
    Public PaddingTVI As Double = 5
    Public Indent As Double = 10
    Public Expanded As Boolean = True
    Public ChildrenTVI As New TList(Of TTreeViewItem)

    Public Overrides Sub Draw(gr As TGraphics)
        Dim ctx As CanvasRenderingContext2D = gr.Context
        gr.save()
        ctx.beginPath()

        If BackgroundColor <> Nothing Then
            ctx.fillStyle = BackgroundColor
            ctx.fillRect(AbsPosition.X, AbsPosition.Y, ActualWidth, ActualHeight)
        End If

        If BorderWidth <> 0 AndAlso BorderColor <> Nothing Then
            ctx.lineWidth = BorderWidth
            ctx.strokeStyle = BorderColor
            ctx.strokeRect(AbsPosition.X, AbsPosition.Y, ActualWidth, ActualHeight)
        End If

        ctx.closePath()
        gr.restore()

        Header.Draw(gr)

        For Each x In ChildrenTVI
            x.Draw(gr)
        Next
    End Sub
End Class

'-------------------------------------------------------------------------------- TTreeView
Public Class TTreeView
    Inherits TScrollView
    Public Root As New TTreeViewItem
End Class

'-------------------------------------------------------------------------------- TSplitContainer
Public Class TSplitContainer

End Class

'-------------------------------------------------------------------------------- TTabPage
Public Class TTabPage
    Public Text As String

    Public TabButton As TButton
    Public Panel As TPanel
End Class

'-------------------------------------------------------------------------------- TTabControl
Public Class TTabControl
    Public TabPages As New TList(Of TTabPage)

End Class

'-------------------------------------------------------------------------------- TDesWnd
Public Class TDesWnd

End Class

'-------------------------------------------------------------------------------- TSrcEditAbs
Public Class TSrcEditAbs

End Class

'-------------------------------------------------------------------------------- TSrcEdit
Public Class TSrcEdit

End Class

'-------------------------------------------------------------------------------- TSrcBrowser
Public Class TSrcBrowser

End Class

'-------------------------------------------------------------------------------- TThumb
Public Class TThumb
    Inherits TControl
End Class

'-------------------------------------------------------------------------------- TComboBox
Public Class TComboBox
End Class

'-------------------------------------------------------------------------------- TCheckBox
Public Class TCheckBox
End Class

'-------------------------------------------------------------------------------- TMenu
Public Class TMenu
End Class

'-------------------------------------------------------------------------------- TPictureBox
Public Class TGrid
End Class

Public Class TPictureBox
End Class

'-------------------------------------------------------------------------------- TSlider
Public Class TSlider
    Public Thumb As TThumb
End Class

'-------------------------------------------------------------------------------- TGroupBox
Public Class TGroupBox
    Public Text As String
End Class

Public Class TWindowApplication
    Inherits TApplication
    Public ViewList As New TList(Of TView)
End Class
