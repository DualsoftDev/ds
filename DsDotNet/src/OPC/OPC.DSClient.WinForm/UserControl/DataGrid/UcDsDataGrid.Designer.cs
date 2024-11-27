namespace OPC.DSClient.WinForm.UserControl
{
    partial class UcDsDataGrid
    {
        private void InitializeComponent()
        {
            this.gridControl1 = new DevExpress.XtraGrid.GridControl();
            this.gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colSensor = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colCount = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colMovingAVG = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colMovingSTD = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colActiveTime = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colWaitingTime = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colMovingTime = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colRatio = new DevExpress.XtraGrid.Columns.GridColumn();
            this.repositoryItemProgressBar = new DevExpress.XtraEditors.Repository.RepositoryItemProgressBar();
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemProgressBar)).BeginInit();
            this.SuspendLayout();
            // 
            // gridControl1
            // 
            this.gridControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridControl1.Location = new System.Drawing.Point(0, 0);
            this.gridControl1.MainView = this.gridView1;
            this.gridControl1.Name = "gridControl1";
            this.gridControl1.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.repositoryItemProgressBar});

            this.gridControl1.Size = new System.Drawing.Size(1200, 600);
            this.gridControl1.TabIndex = 0;
            this.gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridView1});
            // 
            // gridView1
            // 
            this.gridView1.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colName,
            this.colSensor,
            this.colCount,
            this.colMovingAVG,
            this.colMovingSTD,
            this.colActiveTime,
            this.colWaitingTime,
            this.colMovingTime,
            this.colRatio});
            this.gridView1.GridControl = this.gridControl1;
            this.gridView1.Name = "gridView1";
            this.gridView1.OptionsView.ShowAutoFilterRow = true;
            this.gridView1.OptionsView.ShowGroupPanel = false;
            this.gridView1.OptionsView.ColumnAutoWidth = false;
            this.gridView1.BestFitColumns(); // 모든 컬럼 최적 크기로 설정
            // 
            // colName
            // 
            this.colName.Caption = "Name";
            this.colName.FieldName = "Name";
            this.colName.Name = "colName";
            this.colName.Visible = true;
            this.colName.VisibleIndex = 0;
            this.colName.Width = 150;
            // 
            // colSensor
            // 
            this.colSensor.Caption = "Sensor";
            this.colSensor.FieldName = "Sensor";
            this.colSensor.Name = "colSensor";
            this.colSensor.Visible = true;
            this.colSensor.VisibleIndex = 1;
            this.colSensor.Width = 150;
            // 
            // colMovingAVG
            // 
            this.colMovingAVG.Caption = "Moving AVG";
            this.colMovingAVG.FieldName = "MovingAVG";
            this.colMovingAVG.Name = "colMovingAVG";
            this.colMovingAVG.Visible = true;
            this.colMovingAVG.VisibleIndex = 2;
            this.colMovingAVG.Width = 150;
            // 
            // colMovingSTD
            // 
            this.colMovingSTD.Caption = "Moving STD";
            this.colMovingSTD.FieldName = "MovingSTD";
            this.colMovingSTD.Name = "colMovingSTD";
            this.colMovingSTD.Visible = true;
            this.colMovingSTD.VisibleIndex = 3;
            this.colMovingSTD.Width = 150;
            // 
            // colActiveTime
            // 
            this.colActiveTime.Caption = "Active Time";
            this.colActiveTime.FieldName = "ActiveTime";
            this.colActiveTime.Name = "colActiveTime";
            this.colActiveTime.Visible = true;
            this.colActiveTime.VisibleIndex = 4;
            this.colActiveTime.Width = 150;
            // 
            // colWaitingTime
            // 
            this.colWaitingTime.Caption = "Waiting Time";
            this.colWaitingTime.FieldName = "WaitingTime";
            this.colWaitingTime.Name = "colWaitingTime";
            this.colWaitingTime.Visible = true;
            this.colWaitingTime.VisibleIndex = 5;
            this.colWaitingTime.Width = 150;
            // 
            // colMovingTime
            // 
            this.colMovingTime.Caption = "Moving Time";
            this.colMovingTime.FieldName = "MovingTime";
            this.colMovingTime.Name = "colMovingTime";
            this.colMovingTime.Visible = true;
            this.colMovingTime.VisibleIndex = 6;
            this.colMovingTime.Width = 150;
            // 
            // colRatio
            // 
            this.colRatio.Caption = "Waiting/Active Ratio";
            this.colRatio.FieldName = "Ratio";
            this.colRatio.ColumnEdit = this.repositoryItemProgressBar; // ProgressBar와 값 표시
            this.colRatio.Name = "colRatio";
            this.colRatio.Visible = true;
            this.colRatio.VisibleIndex = 7;
            this.colRatio.Width = 150;
            // 
            // colCount
            // 
            this.colCount.Caption = "Count";
            this.colCount.FieldName = "Count";
            this.colCount.Name = "colCount";
            this.colCount.Visible = true;
            this.colCount.VisibleIndex = 8;
            this.colCount.Width = 150;
            // 
            // repositoryItemProgressBar
            // 
            this.repositoryItemProgressBar.Name = "repositoryItemProgressBar";
            this.repositoryItemProgressBar.Maximum = 100;
            this.repositoryItemProgressBar.ShowTitle = true; // 값과 ProgressBar 함께 표시
            this.repositoryItemProgressBar.StartColor = System.Drawing.Color.Green;
            this.repositoryItemProgressBar.EndColor = System.Drawing.Color.Red;
            // 
            // UcDsDataGrid
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gridControl1);
            this.Name = "UcDsDataGrid";
            this.Size = new System.Drawing.Size(1200, 600);
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemProgressBar)).EndInit();
            this.ResumeLayout(false);
        }

        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraGrid.Columns.GridColumn colName;
        private DevExpress.XtraGrid.Columns.GridColumn colSensor;
        private DevExpress.XtraGrid.Columns.GridColumn colCount;
        private DevExpress.XtraGrid.Columns.GridColumn colMovingAVG;
        private DevExpress.XtraGrid.Columns.GridColumn colMovingSTD;
        private DevExpress.XtraGrid.Columns.GridColumn colActiveTime;
        private DevExpress.XtraGrid.Columns.GridColumn colWaitingTime;
        private DevExpress.XtraGrid.Columns.GridColumn colMovingTime;
        private DevExpress.XtraGrid.Columns.GridColumn colRatio;
        private DevExpress.XtraEditors.Repository.RepositoryItemProgressBar repositoryItemProgressBar;
    }
}
