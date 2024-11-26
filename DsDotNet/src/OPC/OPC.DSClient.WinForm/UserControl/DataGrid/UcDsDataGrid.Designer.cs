namespace OPC.DSClient.WinForm.UserControl
{
    partial class UcDsDataGrid
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
            this.gridControl1 = new DevExpress.XtraGrid.GridControl();
            this.gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colVariance = new DevExpress.XtraGrid.Columns.GridColumn();
            this.repositoryItemProgressBar1 = new DevExpress.XtraEditors.Repository.RepositoryItemProgressBar();
            this.colName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colMean = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colSensor = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colCount = new DevExpress.XtraGrid.Columns.GridColumn();
            this.repositoryItemProgressBar2 = new DevExpress.XtraEditors.Repository.RepositoryItemProgressBar();
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemProgressBar1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemProgressBar2)).BeginInit();
            this.SuspendLayout();
            // 
            // gridControl1
            // 
            this.gridControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridControl1.Location = new System.Drawing.Point(0, 0);
            this.gridControl1.MainView = this.gridView1;
            this.gridControl1.Name = "gridControl1";
            this.gridControl1.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.repositoryItemProgressBar1,
            this.repositoryItemProgressBar2});
            this.gridControl1.Size = new System.Drawing.Size(901, 483);
            this.gridControl1.TabIndex = 0;
            this.gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridView1});
            // 
            // gridView1
            // 
            this.gridView1.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colName,
            this.colVariance,
            this.colMean,
            this.colSensor,
            this.colCount});
            this.gridView1.DetailHeight = 306;
            this.gridView1.GridControl = this.gridControl1;
            this.gridView1.Name = "gridView1";
            this.gridView1.OptionsView.ShowAutoFilterRow = true;
            this.gridView1.OptionsView.ShowGroupPanel = false;
            // 
            // Variance
            // 
            this.colVariance.Caption = "Variance";
            this.colVariance.ColumnEdit = this.repositoryItemProgressBar1;
            this.colVariance.FieldName = "Variance";
            this.colVariance.Name = "Variance";
            this.colVariance.Visible = true;
            this.colVariance.VisibleIndex = 1;
            // 
            // repositoryItemProgressBar1
            // 
            this.repositoryItemProgressBar1.Minimum = -100;
            this.repositoryItemProgressBar1.Name = "repositoryItemProgressBar1";
            // 
            // Name
            // 
            this.colName.Caption = "Name";
            this.colName.FieldName = "Name";
            this.colName.Name = "Name";
            this.colName.Visible = true;
            this.colName.VisibleIndex = 0;
            // 
            // Mean
            // 
            this.colMean.Caption = "Mean";
            this.colMean.ColumnEdit = this.repositoryItemProgressBar2; // ProgressBar 적용
            this.colMean.FieldName = "Mean";
            this.colMean.Name = "Mean";
            this.colMean.Visible = true;
            this.colMean.VisibleIndex = 2;
            // 
            // Sensor
            // 
            this.colSensor.Caption = "Sensor";
            this.colSensor.FieldName = "Sensor";
            this.colSensor.Name = "Sensor";
            this.colSensor.Visible = true;
            this.colSensor.VisibleIndex = 3;
            // 
            // Count
            // 
            this.colCount.Caption = "Count";
            this.colCount.FieldName = "Count";
            this.colCount.Name = "Count";
            this.colCount.Visible = true;
            this.colCount.VisibleIndex = 4;
            // 
            // repositoryItemProgressBar2
            // 
            this.repositoryItemProgressBar2.Maximum = 5000;
            this.repositoryItemProgressBar2.Name = "repositoryItemProgressBar2";
            // 
            // UcDsDataGrid
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gridControl1);
            this.Size = new System.Drawing.Size(901, 483);
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemProgressBar1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemProgressBar2)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraGrid.Columns.GridColumn colVariance;
        private DevExpress.XtraEditors.Repository.RepositoryItemProgressBar repositoryItemProgressBar1;
        private DevExpress.XtraGrid.Columns.GridColumn colName;
        private DevExpress.XtraGrid.Columns.GridColumn colMean;
        private DevExpress.XtraGrid.Columns.GridColumn colSensor;
        private DevExpress.XtraGrid.Columns.GridColumn colCount;
        private DevExpress.XtraEditors.Repository.RepositoryItemProgressBar repositoryItemProgressBar2;

      
    }
}
