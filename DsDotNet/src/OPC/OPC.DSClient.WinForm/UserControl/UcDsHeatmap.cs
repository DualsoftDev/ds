using DevExpress.XtraCharts.Heatmap;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.Charts.Heatmap;
using System.Data;

namespace OPC.DSClient.WinForm.UserControl
{

    public partial class UcDsHeatmap : XtraUserControl
    {
        private BindingList<OpcTag> _opcTags;
        private DevExpress.Utils.ToolTipController toolTipController1 = new DevExpress.Utils.ToolTipController();

        public UcDsHeatmap()
        {
            InitializeComponent();
            InitializeHeatmap();
        }

        private void InitializeHeatmap()
        {
            // 색상 팔레트 정의
            var palette = new DevExpress.XtraCharts.Palette("ChangeCountPalette")
            {
                Color.FromArgb(105, 168, 204),
                Color.FromArgb(125, 205, 168),
                Color.FromArgb(180, 224, 149),
                Color.FromArgb(253, 204, 138),
                Color.FromArgb(251, 167, 86),
                Color.FromArgb(225, 123, 49),
                Color.FromArgb(199, 73, 25),
                Color.FromArgb(180, 43, 1)
            };

            // 색상 공급자 설정
            var colorProvider = new HeatmapRangeColorProvider
            {
                Palette = palette,
                ApproximateColors = true // 색상 보간 활성화
            };

            for (int i = 0; i <= 6; i++)
                colorProvider.RangeStops.Add(new HeatmapRangeStop(i * 5, HeatmapRangeStopType.Absolute));
            colorProvider.RangeStops.Add(new HeatmapRangeStop(50, HeatmapRangeStopType.Absolute));

            heatmapControl1.ColorProvider = colorProvider;
            // 툴팁 컨트롤러 설정
            heatmapControl1.ToolTipController = this.toolTipController1;
            heatmapControl1.ToolTipEnabled = true;
            toolTipController1.BeforeShow += ToolTipController1_BeforeShow;
        }

        public void SetDataSource(BindingList<OpcTag> opcTags)
        {
            if (opcTags == null || opcTags.Count == 0)
            {
                MessageBox.Show("No data to display.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 이전 구독 해제 및 새로운 데이터 설정
                if (_opcTags != null)
                {
                    foreach (var tag in _opcTags)
                    {
                        tag.PropertyChanged -= OpcTag_PropertyChanged;
                    }
                }

                _opcTags = opcTags;

                foreach (var tag in _opcTags)
                {
                    tag.PropertyChanged += OpcTag_PropertyChanged;
                }

                // 초기 Heatmap 설정
                UpdateHeatmap();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting data source: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpcTag_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OpcTag.ChangeCount))
            {
                // 변경 횟수가 업데이트되면 Heatmap 갱신
                UpdateHeatmap();
            }
        }
        private void UpdateHeatmap()
        {
            if (_opcTags == null || _opcTags.Count == 0)
                return;


            int width = (int)Math.Ceiling(Math.Sqrt(_opcTags.Count));
            int height = width;

            double[] xArguments = new double[width];
            double[] yArguments = new double[height];
            double[,] values = new double[height, width];

            for (int i = 0; i < width; i++) xArguments[i] = i;
            for (int i = 0; i < height; i++) yArguments[i] = i;

            int index = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (index < _opcTags.Count)
                    {
                        var tag = _opcTags[index++];
                        values[y, x] = tag.ChangeCount;
                    }
                    else
                    {
                        values[y, x] = 0;
                    }
                }
            }

            var adapter = new HeatmapMatrixAdapter
            {
                XArguments = xArguments,
                YArguments = yArguments,
                Values = values
            };

            heatmapControl1.DataAdapter = adapter;
        }


        private void ToolTipController1_BeforeShow(object sender, ToolTipControllerShowEventArgs e)
        {
            if (e.SelectedObject is HeatmapCell cell)
            {
                // SuperToolTip 생성
                SuperToolTip superToolTip = new SuperToolTip();

                try
                {
                    // X, Y 좌표 가져오기
                    int xIndex = Convert.ToInt32(cell.XArgument);
                    int yIndex = Convert.ToInt32(cell.YArgument);

                    // DataTable에서 X, Y 좌표에 해당하는 데이터 검색
                    var matchingRows = _opcTags
                        .Where((tag, index) =>
                        {
                            int x = index % (int)Math.Ceiling(Math.Sqrt(_opcTags.Count));
                            int y = index / (int)Math.Ceiling(Math.Sqrt(_opcTags.Count));
                            return x == xIndex && y == yIndex;
                        })
                        .ToList();

                    if (matchingRows.Count > 0)
                    {
                        var tag = matchingRows[0];
                        superToolTip.Items.Add(new ToolTipItem { Text = $"Name: {tag.Name}" });
                        superToolTip.Items.Add(new ToolTipItem { Text = $"Change Count: {tag.ChangeCount}" });
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
