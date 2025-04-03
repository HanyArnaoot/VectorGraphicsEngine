Partial Public Class VectorGraphicsEngine
    'Handles layer operations and view setting
#Region "Variables"
    Private _layers As New Dictionary(Of Integer, LayerClass)
    Private maxAddedLayerNo As Integer = -1 ' Start at -1 so first layer is 0
#End Region

#Region "classes and Structures"
    ''' <summary>
    ''' Represents the Layers setting class.
    ''' </summary>
    Public Class LayerClass
        Public Property LayerNumber As Integer
        Public Property Visible As Boolean = True ' Layers are visible by default
        Public Property Name As String = "" ' Optional for future use

        Public Sub New(number As Integer)
            LayerNumber = number
        End Sub
    End Class
    ''' <summary>
    ''' Represents the current view settings including zoom and offsets.
    ''' </summary>
    Public Structure ViewSettings
        Public ZoomFactor As Single
        Public HorizontalShift As Double
        Public VerticalShift As Double

        ''' <summary>
        ''' Initializes a new instance of the ViewSettings structure.
        ''' </summary>
        ''' <param name="zoom">The zoom factor.</param>
        ''' <param name="hShift">The horizontal shift in pixels.</param>
        ''' <param name="vShift">The vertical shift in pixels.</param>
        Public Sub New(zoom As Single, hShift As Double, vShift As Double)
            ZoomFactor = zoom
            HorizontalShift = hShift
            VerticalShift = vShift
        End Sub
    End Structure
#End Region
#Region "Layers"
    ''' <summary>
    ''' Clears all drawing elements from the specified layer.
    ''' </summary>
    ''' <param name="layerNo">The layer number to clear.</param>
    Public Sub ClearDrawingLayer(layerNo As Integer)
        DrawElements.RemoveWhere(Function(element) element.LayerNo = layerNo)
        Invalidate()
    End Sub

    ''' <summary>
    ''' Gets a list of drawing elements in the specified layer.
    ''' </summary>
    ''' <param name="layerNo">The layer number to query.</param>
    ''' <returns>A list of IDrawElement objects in the specified layer.</returns>
    Public Function GetElementsInLayer(layerNo As Integer) As List(Of IDrawElement)
        Return DrawElements.Where(Function(element) element.LayerNo = layerNo).Cast(Of IDrawElement).ToList()
    End Function

    ''' <summary>
    ''' Adds a new layer and returns its number.
    ''' </summary>
    ''' <returns>The number of the newly added layer.</returns>
    Public Function AddLayer() As Integer
        maxAddedLayerNo += 1 ' Increment to get the next unique number
        Dim newLayer As New LayerClass(maxAddedLayerNo)
        _layers.Add(maxAddedLayerNo, newLayer)
        Return maxAddedLayerNo
    End Function

    '' <summary>
    ''' Removes the specified layer and all its elements.
    ''' </summary>
    ''' <param name="layerNo">The layer number to remove.</param>
    Public Sub RemoveLayer(layerNo As Integer)
        If _layers.ContainsKey(layerNo) Then
            _layers.Remove(layerNo)
            DrawElements.RemoveWhere(Function(element) element.LayerNo = layerNo)
            Invalidate()
        Else
            Throw New ArgumentException($"Layer {layerNo} does not exist.")
        End If
    End Sub

    ''' <summary>
    ''' Hides the specified layer, making its elements invisible.
    ''' </summary>
    ''' <param name="layerNo">The layer number to hide.</param>
    ''' <exception cref="ArgumentException">Thrown when the specified layer does not exist.</exception>
    Public Sub HideLayer(layerNo As Integer)
        If _layers.ContainsKey(layerNo) Then
            _layers(layerNo).Visible = False
            Invalidate()
        Else
            Throw New ArgumentException($"Layer {layerNo} does not exist.")
        End If
    End Sub

    ''' <summary>
    ''' Hides the specified layer.
    ''' </summary>
    ''' <param name="layerNo">The layer number to hide.</param>
    Public Sub ShowLayer(layerNo As Integer)
        If _layers.ContainsKey(layerNo) Then
            _layers(layerNo).Visible = True
            Invalidate()
        Else
            Throw New ArgumentException($"Layer {layerNo} does not exist.")
        End If
    End Sub
#End Region

    ''' <summary>
    ''' Clears all drawing elements and resets the view settings.
    ''' </summary>
    Public Sub ClearDrawing()
        DrawElements.Clear()
        CurrentViewSettings = New ViewSettings(1, 0, 0)
        Invalidate()
    End Sub
End Class

