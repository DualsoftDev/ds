using DevExpress.XtraTreeMap;
using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.ClipboardSource.SpreadsheetML;
using DevExpress.XtraCharts.Heatmap;

namespace OPC.DSClient.WinForm.UserControl
{
    public partial class UcDsSunburst : XtraUserControl
    {
        public SunInfo SunInfo { get; set; }
        public SunburstHierarchicalDataAdapter DataAdapter { get; private set; }
        private DevExpress.Utils.ToolTipController toolTipController1 = new DevExpress.Utils.ToolTipController();

        public UcDsSunburst()
        {
            InitializeComponent();
            InitializeSunburst();
        }

        private void InitializeSunburst()
        {
            // Sunburst Data Adapter 초기화
            DataAdapter = new SunburstHierarchicalDataAdapter
            {
                Mappings =
                {
                    new SunburstHierarchicalDataMapping
                    {
                        ChildrenDataMember = "SunItems",
                        LabelDataMember = "Label",
                        ValueDataMember = "Value"
                    }
                }
            };

            // Sunburst Control 설정
            sunburstControl1.DataAdapter = DataAdapter;
            sunburstControl1.StartAngle = 0;
            sunburstControl1.HoleRadiusPercent = 20;
            sunburstControl1.ToolTipTextPattern = "{L}: {V}";
            sunburstControl1.HighlightMode = SunburstHighlightMode.PathFromRoot;
            sunburstControl1.Colorizer = new SunburstPaletteColorizer
            {
                VaryColorInGroup = true,
                Palette = Palette.PastelKitPalette
            };
            // 툴팁 설정
            sunburstControl1.ToolTipController = this.toolTipController1;
            sunburstControl1.ToolTipController.BeforeShow += ToolTipController_BeforeShow;
        }

        private void ToolTipController_BeforeShow(object sender, DevExpress.Utils.ToolTipControllerShowEventArgs e)
        {
            if (e.SelectedObject is SunburstItem item)
            {
                // Sunburst 항목에 대한 툴팁 구성
                var sunInfo = item.Tag as SunInfo;
                var superToolTip = new DevExpress.Utils.SuperToolTip
                {
                    Items =
                    {
                        new ToolTipTitleItem { Text = sunInfo?.Label ?? "Unknown" },
                        new ToolTipItem { Text = $"Value: {sunInfo?.Value ?? 0}" }
                    }
                };
                e.SuperTip = superToolTip;
            }
        }

        public void SetDataSource()
        {
        
        }
        internal void SetDataSource(OpcTagManager opcTagManager)
        {
            // SunInfo 데이터를 위한 리스트 생성
            List<SunInfo> data = new List<SunInfo>();

            // OpcFolderTags를 최상위 레벨로 추가
            foreach (var folder in opcTagManager.OpcFolderTags)
            {
                var folderInfo = new SunInfo
                {
                    Label = folder.Name,
                    Value = 0, // 기본값 (폴더는 값이 없음)
                    SunItems = new List<SunInfo>()
                };

                // OpcTags 중 해당 폴더에 속하는 태그를 추가
                var childTags = opcTagManager.OpcTags.Where(tag => tag.ParentPath == folder.Path);
                foreach (var tag in childTags)
                {
                    folderInfo.SunItems.Add(new SunInfo
                    {
                        Label = tag.Name,
                        Value = tag.ChangeCount, // 태그의 변경 횟수를 값으로 사용
                        SunItems = new List<SunInfo>() // Leaf 노드
                    });
                }

                data.Add(folderInfo);
            }

            // 루트 태그가 없는 독립 태그를 추가
            var independentTags = opcTagManager.OpcTags.Where(tag => string.IsNullOrEmpty(tag.ParentPath));
            foreach (var tag in independentTags)
            {
                data.Add(new SunInfo
                {
                    Label = tag.Name,
                    Value = tag.ChangeCount,
                    SunItems = new List<SunInfo>() // Leaf 노드
                });
            }

            // Sunburst 데이터 어댑터에 데이터 설정
            DataAdapter.DataSource = data;
            sunburstControl1.CenterLabel.TextPattern = "Sunburst Diagram";
            sunburstControl1.Refresh();
        }

    }

    public class SunInfo
    {
        public string Label { get; set; }
        public double Value { get; set; }
        public List<SunInfo> SunItems { get; set; } = new List<SunInfo>();
    }
}
