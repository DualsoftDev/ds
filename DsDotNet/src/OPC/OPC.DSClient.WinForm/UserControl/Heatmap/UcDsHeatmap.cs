using DevExpress.XtraCharts.Heatmap;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using DevExpress.Charts.Heatmap;
using Timer = System.Windows.Forms.Timer;

namespace OPC.DSClient.WinForm.UserControl
{
    public partial class UcDsHeatmap : XtraUserControl
    {
        private List<OpcDsTag> _opcTags;
        private Timer _updateTimer;
        private readonly ToolTipController _toolTipController;

        public UcDsHeatmap()
        {
            InitializeComponent();

            // ToolTipController 초기화
            _toolTipController = new ToolTipController
            {
                ToolTipType = ToolTipType.SuperTip // SuperToolTip 사용
            };
            heatmapControl1.ToolTipEnabled = true;
            heatmapControl1.ToolTipController = _toolTipController;
            _toolTipController.BeforeShow += ToolTipController_BeforeShow;

            // Heatmap 초기화
            HeatmapManager.InitializeHeatmapColorProvider(heatmapControl1);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _updateTimer?.Dispose();
                _toolTipController?.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// OPC 태그 데이터를 Heatmap에 설정합니다.
        /// </summary>
        /// <param name="opcTagManager">OPC 태그 매니저</param>
        public void SetDataSource(OpcTagManager opcTagManager)
        {
            if (opcTagManager == null || opcTagManager.OpcTags.Count == 0)
            {
                MessageBox.Show("No data available to display.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _opcTags = opcTagManager.OpcFolderTags.Where(w => w.Path.Split('/').Length == 4).ToList();

            // Heatmap UI 갱신 타이머
            _updateTimer = new Timer
            {
                Interval = 500 // 500ms
            };
            _updateTimer.Tick += (s, e) => {
                if (Global.SelectedUserControl != this) return; // 현재 선택된 UserControl이 아닌 경우 갱신하지 않음
                HeatmapManager.UpdateHeatmap(heatmapControl1, _opcTags);
            }
            ;
            _updateTimer.Start();
        }

        /// <summary>
        /// ToolTipController에서 툴팁 표시 전에 호출됩니다.
        /// </summary>
        private void ToolTipController_BeforeShow(object sender, ToolTipControllerShowEventArgs e)
        {
            if (e.SelectedObject is HeatmapCell cell)
            {
                // SuperToolTip 생성
                var superToolTip = new SuperToolTip();

                try
                {
                    // X, Y 좌표 가져오기
                    var xIndex = Convert.ToInt32(cell.XArgument);
                    var yIndex = Convert.ToInt32(cell.YArgument);

                    // X, Y 좌표에 해당하는 OpcTag 검색
                    int tagIndex = yIndex * (int)Math.Ceiling(Math.Sqrt(_opcTags.Count)) + xIndex;
                    if (tagIndex >= 0 && tagIndex < _opcTags.Count)
                    {
                        var tag = _opcTags[tagIndex];

                        // SuperToolTip 내용 추가
                        superToolTip.Items.Add(new ToolTipItem { Text = $"Name: {tag.Name}" });
                        superToolTip.Items.Add(new ToolTipItem { Text = $"MovingSTD: {tag.MovingSTD/1000.0:F2} sec" });
                        superToolTip.Items.Add(new ToolTipItem { Text = $"Timestamp: {tag.Timestamp}" });
                    }
                    else
                    {
                        superToolTip.Items.Add(new ToolTipItem { Text = "No Data Available" });
                    }
                }
                catch (Exception ex)
                {
                    superToolTip.Items.Add(new ToolTipItem { Text = $"Error: {ex.Message}" });
                }

                // SuperToolTip 설정
                e.SuperTip = superToolTip;
            }
        }
    }
}
