<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class VectorGraphicsEngine
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.LblCoordianates = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'LblCoordianates
        '
        Me.LblCoordianates.AutoSize = True
        Me.LblCoordianates.BackColor = System.Drawing.Color.Transparent
        Me.LblCoordianates.Font = New System.Drawing.Font("Tahoma", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LblCoordianates.Location = New System.Drawing.Point(0, 0)
        Me.LblCoordianates.Name = "LblCoordianates"
        Me.LblCoordianates.Size = New System.Drawing.Size(74, 19)
        Me.LblCoordianates.TabIndex = 0
        Me.LblCoordianates.Text = "location"
        '
        'VectorGraphicsEngine
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.Controls.Add(Me.LblCoordianates)
        Me.Name = "VectorGraphicsEngine"
        Me.Size = New System.Drawing.Size(908, 444)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents LblCoordianates As Label
End Class
