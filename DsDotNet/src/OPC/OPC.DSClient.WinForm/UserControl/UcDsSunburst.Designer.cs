namespace OPC.DSClient.WinForm.UserControl
{
    partial class UcDsSunburst
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            sunburstControl1 = new DevExpress.XtraTreeMap.SunburstControl();
            ((System.ComponentModel.ISupportInitialize)sunburstControl1).BeginInit();
            SuspendLayout();
            // 
            // sunburstControl1
            // 
            sunburstControl1.Dock = DockStyle.Fill;
            sunburstControl1.Label.AutoLayout = false;
            sunburstControl1.Label.Visible = true;
            sunburstControl1.Location = new Point(0, 0);
            sunburstControl1.Name = "sunburstControl1";
            sunburstControl1.Padding = new Padding(2);
            sunburstControl1.Size = new Size(901, 552);
            sunburstControl1.TabIndex = 0;
            // 
            // UcDsSunburst
            // 
            AutoScaleDimensions = new SizeF(7F, 16F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(sunburstControl1);
            Name = "UcDsSunburst";
            Size = new Size(901, 552);
            ((System.ComponentModel.ISupportInitialize)sunburstControl1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraTreeMap.SunburstControl sunburstControl1;
    }
}
