Public Class Form3
    Private planetsLayer As Integer
    Private angle1, angle2, angle3 As Single ' Angles for each planet's orbit
    Private Sub Form3_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Initialize layers and timer
        planetsLayer = Engine.AddLayer()
        animationTimer.Interval = 50 ' Update every 50ms for smooth animation
        animationTimer.Start()

        ' Initial angles for planets
        angle1 = 0
        angle2 = 0
        angle3 = 0

        DrawSun()
    End Sub
    Private Sub DrawSun()
        ' Layer 0: Draw the static sun
        Engine.ClearDrawing()
        Engine.DrawCircle(0, 0, 0, 2, Color.Yellow, True, Color.Orange, False, 2) ' Sun with outline
    End Sub

    Private Sub DrawPlanets()
        ' Clear the planets layer only
        Engine.ClearDrawingLayer(planetsLayer)

        ' Planet 1: Small, fast, blue
        Dim radius1X As Single = 3 ' Horizontal radius
        Dim radius1Y As Single = 2 ' Vertical radius (elliptical orbit)
        Dim x1 As Single = radius1X * Math.Cos(angle1)
        Dim y1 As Single = radius1Y * Math.Sin(angle1)
        Engine.DrawCircle(planetsLayer, x1, y1, 0.3, Color.Blue, True, Color.DarkBlue, False, 1)

        ' Planet 2: Medium, moderate speed, green
        Dim radius2X As Single = 5
        Dim radius2Y As Single = 3
        Dim x2 As Single = radius2X * Math.Cos(angle2)
        Dim y2 As Single = radius2Y * Math.Sin(angle2)
        Engine.DrawCircle(planetsLayer, x2, y2, 0.5, Color.Green, True, Color.DarkGreen, False, 1)

        ' Planet 3: Large, slow, red
        Dim radius3X As Single = 7
        Dim radius3Y As Single = 4
        Dim x3 As Single = radius3X * Math.Cos(angle3)
        Dim y3 As Single = radius3Y * Math.Sin(angle3)
        Engine.DrawCircle(planetsLayer, x3, y3, 0.7, Color.Red, True, Color.DarkRed, False, 1)

        ' Fit the view to show the entire animation
        Engine.FitToExtents(8.0F)
    End Sub

    Private Sub AnimationTimer_Tick(sender As Object, e As EventArgs) Handles animationTimer.Tick
        ' Update angles for each planet (different speeds)
        angle1 += 0.05F ' Fast orbit
        angle2 += 0.03F ' Medium orbit
        angle3 += 0.01F ' Slow orbit

        ' Redraw the planets with updated positions
        DrawPlanets()
    End Sub
End Class