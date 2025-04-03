Partial Public Class VectorGraphicsEngine
    'Contains methods For drawing shapes And the main painting logic.



#Region "Drawing methods"
    ''' <summary>
    ''' Draws a line on the specified layer.
    ''' </summary>
    ''' <param name="layerNo">The layer number to draw on.</param>
    ''' <param name="x1">The x-coordinate of the start point.</param>
    ''' <param name="y1">The y-coordinate of the start point.</param>
    ''' <param name="x2">The x-coordinate of the end point.</param>
    ''' <param name="y2">The y-coordinate of the end point.</param>
    ''' <param name="drawColor">The color of the line (defaults to Black).</param>
    ''' <param name="relativeCoords">Indicates if coordinates are relative.</param>
    ''' <param name="drawWidth">The width of the line in pixels.</param>
    Public Sub DrawLine(ByVal layerNo As Integer, x1 As Single, y1 As Single, x2 As Single, y2 As Single, Optional drawColor As Color = Nothing, Optional relativeCoords As Boolean = False, Optional drawWidth As Integer = 1)
        DrawElements.Add(New LineElement(Me, x1, y1, x2, y2, relativeCoords, drawWidth, If(drawColor = Nothing, Color.Black, drawColor), layerNo))
        Invalidate()
    End Sub

    ''' <summary>
    ''' Draws a circle on the specified layer.
    ''' </summary>
    ''' <param name="layerNo">The layer number to draw on.</param>
    ''' <param name="x1">The x-coordinate of the center.</param>
    ''' <param name="y1">The y-coordinate of the center.</param>
    ''' <param name="radius">The radius of the circle.</param>
    ''' <param name="drawColor">The outline color (defaults to Black).</param>
    ''' <param name="filled">Indicates if the circle should be filled.</param>
    ''' <param name="fillColor">The fill color (defaults to Black).</param>
    ''' <param name="fixedRadius">Indicates if the radius is fixed in pixels.</param>
    ''' <param name="drawWidth">The width of the outline in pixels.</param>
    Public Sub DrawCircle(ByVal layerNo As Integer, x1 As Single, y1 As Single, radius As Single, Optional drawColor As Color = Nothing, Optional filled As Boolean = False, Optional fillColor As Color = Nothing, Optional fixedRadius As Boolean = False, Optional drawWidth As Integer = 1)
        DrawElements.Add(New CircleElement(Me, x1, y1, radius, fixedRadius, drawWidth, If(drawColor = Nothing, Color.Black, drawColor), If(fillColor = Nothing, Color.Black, fillColor), filled, layerNo))
        Invalidate()
    End Sub

    ''' <summary>
    ''' Draws a rectangle on the specified layer.
    ''' </summary>
    ''' <param name="layerNo">The layer number to draw on.</param>
    ''' <param name="x1">The x-coordinate of the first corner.</param>
    ''' <param name="y1">The y-coordinate of the first corner.</param>
    ''' <param name="x2">The x-coordinate of the second corner.</param>
    ''' <param name="y2">The y-coordinate of the second corner.</param>
    ''' <param name="relativeCoords">Indicates if coordinates are relative.</param>
    ''' <param name="drawWidth">The width of the outline in pixels.</param>
    ''' <param name="drawColor">The outline color (defaults to Black).</param>
    ''' <param name="fillColor">The fill color (defaults to White).</param>
    ''' <param name="filled">Indicates if the rectangle should be filled.</param>
    Public Sub DrawRectangle(ByVal layerNo As Integer, x1 As Single, y1 As Single, x2 As Single, y2 As Single, Optional relativeCoords As Boolean = False, Optional drawWidth As Integer = 1, Optional drawColor As Color = Nothing, Optional fillColor As Color = Nothing, Optional filled As Boolean = False)
        DrawElements.Add(New RectangleElement(Me, x1, y1, x2, y2, relativeCoords, drawWidth, If(drawColor = Nothing, Color.Black, drawColor), If(fillColor = Nothing, Color.White, fillColor), filled, layerNo))
        Invalidate()
    End Sub

    ''' <summary>
    ''' Draws a text label on the specified layer.
    ''' </summary>
    ''' <param name="layerNo">The layer number to draw on.</param>
    ''' <param name="text">The text to display.</param>
    ''' <param name="x1">The x-coordinate of the label's position.</param>
    ''' <param name="y1">The y-coordinate of the label's position.</param>
    ''' <param name="drawColor">The color of the text (defaults to Black).</param>
    ''' <param name="fontSize">The size of the font in points (defaults to 8).</param>
    Public Sub DrawLabel(ByVal layerNo As Integer, text As String, x1 As Single, y1 As Single, Optional drawColor As Color = Nothing, Optional fontSize As Integer = 8)
        DrawElements.Add(New LabelElement(Me, x1, y1, text, If(drawColor = Nothing, Color.Black, drawColor), fontSize, layerNo))
        Invalidate()
    End Sub
#End Region
#Region "Main Draw Functions"
    ''' <summary>
    ''' Paints the control, including background, grid, axes, and drawing elements.
    ''' </summary>
    ''' <param name="e">The PaintEventArgs containing event data.</param>
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics
        'Using g As Graphics = Me.CreateGraphics()

        If _backgroundImage IsNot Nothing Then
            g.DrawImage(_backgroundImage, 0, 0, Me.Width, Me.Height)
        Else
            g.Clear(Color.White)
        End If
        '
        Draw_Grid(g)
        Draw_Axes(g)
        '
        For Each element In DrawElements
            Dim layer As LayerClass = Nothing
            If _layers.ContainsKey(element.LayerNo) AndAlso _layers(element.LayerNo).Visible Then
                element.Draw(g, CurrentViewSettings.ZoomFactor, CurrentViewSettings.HorizontalShift, CurrentViewSettings.VerticalShift, Me.Height)
            End If
        Next
        ' Draw the scale bar
        DrawScaleBar(g)
        'End Using
    End Sub

    ''' <summary>
    ''' Draws the grid lines on the graphics surface.
    ''' </summary>
    ''' <param name="g">The Graphics object to draw on.</param>
    Private Sub Draw_Grid(g As Graphics)
        Dim grid As Double = DIST_Real_to_Pict(_GridSpacing, CurrentViewSettings.ZoomFactor)
        If Draw_Grid_boolean AndAlso grid >= 0.1 Then
            Dim param As Double = CurrentViewSettings.HorizontalShift - Math.Floor(CurrentViewSettings.HorizontalShift / grid) * grid
            Dim param2 As Double = CurrentViewSettings.VerticalShift - Math.Floor(CurrentViewSettings.VerticalShift / grid) * grid
            Using pen As New Pen(Color.LightGray, 1)
                For N As Integer = 1 To CInt(Me.Width / grid) + 1
                    g.DrawLine(pen, CSng(N * grid - param), 0, CSng(N * grid - param), Me.Height)
                Next
                For N As Integer = 0 To CInt(Me.Height / grid)
                    g.DrawLine(pen, 0, CSng(Me.Height - N * grid - param2), Me.Width, CSng(Me.Height - N * grid - param2))
                Next
            End Using
        End If
    End Sub

    ''' <summary>
    ''' Draws the coordinate axes on the graphics surface.
    ''' </summary>
    ''' <param name="g">The Graphics object to draw on.</param>
    Private Sub Draw_Axes(g As Graphics)
        If Draw_Axes_boolean Then
            Using pen As New Pen(Color.Blue, 1)
                Dim x0 As Single = X_Real_to_Pict(0, CurrentViewSettings.ZoomFactor, CurrentViewSettings.HorizontalShift)
                Dim y0 As Single = Y_Real_to_Pict(0, CurrentViewSettings.ZoomFactor, CurrentViewSettings.VerticalShift, Me.Height)
                Dim arrowLength As Single = 10.0F
                Dim arrowWidth As Single = 5.0F
                Dim AxisLength As Single = 80.0F
                g.DrawLine(pen, x0, y0, x0 + AxisLength, y0)
                Dim xArrowStart As Single = x0 + AxisLength - arrowLength
                g.DrawLine(pen, x0 + AxisLength, y0, xArrowStart, y0 - arrowWidth)
                g.DrawLine(pen, x0 + AxisLength, y0, xArrowStart, y0 + arrowWidth)
                g.DrawLine(pen, x0, y0, x0, y0 - AxisLength)
                Dim yArrowStart As Single = arrowLength
                g.DrawLine(pen, x0, y0 - AxisLength, x0 - arrowWidth, y0 - AxisLength + arrowLength)
                g.DrawLine(pen, x0, y0 - AxisLength, x0 + arrowWidth, y0 - AxisLength + arrowLength)
                Using font As New Font("Arial", 14)
                    g.DrawString("x", font, Brushes.Blue, x0 + AxisLength / 2, y0 - 2)
                    g.DrawString("y", font, Brushes.Blue, x0 - 14 - 2, y0 - 14 * 2 - AxisLength / 2)
                End Using
            End Using
        End If
    End Sub
#End Region
#Region "Scale Bar"
    ''' <summary>
    ''' Draws a scale bar in the bottom-left corner of the control.
    ''' </summary>
    ''' <param name="g">The Graphics object to draw on.</param>
    Private Sub DrawScaleBar(g As Graphics)
        If Not _showScaleBar Then Exit Sub

        ' Constants for appearance
        Const margin As Single = 10 ' Margin from edges
        Const barHeight As Single = 5 ' Height of tick marks
        Const fontSize As Single = 10 ' Font size for label

        ' Calculate a "nice" real-world distance based on target pixel length
        Dim targetPixelLength As Single = _scaleBarLengthPixels
        Dim realDistance As Single = targetPixelLength * CurrentViewSettings.ZoomFactor ' Convert pixels to real units
        Dim niceDistance As Single = GetNiceDistance(realDistance) ' Adjust to a round number
        Dim pixelLength As Single = DIST_Real_to_Pict(niceDistance, CurrentViewSettings.ZoomFactor) ' Convert back to pixels

        ' Position in bottom-left corner
        Dim xStart As Single = margin
        Dim xEnd As Single = xStart + pixelLength
        Dim yPos As Single = Me.Height - margin - barHeight

        ' Draw the scale bar
        Using pen As New Pen(Color.Black, 1)
            ' Horizontal line
            g.DrawLine(pen, xStart, yPos, xEnd, yPos)
            ' Left tick
            g.DrawLine(pen, xStart, yPos - barHeight, xStart, yPos + barHeight)
            ' Right tick
            g.DrawLine(pen, xEnd, yPos - barHeight, xEnd, yPos + barHeight)
        End Using

        ' Draw the label
        Dim label As String = $"{niceDistance:F0} Units" ' Customize units as needed
        Using font As New Font("Arial", fontSize), brush As New SolidBrush(Color.Black)
            g.DrawString(label, font, brush, xStart + pixelLength / 2 - fontSize * label.Length / 4, yPos - fontSize - 4)
        End Using
    End Sub

    ''' <summary>
    ''' Adjusts a real-world distance to a "nice" round number (e.g., 1, 5, 10, 50).
    ''' </summary>
    ''' <param name="distance">The raw real-world distance.</param>
    ''' <returns>A rounded, user-friendly distance.</returns>
    Private Function GetNiceDistance(distance As Single) As Single
        Dim magnitude As Integer = CInt(Math.Floor(Math.Log10(distance)))
        Dim normalized As Single = distance / CSng(Math.Pow(10, magnitude))
        Dim niceValue As Single

        If normalized >= 5 Then
            niceValue = 10
        ElseIf normalized >= 2 Then
            niceValue = 5
        Else
            niceValue = 1
        End If

        Return niceValue * CSng(Math.Pow(10, magnitude))
    End Function
#End Region
End Class

