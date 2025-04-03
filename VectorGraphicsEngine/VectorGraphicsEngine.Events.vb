Partial Public Class VectorGraphicsEngine
    'Manages mouse and user interaction events.


#Region "Event handler"
    ''' <summary>
    ''' Occurs when the mouse button is pressed within the control.
    ''' </summary>
    Public Event VectorMouseDown As EventHandler(Of MouseEventArgs)

    ''' <summary>
    ''' Occurs when the mouse moves over the control.
    ''' </summary>
    Public Event VectorMouseMove As EventHandler(Of MouseEventArgs)

    ''' <summary>
    ''' Occurs adding draw element to a layer that has not been initialized.
    ''' </summary>
    Public Event LayerNotInitializedWarning As EventHandler(Of LayerNotInitializedEventArgs)

#End Region


#Region " event handlers"
    ''' <summary>
    ''' Handles the mouse move event to update coordinates and enable panning.
    ''' </summary>
    ''' <param name="e">The MouseEventArgs containing event data.</param>
    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        Dim realX As Single = X_Pict_to_Real(e.X, CurrentViewSettings.ZoomFactor, CurrentViewSettings.HorizontalShift)
        Dim realY As Single = Y_Pict_to_Real(e.Y, CurrentViewSettings.ZoomFactor, CurrentViewSettings.VerticalShift, Me.Height)
        MouseLocationPixel = New PointF(e.X, e.Y)
        MouseLocationReal = New PointF(realX, realY)
        If Not IsNothing(LblCoordianates) Then
            LblCoordianates.Text = "Location:" + Str(Int(realX * 10) / 10) + " , " + Str(Int(realY * 10) / 10)
        End If
        RaiseEvent VectorMouseMove(Me, e)
        If isPanning Then
            Dim dx As Integer = e.X - panStartX
            Dim dy As Integer = e.Y - panStartY
            CurrentViewSettings = New ViewSettings(CurrentViewSettings.ZoomFactor, CurrentViewSettings.HorizontalShift - dx, CurrentViewSettings.VerticalShift - dy)
            panStartX = e.X
            panStartY = e.Y
            Invalidate()
        End If
    End Sub

    ''' <summary>
    ''' Handles the mouse down event to initiate panning and raise the VectorMouseDown event.
    ''' </summary>
    ''' <param name="e">The MouseEventArgs containing event data.</param>
    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        Dim realX As Single = X_Pict_to_Real(e.X, CurrentViewSettings.ZoomFactor, CurrentViewSettings.HorizontalShift)
        Dim realY As Single = Y_Pict_to_Real(e.Y, CurrentViewSettings.ZoomFactor, CurrentViewSettings.VerticalShift, Me.Height)
        RaiseEvent VectorMouseDown(Me, e)
        If e.Button = MouseButtons.Middle Then
            isPanning = True
            panStartX = e.X
            panStartY = e.Y
            Me.Cursor = Cursors.Hand
        End If
    End Sub

    ''' <summary>
    ''' Handles the mouse wheel event to zoom in or out.
    ''' </summary>
    ''' <param name="e">The MouseEventArgs containing event data.</param>
    Protected Overrides Sub OnMouseWheel(e As MouseEventArgs)
        MyBase.OnMouseWheel(e)
        Dim zoomFactor As Single = If(e.Delta < 0, 1.1F, 0.9F)
        Dim oldScale As Single = Me.CurrentViewSettings.ZoomFactor
        Me.CurrentViewSettings.ZoomFactor *= zoomFactor
        Dim pivotXPixel As Single = e.X
        Dim pivotYPixel As Single = e.Y
        Dim pivotXRealBefore As Single = X_Pict_to_Real(pivotXPixel, oldScale, CurrentViewSettings.HorizontalShift)
        Dim pivotYRealBefore As Single = Y_Pict_to_Real(pivotYPixel, oldScale, CurrentViewSettings.VerticalShift, Me.Height)
        Dim pivotXPixelAfter As Single = X_Real_to_Pict(pivotXRealBefore, Me.CurrentViewSettings.ZoomFactor, CurrentViewSettings.HorizontalShift)
        Dim pivotYPixelAfter As Single = Y_Real_to_Pict(pivotYRealBefore, Me.CurrentViewSettings.ZoomFactor, CurrentViewSettings.VerticalShift, Me.Height)
        CurrentViewSettings.HorizontalShift += pivotXPixelAfter - pivotXPixel
        CurrentViewSettings.VerticalShift += pivotYPixelAfter - pivotYPixel
        Invalidate()
    End Sub

    ''' <summary>
    ''' Handles the mouse up event to end panning.
    ''' </summary>
    ''' <param name="e">The MouseEventArgs containing event data.</param>
    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        MyBase.OnMouseUp(e)
        If e.Button = MouseButtons.Middle Then
            isPanning = False
            Me.Cursor = Cursors.Default
        End If
    End Sub
#End Region

End Class
