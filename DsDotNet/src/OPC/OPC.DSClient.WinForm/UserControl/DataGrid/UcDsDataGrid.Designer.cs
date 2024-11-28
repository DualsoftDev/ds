namespace OPC.DSClient.WinForm.UserControl
{
    partial class UcDsDataGrid
    {
        private void InitializeComponent()
        {
            gridControl1 = new DevExpress.XtraGrid.GridControl();
            gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            colName = new DevExpress.XtraGrid.Columns.GridColumn();
            colFinish = new DevExpress.XtraGrid.Columns.GridColumn();
            colCount = new DevExpress.XtraGrid.Columns.GridColumn();
            colMovingAVG = new DevExpress.XtraGrid.Columns.GridColumn();
            colMovingSTD = new DevExpress.XtraGrid.Columns.GridColumn();
            colActiveTime = new DevExpress.XtraGrid.Columns.GridColumn();
            colWaitingTime = new DevExpress.XtraGrid.Columns.GridColumn();
            colMovingTime = new DevExpress.XtraGrid.Columns.GridColumn();
            colRatio = new DevExpress.XtraGrid.Columns.GridColumn();
            repositoryItemProgressBar = new DevExpress.XtraEditors.Repository.RepositoryItemProgressBar();
            ((System.ComponentModel.ISupportInitialize)gridControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemProgressBar).BeginInit();
            SuspendLayout();
            // 
            // gridControl1
            // 
            gridControl1.Dock = DockStyle.Fill;
            gridControl1.Location = new Point(0, 0);
            gridControl1.MainView = gridView1;
            gridControl1.Name = "gridControl1";
            gridControl1.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] { repositoryItemProgressBar });
            gridControl1.Size = new Size(1200, 600);
            gridControl1.TabIndex = 0;
            gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridView1 });
            // 
            // gridView1
            // 
            gridView1.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] { colName, colFinish, colCount, colRatio, colMovingAVG, colMovingSTD, colActiveTime, colWaitingTime, colMovingTime });
            gridView1.GridControl = gridControl1;
            gridView1.Name = "gridView1";

            // 
            // colName
            // 
            colName.Caption = "Name";
            colName.FieldName = "Name";
            colName.Name = "colName";
            colName.Visible = true;
            colName.VisibleIndex = 0;
            // 
            // colFinish
            // 
            colFinish.Caption = "Finish";
            colFinish.FieldName = "Finish";
            colFinish.Name = "colFinish";
            colFinish.Visible = true;
            colFinish.VisibleIndex = 1;
            // 
            // colCount
            // 
            colCount.Caption = "Count";
            colCount.FieldName = "Count";
            colCount.Name = "colCount";
            colCount.Visible = true;
            colCount.VisibleIndex = 2;
            // 
            // colMovingAVG
            // 
            colMovingAVG.Caption = "Moving AVG";
            colMovingAVG.FieldName = "MovingAVG";
            colMovingAVG.Name = "colMovingAVG";
            colMovingAVG.Visible = true;
            colMovingAVG.VisibleIndex = 8;
            // 
            // colMovingSTD
            // 
            colMovingSTD.Caption = "Moving STD";
            colMovingSTD.FieldName = "MovingSTD";
            colMovingSTD.Name = "colMovingSTD";
            colMovingSTD.Visible = true;
            colMovingSTD.VisibleIndex = 9;
            // 
            // colActiveTime
            // 
            colActiveTime.Caption = "Active Time(ms)";
            colActiveTime.FieldName = "ActiveTime";
            colActiveTime.Name = "colActiveTime";
            colActiveTime.Visible = true;
            colActiveTime.VisibleIndex = 6;
            // 
            // colWaitingTime
            // 
            colWaitingTime.Caption = "Waiting Time(ms)";
            colWaitingTime.FieldName = "WaitingTime";
            colWaitingTime.Name = "colWaitingTime";
            colWaitingTime.Visible = true;
            colWaitingTime.VisibleIndex = 4;
            // 
            // colMovingTime
            // 
            colMovingTime.Caption = "Moving Time(ms)";
            colMovingTime.FieldName = "MovingTime";
            colMovingTime.Name = "colMovingTime";
            colMovingTime.Visible = true;
            colMovingTime.VisibleIndex = 5;
            // 
            // colRatio
            // 
            colRatio.Caption = "Waiting/Active Ratio";
            colRatio.ColumnEdit = repositoryItemProgressBar;
            colRatio.FieldName = "Ratio";
            colRatio.Name = "colRatio";
            colRatio.Visible = true;
            colRatio.VisibleIndex = 3;
            // 
            // repositoryItemProgressBar
            // 
            repositoryItemProgressBar.EndColor = Color.Red;
            repositoryItemProgressBar.Name = "repositoryItemProgressBar";
            repositoryItemProgressBar.StartColor = Color.Green;
            repositoryItemProgressBar.ShowTitle = false; // 값 숨김
            // 
            // UcDsDataGrid
            // 
            AutoScaleDimensions = new SizeF(7F, 14F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(gridControl1);
            Name = "UcDsDataGrid";
            Size = new Size(1200, 600);
            ((System.ComponentModel.ISupportInitialize)gridControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemProgressBar).EndInit();
            ResumeLayout(false);
        }

        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraGrid.Columns.GridColumn colName;
        private DevExpress.XtraGrid.Columns.GridColumn colFinish;
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
