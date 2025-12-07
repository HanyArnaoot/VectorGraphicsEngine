namespace Arnaoot.VectorGraphics.UI
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            vectorDrawEngine1 = new VectorDrawEngine.VectorDrawEngine();
            SuspendLayout();
            // 
            // vectorDrawEngine1
            // 
            vectorDrawEngine1.BackColor = SystemColors.Control;
            vectorDrawEngine1.BackgroundColor = SystemColors.Control;
            vectorDrawEngine1.Dock = DockStyle.Fill;
            vectorDrawEngine1.GridSpacing = 50F;
            vectorDrawEngine1.Location = new Point(0, 0);
            vectorDrawEngine1.MinZoomRegionSize = 1F;
            vectorDrawEngine1.Name = "vectorDrawEngine1";
            vectorDrawEngine1.ShowAxes = true;
            vectorDrawEngine1.ShowGrid = false;
            vectorDrawEngine1.ShowScaleBar = true;
            vectorDrawEngine1.Size = new Size(800, 450);
            vectorDrawEngine1.TabIndex = 0;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(vectorDrawEngine1);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
        }

        #endregion

        private VectorDrawEngine.VectorDrawEngine vectorDrawEngine1;
    }
}