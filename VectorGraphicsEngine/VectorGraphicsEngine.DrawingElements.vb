Partial Public Class VectorGraphicsEngine

#Region "DrawElement interface"
    ''' <summary>
    ''' Defines the contract for drawable elements within the VectorGraphicsEngine.
    ''' </summary>
    Public Interface IDrawElement
        ''' <summary>
        ''' Gets the layer number of the drawing element.
        ''' </summary>
        ReadOnly Property LayerNo As Integer

        ''' <summary>
        ''' Draws the element on the specified graphics surface.
        ''' </summary>
        ''' <param name="g">The Graphics object to draw on.</param>
        ''' <param name="zoomFactor">The current zoom level of the view.</param>
        ''' <param name="horizontalOffset">The horizontal offset in pixel coordinates.</param>
        ''' <param name="VerticalShift">The vertical offset in pixel coordinates.</param>
        ''' <param name="height">The height of the drawing area in pixels.</param>
        Sub Draw(g As Graphics, zoomFactor As Single, horizontalOffset As Double, VerticalShift As Double, height As Integer)

        ''' <summary>
        ''' Gets the bounding rectangle of the element in real-world coordinates.
        ''' </summary>
        ''' <returns>A RectangleF representing the bounds of the element.</returns>
        Function GetBounds() As RectangleF ' Returns real-world bounds
    End Interface

    ' Element definitions
    ' Defines the drawing element classes And Interface
    ''' <summary>
    ''' Raise  Layer Not Initialized Warning.
    ''' </summary>
    ''' <param name="layerNo">The layer No that was called and was not added.</param>
    Private Sub RaiseLayerNotInitializedWarning(layerNo As Integer)
        RaiseEvent LayerNotInitializedWarning(Me, New LayerNotInitializedEventArgs(layerNo))
    End Sub
    Private MustInherit Class DrawElement
        Implements IDrawElement
        Public Property LayerNo As Integer Implements IDrawElement.LayerNo ' Use Property for interface

        ''' <summary>
        ''' Draws the element on the specified graphics surface.
        ''' </summary>
        ''' <param name="g">The Graphics object to draw on.</param>
        ''' <param name="zoomFactor">The current zoom level of the view.</param>
        ''' <param name="horizontalOffset">The horizontal offset in pixel coordinates.</param>
        ''' <param name="VerticalShift">The vertical offset in pixel coordinates.</param>
        ''' <param name="height">The height of the drawing area in pixels.</param>
        Public MustOverride Sub Draw(g As Graphics, zoomFactor As Single, horizontalOffset As Double, VerticalShift As Double, height As Integer) Implements IDrawElement.Draw
        ''' <summary>
        ''' Gets the bounding rectangle of the element in real-world coordinates.
        ''' </summary>
        ''' <returns>A RectangleF representing the bounds of the element.</returns>
        Public MustOverride Function GetBounds() As RectangleF Implements IDrawElement.GetBounds
        Public Sub New(parent As VectorGraphicsEngine, layerNo As Integer)
            If Not parent._layers.ContainsKey(layerNo) Then
                parent.RaiseLayerNotInitializedWarning(layerNo)
            End If
            Me.LayerNo = layerNo
        End Sub
    End Class
#End Region
#Region "EventArgs"
    Public Class LayerNotInitializedEventArgs
        Inherits EventArgs
        Public Property LayerNumber As Integer
        Public Sub New(layerNo As Integer)
            LayerNumber = layerNo
        End Sub
    End Class
#End Region
    Private Class LineElement
        Inherits DrawElement
        Public X1, Y1, X2, Y2 As Single
        Public RelativeCoords As Boolean
        Public DrawWidth As Integer
        Public DrawColor As Color

        ''' <summary>
        ''' Initializes a new instance of the LineElement class.
        ''' </summary>
        ''' <param name="x1">The x-coordinate of the start point.</param>
        ''' <param name="y1">The y-coordinate of the start point.</param>
        ''' <param name="x2">The x-coordinate of the end point.</param>
        ''' <param name="y2">The y-coordinate of the end point.</param>
        ''' <param name="relativeCoords">Indicates if coordinates are relative to the start point.</param>
        ''' <param name="drawWidth">The width of the line in pixels.</param>
        ''' <param name="drawColor">The color of the line.</param>
        ''' <param name="layerNo">The layer number to assign the line to.</param>
        Public Sub New(parent As VectorGraphicsEngine, x1 As Single, y1 As Single, x2 As Single, y2 As Single, relativeCoords As Boolean, drawWidth As Integer, drawColor As Color, layerNo As Integer)
            MyBase.New(parent, layerNo)
            Me.X1 = x1 : Me.Y1 = y1 : Me.X2 = x2 : Me.Y2 = y2
            Me.RelativeCoords = relativeCoords : Me.DrawWidth = drawWidth : Me.DrawColor = drawColor
            Me.LayerNo = layerNo
        End Sub

        ''' <summary>
        ''' Draws the line element on the specified graphics surface.
        ''' </summary>
        Public Overrides Sub Draw(g As Graphics, zoomFactor As Single, horizontalOffset As Double, VerticalShift As Double, height As Integer)
            Using pen As New Pen(DrawColor, DrawWidth)
                Dim x1Pix As Single = VectorGraphicsEngine.X_Real_to_Pict(X1, zoomFactor, horizontalOffset)
                Dim y1Pix As Single = VectorGraphicsEngine.Y_Real_to_Pict(Y1, zoomFactor, VerticalShift, height)
                Dim x2Pix As Single = If(RelativeCoords, x1Pix + X2, VectorGraphicsEngine.X_Real_to_Pict(X2, zoomFactor, horizontalOffset))
                Dim y2Pix As Single = If(RelativeCoords, y1Pix + Y2, VectorGraphicsEngine.Y_Real_to_Pict(Y2, zoomFactor, VerticalShift, height))
                g.DrawLine(pen, x1Pix, y1Pix, x2Pix, y2Pix)
            End Using
        End Sub

        ''' <summary>
        ''' Gets the bounding rectangle of the line in real-world coordinates.
        ''' </summary>
        Public Overrides Function GetBounds() As RectangleF
            If RelativeCoords Then
                Return New RectangleF(X1, Y1, X2, Y2)
            Else
                Dim minX As Single = Math.Min(X1, X2)
                Dim minY As Single = Math.Min(Y1, Y2)
                Dim maxX As Single = Math.Max(X1, X2)
                Dim maxY As Single = Math.Max(Y1, Y2)
                Return New RectangleF(minX, minY, maxX - minX, maxY - minY)
            End If
        End Function
    End Class

    Private Class CircleElement
        Inherits DrawElement
        Public X, Y, Radius As Single
        Public FixedRadius As Boolean
        Public DrawWidth As Integer
        Public DrawColor, FillColor As Color
        Public Filled As Boolean

        ''' <summary>
        ''' Initializes a new instance of the CircleElement class.
        ''' </summary>
        ''' <param name="x">The x-coordinate of the circle's center.</param>
        ''' <param name="y">The y-coordinate of the circle's center.</param>
        ''' <param name="radius">The radius of the circle.</param>
        ''' <param name="fixedRadius">Indicates if the radius is fixed in pixels.</param>
        ''' <param name="drawWidth">The width of the circle's outline.</param>
        ''' <param name="drawColor">The color of the circle's outline.</param>
        ''' <param name="fillColor">The fill color of the circle.</param>
        ''' <param name="filled">Indicates if the circle should be filled.</param>
        ''' <param name="layerNo">The layer number to assign the circle to.</param>
        Public Sub New(Parent As VectorGraphicsEngine, x As Single, y As Single, radius As Single, fixedRadius As Boolean, drawWidth As Integer, drawColor As Color, fillColor As Color, filled As Boolean, layerNo As Integer)
            MyBase.New(Parent, layerNo)
            Me.X = x : Me.Y = y : Me.Radius = radius : Me.FixedRadius = fixedRadius
            Me.DrawWidth = drawWidth : Me.DrawColor = drawColor : Me.FillColor = fillColor : Me.Filled = filled
            Me.LayerNo = layerNo
            If radius < 0 Then Throw New ArgumentException("Radius cannot be negative.")
        End Sub

        ''' <summary>
        ''' Draws the circle element on the specified graphics surface.
        ''' </summary>
        Public Overrides Sub Draw(g As Graphics, zoomFactor As Single, horizontalOffset As Double, VerticalShift As Double, height As Integer)
            Dim my_rad As Single = If(FixedRadius, Radius, VectorGraphicsEngine.DIST_Real_to_Pict(Radius, zoomFactor))
            If my_rad < 1 Then my_rad = 1
            Using pen As New Pen(DrawColor, DrawWidth), brush As New SolidBrush(FillColor)
                Dim xPix As Single = VectorGraphicsEngine.X_Real_to_Pict(X, zoomFactor, horizontalOffset) - my_rad
                Dim yPix As Single = VectorGraphicsEngine.Y_Real_to_Pict(Y, zoomFactor, VerticalShift, height) - my_rad
                If Filled Then g.FillEllipse(brush, xPix, yPix, my_rad * 2, my_rad * 2)
                g.DrawEllipse(pen, xPix, yPix, my_rad * 2, my_rad * 2)
            End Using
        End Sub

        ''' <summary>
        ''' Gets the bounding rectangle of the circle in real-world coordinates.
        ''' </summary>
        Public Overrides Function GetBounds() As RectangleF
            Return New RectangleF(X - Radius, Y - Radius, Radius * 2, Radius * 2)
        End Function
    End Class

    Private Class RectangleElement
        Inherits DrawElement
        Public X1, Y1, X2, Y2 As Single
        Public RelativeCoords As Boolean
        Public DrawWidth As Integer
        Public DrawColor, FillColor As Color
        Public Filled As Boolean

        ''' <summary>
        ''' Initializes a new instance of the RectangleElement class.
        ''' </summary>
        ''' <param name="x1">The x-coordinate of the first corner.</param>
        ''' <param name="y1">The y-coordinate of the first corner.</param>
        ''' <param name="x2">The x-coordinate of the second corner.</param>
        ''' <param name="y2">The y-coordinate of the second corner.</param>
        ''' <param name="relativeCoords">Indicates if coordinates are relative.</param>
        ''' <param name="drawWidth">The width of the rectangle's outline.</param>
        ''' <param name="drawColor">The color of the rectangle's outline.</param>
        ''' <param name="fillColor">The fill color of the rectangle.</param>
        ''' <param name="filled">Indicates if the rectangle should be filled.</param>
        ''' <param name="layerNo">The layer number to assign the rectangle to.</param>
        Public Sub New(parent As VectorGraphicsEngine, x1 As Single, y1 As Single, x2 As Single, y2 As Single, relativeCoords As Boolean, drawWidth As Integer, drawColor As Color, fillColor As Color, filled As Boolean, layerNo As Integer)
            MyBase.New(parent, layerNo)
            Me.X1 = x1 : Me.Y1 = y1 : Me.X2 = x2 : Me.Y2 = y2
            Me.RelativeCoords = relativeCoords : Me.DrawWidth = drawWidth : Me.DrawColor = drawColor : Me.FillColor = fillColor : Me.Filled = filled
            Me.LayerNo = layerNo
        End Sub

        ''' <summary>
        ''' Draws the rectangle element on the specified graphics surface.
        ''' </summary>
        Public Overrides Sub Draw(g As Graphics, zoomFactor As Single, horizontalOffset As Double, VerticalShift As Double, height As Integer)
            Using pen As New Pen(DrawColor, DrawWidth), brush As New SolidBrush(FillColor)
                Dim rect As RectangleF
                If Not RelativeCoords Then
                    Dim x1Pix As Single = VectorGraphicsEngine.X_Real_to_Pict(X1, zoomFactor, horizontalOffset)
                    Dim y1Pix As Single = VectorGraphicsEngine.Y_Real_to_Pict(Y1, zoomFactor, VerticalShift, height)
                    Dim x2Pix As Single = VectorGraphicsEngine.X_Real_to_Pict(X2, zoomFactor, horizontalOffset)
                    Dim y2Pix As Single = VectorGraphicsEngine.Y_Real_to_Pict(Y2, zoomFactor, VerticalShift, height)
                    Dim x As Single = Math.Min(x1Pix, x2Pix)
                    Dim y As Single = Math.Min(y1Pix, y2Pix)
                    Dim w As Single = Math.Abs(x2Pix - x1Pix)
                    Dim h As Single = Math.Abs(y2Pix - y1Pix)
                    rect = New RectangleF(x, y, w, h)
                Else
                    rect = New RectangleF(VectorGraphicsEngine.X_Real_to_Pict(X1, zoomFactor, horizontalOffset), VectorGraphicsEngine.Y_Real_to_Pict(Y1, zoomFactor, VerticalShift, height), X2, Y2)
                End If
                If Filled Then g.FillRectangle(brush, rect)
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height)
            End Using
        End Sub

        ''' <summary>
        ''' Gets the bounding rectangle of the rectangle in real-world coordinates.
        ''' </summary>
        Public Overrides Function GetBounds() As RectangleF
            If RelativeCoords Then
                Return New RectangleF(X1, Y1, X2, Y2)
            Else
                Dim minX As Single = Math.Min(X1, X2)
                Dim minY As Single = Math.Min(Y1, Y2)
                Dim maxX As Single = Math.Max(X1, X2)
                Dim maxY As Single = Math.Max(Y1, Y2)
                Return New RectangleF(minX, minY, maxX - minX, maxY - minY)
            End If
        End Function
    End Class

    Private Class LabelElement
        Inherits DrawElement
        Public X, Y As Single
        Public Text As String
        Public DrawColor As Color
        Public FontSize As Integer

        ''' <summary>
        ''' Initializes a new instance of the LabelElement class.
        ''' </summary>
        ''' <param name="x">The x-coordinate of the label's position.</param>
        ''' <param name="y">The y-coordinate of the label's position.</param>
        ''' <param name="text">The text to display.</param>
        ''' <param name="drawColor">The color of the text.</param>
        ''' <param name="fontSize">The size of the font in points.</param>
        ''' <param name="layerNo">The layer number to assign the label to.</param>
        Public Sub New(parent As VectorGraphicsEngine, x As Single, y As Single, text As String, drawColor As Color, fontSize As Integer, layerNo As Integer)
            MyBase.New(parent, layerNo)
            Me.X = x : Me.Y = y : Me.Text = text : Me.DrawColor = drawColor : Me.FontSize = fontSize
            Me.LayerNo = layerNo
        End Sub

        ''' <summary>
        ''' Draws the label element on the specified graphics surface.
        ''' </summary>
        Public Overrides Sub Draw(g As Graphics, zoomFactor As Single, horizontalOffset As Double, VerticalShift As Double, height As Integer)
            Using font As New Font("Arial", FontSize), brush As New SolidBrush(DrawColor)
                g.DrawString(Text, font, brush, VectorGraphicsEngine.X_Real_to_Pict(X, zoomFactor, horizontalOffset), VectorGraphicsEngine.Y_Real_to_Pict(Y, zoomFactor, VerticalShift, height))
            End Using
        End Sub

        ''' <summary>
        ''' Gets the approximate bounding rectangle of the label in real-world coordinates.
        ''' </summary>
        Public Overrides Function GetBounds() As RectangleF
            Dim approxWidth As Single = Text.Length * FontSize * 0.6F
            Dim approxHeight As Single = FontSize
            Return New RectangleF(X, Y, approxWidth, approxHeight)
        End Function
    End Class

End Class

