namespace OPC.DSClient.WinForm.UserControl
{
    partial class UcDsTreemap
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
            DevExpress.XtraTreeMap.HatchFillStyle hatchFillStyle1 = new DevExpress.XtraTreeMap.HatchFillStyle();
            DevExpress.XtraTreeMap.HatchFillStyle hatchFillStyle2 = new DevExpress.XtraTreeMap.HatchFillStyle();
            treeMapControl1 = new DevExpress.XtraTreeMap.TreeMapControl();
            ((System.ComponentModel.ISupportInitialize)treeMapControl1).BeginInit();
            SuspendLayout();
            // 
            // treeMapControl1
            // 
            treeMapControl1.Appearance.GroupStyle.TextGlowColor = Color.Empty;
            treeMapControl1.Appearance.HighlightedLeafStyle.FillStyle = hatchFillStyle1;
            treeMapControl1.Appearance.SelectedLeafStyle.FillStyle = hatchFillStyle2;
            treeMapControl1.Dock = DockStyle.Fill;
            treeMapControl1.Location = new Point(0, 0);
            treeMapControl1.Name = "treeMapControl1";
            treeMapControl1.Size = new Size(901, 552);
            treeMapControl1.TabIndex = 0;
            // 
            // UcDsTreemap
            // 
            AutoScaleDimensions = new SizeF(7F, 16F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(treeMapControl1);
            Name = "UcDsTreemap";
            Size = new Size(901, 552);
            ((System.ComponentModel.ISupportInitialize)treeMapControl1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraTreeMap.TreeMapControl treeMapControl1;
    }
}
