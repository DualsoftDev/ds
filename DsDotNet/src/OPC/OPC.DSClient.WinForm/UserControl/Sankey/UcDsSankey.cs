using System.Windows.Forms;
using System.Linq;
using DevExpress.XtraCharts.Sankey;
using DevExpress.XtraEditors;
using Timer = System.Windows.Forms.Timer;
using DevExpress.XtraCharts;

namespace OPC.DSClient.WinForm.UserControl
{
    public partial class UcDsSankey : XtraUserControl
    {
        private OpcTagManager _opcTagManager;
        private readonly Timer _updateTimer;

        public UcDsSankey()
        {
            InitializeComponent();
            InitializeSankeyDiagram();

            // Timer 설정: 500ms 간격
            _updateTimer = new Timer
            {
                Interval = 500
            };
            _updateTimer.Tick += UpdateSankeyData;
        }

        /// <summary>
        /// Sankey 다이어그램 초기화
        /// </summary>
        private void InitializeSankeyDiagram()
        {
            sankeyDiagramControl1.SourceDataMember = "Source";
            sankeyDiagramControl1.TargetDataMember = "Target";
            sankeyDiagramControl1.WeightDataMember = "Weight";

            // Configuring the colorizer and layout algorithm for the diagram.
            sankeyDiagramControl1.Colorizer = new UcColorizer();
            sankeyDiagramControl1.LayoutAlgorithm = new MyLayoutAlgorithm();
            //sankeyDiagramControl1.LayoutAlgorithm = new SankeyLinearLayoutAlgorithm();
            sankeyDiagramControl1.MinimumSize = new Size(400, 300);

            // Setting node label orientation and disabling highlighting.
            sankeyDiagramControl1.NodeLabel.TextOrientation = TextOrientation.Horizontal;
            sankeyDiagramControl1.EnableHighlighting = false;

            sankeyDiagramControl1.CustomizeNode += OnCustomizeNode;
            sankeyDiagramControl1.CustomizeNodeToolTip += OnCustomizeNodeToolTip;
            sankeyDiagramControl1.CustomizeLinkToolTip += OnCustomizeLinkToolTip;
            sankeyDiagramControl1.MouseDoubleClick += OnSankeyNodeDoubleClick;
        }

        public void SetDataSource(OpcTagManager opcTagManager)
        {
            _opcTagManager = opcTagManager;
            if (string.IsNullOrWhiteSpace(opcTagManager?.OpcJsonText))
            {
                throw new Exception("OPC Json data is empty.");
            }

            try
            {
                var sankeyData = SankeyDataUtil.CreateSankeyData(opcTagManager, new List<OpcDsTag>());
                BindDataToSankey(sankeyData);

                // 타이머 시작
                _updateTimer.Start();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set Sankey data: {ex.Message}");
            }
        }
        private void OnSankeyNodeDoubleClick(object sender, MouseEventArgs e)
        {
            // 클릭한 위치의 노드 가져오기
            var node = LinkNodePoint.GetNode(e.Location);
            List<SankeyDsLink> sankeyData = new List<SankeyDsLink>();

            // 노드가 OpcDsTag를 포함하는 경우 처리
            if (node?.Tag is OpcDsTag opcDsTag)
            {
                // 하위 태그 가져오기
                var subTags = _opcTagManager.OpcFolderTags
                                            .Where(f => f.ParentPath == opcDsTag.Path)
                                            .ToList();

                // 하위 태그가 있는 경우 해당 태그로 Sankey 데이터 생성
                sankeyData = SankeyDataUtil.CreateSankeyData(_opcTagManager, subTags);
            }

            if (!sankeyData.Any())
                sankeyData = SankeyDataUtil.CreateSankeyData(_opcTagManager, []);

            // Sankey 다이어그램에 데이터 바인딩
            BindDataToSankey(sankeyData);
        }

        private void UpdateSankeyData(object sender, EventArgs e)
        {
            if (Global.SelectedUserControl != this) return; // 현재 선택된 UserControl이 아닌 경우 갱신하지 않음
            if (_opcTagManager == null) return;

            try
            {
                var data = sankeyDiagramControl1.DataSource;
                sankeyDiagramControl1.DataSource = null;
                sankeyDiagramControl1.DataSource = data;
                sankeyDiagramControl1.Refresh();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating Sankey data: {ex.Message}");
            }
        }

        private void BindDataToSankey(List<SankeyDsLink> sankeyData)
        {
            sankeyDiagramControl1.DataSource = sankeyData;
            sankeyDiagramControl1.Refresh();
        }

        private void OnCustomizeNode(object sender, CustomizeSankeyNodeEventArgs e)
        {
            if (e.Node.Tag is OpcDsTag tag)
                e.Label.Text = $"{tag.Name}";
        }

        private void OnCustomizeNodeToolTip(object sender, CustomizeSankeyNodeToolTipEventArgs e)
        {
            if (e.Node.Tag is OpcDsTag opc)
            {
                e.Title = $"Name: {opc.Name}";
                e.Content = $"QualifiedName: {opc.QualifiedName}\n" +
                            $"Count: {opc.Count}\n" +
                            $"Average: {opc.MovingAVG}\n" +
                            $"Variance: {opc.MovingSTD}";
            }
        }

        private void OnCustomizeLinkToolTip(object sender, CustomizeSankeyLinkToolTipEventArgs e)
        {
            e.Title = $"{e.Link.SourceNode.Tag} → {e.Link.TargetNode.Tag}";
            e.Content = $"Weight: {e.Link.TotalWeight}";
        }
    }
}
