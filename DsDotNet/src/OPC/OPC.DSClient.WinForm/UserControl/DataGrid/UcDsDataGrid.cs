using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            gridView1.OptionsView.ShowAutoFilterRow = true; // Enable filter row
            gridView1.OptionsView.ShowGroupPanel = false; // Disable group panel
            gridView1.OptionsBehavior.Editable = false; // Read-only grid

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

        public void SetDataSource(OpcTagManager opcTagManager)
        {
            _opcTagManager = opcTagManager;

            try
            {
                // BindingList를 데이터 소스로 설정
                _bindingList = DataGridUtil.GetDataSource(_opcTagManager);
                gridControl1.DataSource = _bindingList;

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
                var sensorValue = gridView1.GetRowCellValue(rowHandle, "Sensor") as bool?;
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
            if (e.Column.FieldName == "Sensor" && _fadeProgress.TryGetValue(e.RowHandle, out var fadeValue))
            {
                // FadeValue에 따라 색상을 점진적으로 변경
                int greenComponent = (int)(fadeValue * 255);
                e.Appearance.BackColor = Color.FromArgb(greenComponent, Color.Green);
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
