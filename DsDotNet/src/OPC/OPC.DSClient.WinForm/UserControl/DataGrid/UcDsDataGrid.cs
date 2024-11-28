using DevExpress.Sparkline;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace OPC.DSClient.WinForm.UserControl
{
    public partial class UcDsDataGrid : XtraUserControl
    {
        private OpcTagManager _opcTagManager;
        private BindingList<GridItem> _bindingList;
        private Timer _refreshTimer; // 타이머 선언
        private Dictionary<int, double> _fadeProgress; // 각 RowHandle에 대한 색상 페이드 상태 관리

        public UcDsDataGrid()
        {
            InitializeComponent();

            // GridView 설정
            gridView1.OptionsView.ShowAutoFilterRow = true; // Enable filter row
            gridView1.OptionsView.ShowGroupPanel = false; // Disable group panel
            gridView1.OptionsBehavior.Editable = false; // Read-only grid
            gridView1.OptionsView.ColumnAutoWidth = true;

            AddSparklineColumn(); // 스파크라인 열 추가

            _fadeProgress = new Dictionary<int, double>();
            InitializeTimer(); // 타이머 초기화
            gridView1.RowCellStyle += GridView1_RowCellStyle; // RowCellStyle 이벤트 등록
        }

        private void InitializeTimer()
        {
            _refreshTimer = new Timer();
            _refreshTimer.Interval = 100; // 100ms 간격
            _refreshTimer.Tick += (s, e) => RefreshData();
        }

        /// <summary>
        /// 스파크라인 열을 생성하고 추가합니다.
        /// </summary>
        private void AddSparklineColumn()
        {
            // 스파크라인 RepositoryItem 생성 및 설정
            RepositoryItemSparklineEdit repositoryItemSparklineEdit = new RepositoryItemSparklineEdit();
            LineSparklineView lineSparklineView = repositoryItemSparklineEdit.View as LineSparklineView;

            // 주요 지점 강조 설정
            if (lineSparklineView != null)
            {
                lineSparklineView.HighlightEndPoint = true;
                lineSparklineView.HighlightMaxPoint = true;
                lineSparklineView.HighlightMinPoint = true;
                lineSparklineView.HighlightStartPoint = true;
            }

            // 스파크라인 열 생성 및 추가
            GridColumn colPayments = new GridColumn
            {
                Visible = true,
                Caption = "MovingTimes",
                UnboundDataType = typeof(object),
                ColumnEdit = repositoryItemSparklineEdit,
                FieldName = "MovingTimes", // 사용자 정의 필드명
                MaxWidth = 300,
                MinWidth = 50,
                VisibleIndex = 7,
                Width = 255
            };

            this.gridView1.Columns.Add(colPayments);
            this.gridControl1.RepositoryItems.Add(repositoryItemSparklineEdit);

            // 사용자 정의 열 데이터 처리를 위한 핸들러 등록
            this.gridView1.CustomUnboundColumnData += (s, e) =>
            {
                if (e.IsGetData)
                {
                    e.Value = ((GridItem)e.Row).MovingTimes; // 스파크라인 데이터 설정
                }
            };
        }

        public void SetDataSource(OpcTagManager opcTagManager, bool bFlowMonitor)
        {
            _opcTagManager = opcTagManager;

            try
            {
                // BindingList를 데이터 소스로 설정
                _bindingList = bFlowMonitor ? DataGridUtil.GetDataSourceFlow(_opcTagManager) 
                                            : DataGridUtil.GetDataSourceIO(_opcTagManager);
                gridControl1.DataSource = _bindingList;
                FormatColumnsForFloat(); // 컬럼 포맷 설정
                gridView1.BestFitColumns(); // 컬럼 크기 최적화    

                _refreshTimer.Start(); // 데이터 소스 설정 후 타이머 시작
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data source: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void RefreshData()
        {
            try
            {
                if (Global.SelectedUserControl != this) return; // 현재 선택된 UserControl이 아닌 경우 갱신하지 않음

                if (_opcTagManager != null && _bindingList != null)
                {
                    // 데이터 갱신
                    gridControl1.RefreshDataSource();
                    UpdateFadeStates(); // 페이드 상태 업데이트
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateFadeStates()
        {
            foreach (var rowHandle in Enumerable.Range(0, gridView1.RowCount))
            {
                var sensorValue = gridView1.GetRowCellValue(rowHandle, "Finish") as bool?;
                if (sensorValue.HasValue)
                {
                    if (sensorValue.Value)
                    {
                        // Sensor가 활성화된 경우 페이드 인
                        if (!_fadeProgress.ContainsKey(rowHandle) || _fadeProgress[rowHandle] < 1.0)
                            _fadeProgress[rowHandle] = Math.Min(1.0, (_fadeProgress.ContainsKey(rowHandle) ? _fadeProgress[rowHandle] : 0.0) + 0.1);
                    }
                    else
                    {
                        // Sensor가 비활성화된 경우 페이드 아웃
                        if (!_fadeProgress.ContainsKey(rowHandle) || _fadeProgress[rowHandle] > 0.0)
                            _fadeProgress[rowHandle] = Math.Max(0.0, (_fadeProgress.ContainsKey(rowHandle) ? _fadeProgress[rowHandle] : 1.0) - 0.1);
                    }
                }
                else
                {
                    _fadeProgress[rowHandle] = 0.0; // Sensor 값이 없는 경우 기본값
                }
            }
        }

        private void GridView1_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            if (e.Column.FieldName == "Finish" && _fadeProgress.TryGetValue(e.RowHandle, out var fadeValue))
            {
                // FadeValue에 따라 색상을 점진적으로 변경
                int greenComponent = (int)(fadeValue * 255);
                e.Appearance.BackColor = Color.FromArgb(greenComponent, Color.Green);
            }
        }

        private void FormatColumnsForFloat()
        {
            foreach (GridColumn column in gridView1.Columns)
            {
                // 열의 데이터 타입이 float인지 확인
                if (column.ColumnType == typeof(float))
                {
                    // 소수점 두 자리까지 표시하도록 설정
                    column.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                    column.DisplayFormat.FormatString = "F2";
                }
            }
            
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose(); // 타이머 해제
            }
            base.Dispose(disposing);
        }
    }
}
