<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form3
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form3))
        Me.BtnResetView = New System.Windows.Forms.Button()
        Me.BtnToggleLayer = New System.Windows.Forms.Button()
        Me.LblCoordinates = New System.Windows.Forms.Label()
        Me.Engine = New VectorGraphicsEngine.VectorGraphicsEngine()
        Me.animationTimer = New System.Windows.Forms.Timer(Me.components)
        Me.SuspendLayout()
        '
        'BtnResetView
        '
        Me.BtnResetView.Location = New System.Drawing.Point(136, 407)
        Me.BtnResetView.Name = "BtnResetView"
        Me.BtnResetView.Size = New System.Drawing.Size(81, 31)
        Me.BtnResetView.TabIndex = 10
        Me.BtnResetView.Text = "ResetView"
        Me.BtnResetView.UseVisualStyleBackColor = True
        '
        'BtnToggleLayer
        '
        Me.BtnToggleLayer.Location = New System.Drawing.Point(35, 407)
        Me.BtnToggleLayer.Name = "BtnToggleLayer"
        Me.BtnToggleLayer.Size = New System.Drawing.Size(81, 31)
        Me.BtnToggleLayer.TabIndex = 9
        Me.BtnToggleLayer.Text = "ToggleLayer"
        Me.BtnToggleLayer.UseVisualStyleBackColor = True
        '
        'LblCoordinates
        '
        Me.LblCoordinates.AutoSize = True
        Me.LblCoordinates.Font = New System.Drawing.Font("Tahoma", 14.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LblCoordinates.Location = New System.Drawing.Point(256, 408)
        Me.LblCoordinates.Name = "LblCoordinates"
        Me.LblCoordinates.Size = New System.Drawing.Size(73, 23)
        Me.LblCoordinates.TabIndex = 8
        Me.LblCoordinates.Text = "Label1"
        '
        'Engine
        '
        Me.Engine.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.Engine.DisplayScale = 1.0!
        Me.Engine.GridSpacing = 1.0!
        Me.Engine.Location = New System.Drawing.Point(35, 12)
        Me.Engine.Name = "Engine"
        Me.Engine.ShowAxes = True
        Me.Engine.ShowGrid = False
        Me.Engine.ShowScaleBar = True
        Me.Engine.Size = New System.Drawing.Size(730, 377)
        Me.Engine.TabIndex = 7
        '
        'animationTimer
        '
        Me.animationTimer.Enabled = True
        Me.animationTimer.Interval = 500
        '
        'Form3
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(800, 450)
        Me.Controls.Add(Me.BtnResetView)
        Me.Controls.Add(Me.BtnToggleLayer)
        Me.Controls.Add(Me.LblCoordinates)
        Me.Controls.Add(Me.Engine)
        Me.Name = "Form3"
        Me.Text = "Form3"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents BtnResetView As Button
    Friend WithEvents BtnToggleLayer As Button
    Friend WithEvents LblCoordinates As Label
    Friend WithEvents Engine As VectorGraphicsEngine.VectorGraphicsEngine
    Friend WithEvents animationTimer As Timer
End Class
