namespace OPC.DSClient.WinForm.UserControl
{
    partial class UcDsHeatmap
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

      

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            heatmapControl1 = new DevExpress.XtraCharts.Heatmap.HeatmapControl();
            SuspendLayout();
            // 
            // heatmapControl1
            // 
            heatmapControl1.Border.Visibility = DevExpress.Utils.DefaultBoolean.False;
            heatmapControl1.Dock = DockStyle.Fill;
            heatmapControl1.Legend.LegendID = -1;
            heatmapControl1.Legend.Visibility = DevExpress.Utils.DefaultBoolean.True;
            heatmapControl1.Location = new Point(0, 0);
            heatmapControl1.Name = "heatmapControl1";
            heatmapControl1.Size = new Size(901, 552);
            heatmapControl1.TabIndex = 0;
            heatmapControl1.Text = "heatmapControl1";
            // 
            // UcDsHeatmap
            // 
            AutoScaleDimensions = new SizeF(7F, 16F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(heatmapControl1);
            Name = "UcDsHeatmap";
            Size = new Size(901, 552);
            ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraCharts.Heatmap.HeatmapControl heatmapControl1;
    }
}
