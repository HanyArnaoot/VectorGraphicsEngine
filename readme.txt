# VectorGraphicsEngine
A VB.NET UserControl for vector graphics with zooming, panning, and layering.

## Requirements
- Visual Studio (2019 or later recommended)
- .NET Framework 4.8

## Setup
1. Clone or download this repository.
2. Open `VectorGraphicsEngine.vbproj` in Visual Studio.
3. Build the solution to use the control in your projects.

## Usage
```vb.net
Dim engine As New VectorGraphicsEngine()
engine.DrawLine(0, 0, 0, 10, 10, Color.Blue)
engine.ShowScaleBar = True
Me.Controls.Add(engine)