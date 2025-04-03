Partial Public Class VectorGraphicsEngine
    ' Contains methods For saving the view As an image.


    ''' <summary>
    ''' Saves the current view as an image file.
    ''' </summary>
    ''' <param name="filePath">The path to save the image file.</param>
    ''' <param name="width">The width of the output image in pixels.</param>
    ''' <param name="height">The height of the output image in pixels.</param>
    ''' <param name="format">The image format (defaults to PNG if not specified).</param>
    ''' <param name="includeBackground">Indicates whether to include the background image.</param>
    Public Sub SaveAsImage(filePath As String, width As Integer, height As Integer, Optional format As Imaging.ImageFormat = Nothing, Optional includeBackground As Boolean = True)
        Dim imageFormat As Imaging.ImageFormat = If(format, Imaging.ImageFormat.Png)
        Using bmp As New Bitmap(width, height)
            Using g As Graphics = Graphics.FromImage(bmp)
                If includeBackground AndAlso _backgroundImage IsNot Nothing Then
                    g.DrawImage(_backgroundImage, 0, 0, width, height)
                Else
                    g.Clear(Color.White)
                End If
                Draw_Grid(g)
                Draw_Axes(g)
                '
                For Each element In DrawElements
                    If _layers.ContainsKey(element.LayerNo) AndAlso _layers(element.LayerNo).Visible Then
                        element.Draw(g, CurrentViewSettings.ZoomFactor, CurrentViewSettings.HorizontalShift, CurrentViewSettings.VerticalShift, height)
                    End If
                Next
            End Using
            bmp.Save(filePath, imageFormat)
        End Using
    End Sub

    ''' <summary>
    ''' Saves a specific region of the view as an image file.
    ''' </summary>
    ''' <param name="filePath">The path to save the image file.</param>
    ''' <param name="region">The region in real-world coordinates to save.</param>
    ''' <param name="pixelWidth">The width of the output image in pixels.</param>
    ''' <param name="pixelHeight">The height of the output image in pixels.</param>
    ''' <param name="format">The image format (defaults to PNG if not specified).</param>
    ''' <param name="includeBackground">Indicates whether to include the background image.</param>
    ''' <param name="Padding">The padding percentage around the region.</param>
    Public Sub SaveRegionAsImage(filePath As String, region As RectangleF, pixelWidth As Integer, pixelHeight As Integer, Optional format As Imaging.ImageFormat = Nothing, Optional includeBackground As Boolean = True, Optional Padding As Single = 5.0F)
        If region.Width <= 0 OrElse region.Height <= 0 OrElse pixelWidth <= 0 OrElse pixelHeight <= 0 Then
            Throw New ArgumentException("Region dimensions and pixel size must be positive.")
        End If
        Dim imageFormat As Imaging.ImageFormat = If(format, Imaging.ImageFormat.Png)
        Using bmp As New Bitmap(pixelWidth, pixelHeight)
            Using g As Graphics = Graphics.FromImage(bmp)
                If includeBackground AndAlso _backgroundImage IsNot Nothing Then
                    g.DrawImage(_backgroundImage, 0, 0, pixelWidth, pixelHeight)
                Else
                    g.Clear(Color.White)
                End If
                Dim RegionViewSettings As ViewSettings = GetRegionViewSettings(region, pixelWidth, pixelHeight, Padding)
                For Each element In DrawElements
                    If _layers.ContainsKey(element.LayerNo) Then
                        element.Draw(g, RegionViewSettings.ZoomFactor, RegionViewSettings.HorizontalShift, RegionViewSettings.VerticalShift, pixelHeight)
                    End If
                Next
            End Using
            bmp.Save(filePath, imageFormat)
        End Using
    End Sub

End Class
