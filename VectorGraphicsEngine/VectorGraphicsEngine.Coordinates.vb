Partial Public Class VectorGraphicsEngine
    'Handles coordinate transformation logic.


#Region "Extents"
    ''' <summary>
    ''' Gets the bounding rectangle that encompasses all visible elements.
    ''' </summary>
    ''' <returns>A RectangleF representing the extents, or RectangleF.Empty if no elements are visible.</returns>
    Public Function GetExtents() As RectangleF
        If DrawElements.Count = 0 Then Return RectangleF.Empty

        ' Filter elements based on the Visible property of their corresponding layer
        Dim visibleElements = DrawElements.Where(Function(e) _layers.ContainsKey(e.LayerNo) AndAlso _layers(e.LayerNo).Visible)

        If Not visibleElements.Any() Then Return RectangleF.Empty

        ' Calculate the union of bounds for visible elements
        Dim bounds = visibleElements.First().GetBounds()
        For Each element In visibleElements.Skip(1)
            bounds = RectangleF.Union(bounds, element.GetBounds())
        Next

        Return bounds
    End Function

    ''' <summary>
    ''' Adjusts the view to fit all visible elements with optional padding.
    ''' </summary>
    ''' <param name="PaddingPercent">The percentage of padding to add around the extents.</param>
    Public Sub FitToExtents(Optional PaddingPercent As Single = 5.0F)
        Dim extents = GetExtents()
        If extents.IsEmpty Then Exit Sub
        Dim RegionFitSettings As VectorGraphicsEngine.ViewSettings = GetRegionViewSettings(GetExtents, Me.Width, Me.Height, PaddingPercent)
        CurrentViewSettings = RegionFitSettings
        Invalidate()
    End Sub

    ''' <summary>
    ''' Calculates view settings to fit a specific region into a given pixel area.
    ''' </summary>
    ''' <param name="region">The region in real-world coordinates to fit.</param>
    ''' <param name="PixelWidth">The width of the target area in pixels.</param>
    ''' <param name="PixelHeight">The height of the target area in pixels.</param>
    ''' <param name="paddingPercent">The percentage of padding to add around the region.</param>
    ''' <returns>A ViewSettings structure with the calculated zoom and offsets.</returns>
    Public Function GetRegionViewSettings(region As RectangleF, PixelWidth As Single, PixelHeight As Single, Optional paddingPercent As Single = 5.0F) As ViewSettings
        If region.Width <= 0 OrElse region.Height <= 0 Then
            Throw New ArgumentException("Region width and height must be positive.")
        End If
        Dim paddingX As Single = region.Width * paddingPercent / 100.0F
        Dim paddingY As Single = region.Height * paddingPercent / 100.0F
        region.Inflate(paddingX, paddingY)
        Dim scaleX As Single = PixelWidth / region.Width
        Dim scaleY As Single = PixelHeight / region.Height
        Dim zoomFactor As Single = 1 / Math.Min(scaleX, scaleY)
        Dim centerXReal As Single = region.X + region.Width / 2
        Dim centerYReal As Single = region.Y + region.Height / 2
        Dim centerXPixel As Single = PixelWidth / 2
        Dim centerYPixel As Single = PixelHeight / 2
        Dim hShift As Double = X_Real_to_Pict(centerXReal, zoomFactor, 0) - centerXPixel
        Dim vShift As Double = Y_Real_to_Pict(centerYReal, zoomFactor, 0, PixelHeight) - centerYPixel
        Return New ViewSettings(zoomFactor, hShift, vShift)
    End Function
#End Region


#Region "Coordinate conversion methods"
    ''' <summary>
    ''' Converts an x-coordinate from real-world to pixel coordinates.
    ''' </summary>
    ''' <param name="t">The real-world x-coordinate.</param>
    ''' <param name="zoomFactor">The current zoom factor.</param>
    ''' <param name="horizontalOffset">The horizontal offset in pixels.</param>
    ''' <returns>The x-coordinate in pixel space.</returns>
    Friend Shared Function X_Real_to_Pict(t As Single, zoomFactor As Single, horizontalOffset As Double) As Single
        Dim result As Single = t / zoomFactor - CSng(horizontalOffset)
        Return result
    End Function

    ''' <summary>
    ''' Converts a y-coordinate from real-world to pixel coordinates.
    ''' </summary>
    ''' <param name="t">The real-world y-coordinate.</param>
    ''' <param name="zoomFactor">The current zoom factor.</param>
    ''' <param name="VerticalShift">The vertical offset in pixels.</param>
    ''' <param name="height">The height of the drawing area in pixels.</param>
    ''' <returns>The y-coordinate in pixel space.</returns>
    Friend Shared Function Y_Real_to_Pict(t As Single, zoomFactor As Single, VerticalShift As Double, height As Integer) As Single
        Return height - (t / zoomFactor + CSng(VerticalShift))
    End Function

    ''' <summary>
    ''' Converts a distance from real-world to pixel coordinates.
    ''' </summary>
    ''' <param name="t">The real-world distance.</param>
    ''' <param name="zoomFactor">The current zoom factor.</param>
    ''' <returns>The distance in pixel space.</returns>
    Friend Shared Function DIST_Real_to_Pict(t As Single, zoomFactor As Single) As Single
        Return t / zoomFactor
    End Function

    ''' <summary>
    ''' Converts an x-coordinate from pixel to real-world coordinates.
    ''' </summary>
    ''' <param name="x">The pixel x-coordinate.</param>
    ''' <param name="zoomFactor">The current zoom factor.</param>
    ''' <param name="horizontalOffset">The horizontal offset in pixels.</param>
    ''' <returns>The x-coordinate in real-world space.</returns>
    Private Function X_Pict_to_Real(x As Single, zoomFactor As Single, horizontalOffset As Double) As Single
        Return (x + CSng(horizontalOffset)) * zoomFactor
    End Function

    ''' <summary>
    ''' Converts a y-coordinate from pixel to real-world coordinates.
    ''' </summary>
    ''' <param name="y">The pixel y-coordinate.</param>
    ''' <param name="zoomFactor">The current zoom factor.</param>
    ''' <param name="VerticalShift">The vertical offset in pixels.</param>
    ''' <param name="height">The height of the drawing area in pixels.</param>
    ''' <returns>The y-coordinate in real-world space.</returns>
    Private Function Y_Pict_to_Real(y As Single, zoomFactor As Single, VerticalShift As Double, height As Integer) As Single
        Return (height - y - CSng(VerticalShift)) * zoomFactor
    End Function

    ''' <summary>
    ''' Handles the load event of the control.
    ''' </summary>
    ''' <param name="sender">The source of the event.</param>
    ''' <param name="e">The EventArgs containing event data.</param>
    Private Sub VectorGraphicsEngine_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'add command upon your needs
    End Sub
#End Region

End Class


