Imports System.ComponentModel

<DesignerCategory("UserControl")>
Public Class VectorGraphicsEngine
    Inherits UserControl
#Region "Local Variables"



#Region "Control fields"
    Private _showScaleBar As Boolean = True ' Toggle scale bar visibility
    Private _scaleBarLengthPixels As Single = 100 ' Target pixel length for scale bar
    '
    Dim CurrentViewSettings As New ViewSettings(1, 0, 0)
    Private DrawElements As New HashSet(Of DrawElement)
    Private _GridSpacing As Single = 1.0F
    Private Draw_Axes_boolean As Boolean = True
    Private Draw_Grid_boolean As Boolean = False
    Private isPanning As Boolean = False
    Private panStartX As Integer
    Private panStartY As Integer
    'Private _visibleLayers As New HashSet(Of Integer)
    Private _backgroundImage As Image

    ''' <summary>
    ''' The current mouse location in real-world coordinates.
    ''' </summary>
    Public MouseLocationReal As PointF

    ''' <summary>
    ''' The current mouse location in pixel coordinates.
    ''' </summary>
    Public MouseLocationPixel As PointF
#End Region

#End Region
#Region "Constructor"
    ''' <summary>
    ''' Initializes a new instance of the VectorGraphicsEngine class.
    ''' </summary>
    Public Sub New()
        InitializeComponent()
        Me.DoubleBuffered = True
        Me.SetStyle(ControlStyles.ResizeRedraw, True)
        ' Initialize default layer 0
        maxAddedLayerNo = -1 ' Reset to -1 so AddLayer starts at 0
        AddLayer() ' Adds layer 0
        If LblCoordianates IsNot Nothing Then
            LblCoordianates.Visible = True
            LblCoordianates.BringToFront()
        End If
    End Sub
#End Region
#Region "Properties"
    ''' <summary>
    ''' Gets or sets a value indicating whether to display the scale bar.
    ''' </summary>
    Public Property ShowScaleBar As Boolean
        Get
            Return _showScaleBar
        End Get
        Set(value As Boolean)
            _showScaleBar = value
            Invalidate()
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets a layer by its layer number.
    ''' </summary>
    ''' <param name="index">The layer number to access.</param>
    ''' <returns>The Layer object associated with the specified layer number.</returns>
    ''' <exception cref="ArgumentException">Thrown when the specified layer does not exist.</exception>
    Default Public Property Layer(index As Integer) As LayerClass
        ' Indexer property to get/set a Layer by its LayerNumber
        Get
            If _layers.ContainsKey(index) Then
                Return _layers(index)
            Else
                Throw New ArgumentException($"Layer {index} does not exist. Use AddLayer() to create it first.")
            End If
        End Get
        Set(value As LayerClass)
            If _layers.ContainsKey(index) Then
                ' Update the existing layer with the new value
                _layers(index) = value
                Invalidate() ' Redraw to reflect changes
            Else
                Throw New ArgumentException($"Layer {index} does not exist. Use AddLayer() to create it first.")
            End If
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets the spacing between grid lines in real-world units.
    ''' </summary>
    Public Property GridSpacing As Single
        Get
            Return _GridSpacing
        End Get
        Set(value As Single)
            _GridSpacing = value
            Invalidate()
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets a value indicating whether to display the axes.
    ''' </summary>
    Public Property ShowAxes As Boolean
        Get
            Return Draw_Axes_boolean
        End Get
        Set(value As Boolean)
            Draw_Axes_boolean = value
            Invalidate()
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets a value indicating whether to display the grid.
    ''' </summary>
    Public Property ShowGrid As Boolean
        Get
            Return Draw_Grid_boolean
        End Get
        Set(value As Boolean)
            Draw_Grid_boolean = value
            Invalidate()
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets the current display scale (zoom factor).
    ''' </summary>
    Public Property DisplayScale As Single
        Get
            Return CurrentViewSettings.ZoomFactor
        End Get
        Set(value As Single)
            If value > 0 Then
                CurrentViewSettings.ZoomFactor = value
                Invalidate()
            End If
        End Set
    End Property
#End Region

End Class