namespace OPC.DSClient.WinForm.UserControl
{
    partial class UcDsSankey
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
            sankeyDiagramControl1 = new DevExpress.XtraCharts.Sankey.SankeyDiagramControl();
            SuspendLayout();
            // 
            // sankeyDiagramControl1
            // 
            sankeyDiagramControl1.Dock = DockStyle.Fill;
            sankeyDiagramControl1.Location = new Point(0, 0);
            sankeyDiagramControl1.Name = "sankeyDiagramControl1";
            sankeyDiagramControl1.Size = new Size(901, 552);
            sankeyDiagramControl1.TabIndex = 0;
            sankeyDiagramControl1.Text = "sankeyDiagramControl1";
            // 
            // UcDsSankey
            // 
            AutoScaleDimensions = new SizeF(7F, 16F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(sankeyDiagramControl1);
            Name = "UcDsSankey";
            Size = new Size(901, 552);
            ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraCharts.Sankey.SankeyDiagramControl sankeyDiagramControl1;
    }
}
