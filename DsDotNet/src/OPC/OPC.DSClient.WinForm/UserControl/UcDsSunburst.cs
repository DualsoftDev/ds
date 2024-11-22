using DevExpress.Map.Kml.Model;
using DevExpress.XtraEditors;
using DevExpress.XtraTreeMap;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace OPC.DSClient.WinForm.UserControl
{
    public partial class UcDsSunburst : XtraUserControl
    {
        public SunburstHierarchicalDataAdapter DataAdapter { get; private set; }

        public UcDsSunburst()
        {
            InitializeComponent();
            InitializeSunburst();
            SetDemoDataSource();
        }

        private void InitializeSunburst()
        {
            // Sunburst Data Adapter 초기화
            DataAdapter = new SunburstHierarchicalDataAdapter();

            // Sunburst Data Mapping 설정
            var sunburstMapping = new SunburstHierarchicalDataMapping
            {
                ChildrenDataMember = "SunItems", // 하위 데이터
                LabelDataMember = "Label", // 항목 레이블
                ValueDataMember = "Value" // 항목 값
            };
            DataAdapter.Mappings.Add(sunburstMapping);
            DataAdapter.Mappings[0].Type = typeof(SunInfo); // 데이터 타입 지정

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
        }

        internal void SetDemoDataSource()
        {
            try
            {
                // 데모 데이터 생성
                var data = new List<SunInfo>
                {
                    new SunInfo
                    {
                        Label = "Root 1",
                        Value = 0,
                        SunItems = new List<SunInfo>
                        {
                            new SunInfo { Label = "Child 1.1", Value = 10 },
                            new SunInfo { Label = "Child 1.2", Value = 20 },
                            new SunInfo
                            {
                                Label = "Child 1.3",
                                Value = 0,
                                SunItems = new List<SunInfo>
                                {
                                    new SunInfo { Label = "Subchild 1.3.1", Value = 5 },
                                    new SunInfo { Label = "Subchild 1.3.2", Value = 15 }
                                }
                            }
                        }
                    },
                    new SunInfo
                    {
                        Label = "Root 2",
                        Value = 0,
                        SunItems = new List<SunInfo>
                        {
                            new SunInfo { Label = "Child 2.1", Value = 25 },
                            new SunInfo { Label = "Child 2.2", Value = 35 }
                        }
                    }
                };

                DataAdapter.DataSource = data;
                sunburstControl1.CenterLabel.TextPattern = "Demo Sunburst Diagram";
                sunburstControl1.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting demo data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        internal void SetDataSource(OpcTagManager opcTagManager)
        {
            System.ComponentModel.BindingList<OpcTag> treeData = new System.ComponentModel.BindingList<OpcTag>();

            opcTagManager.OpcFolderTags.ForEach(f => treeData.Add(f)); // OpcFolderTags를 OpcTags에 추가 

            foreach (var item in opcTagManager.OpcTags)
            {
                treeData.Add(item);
            }


            try
            {
                // 루트 노드 생성
                var root = new SunInfo
                {
                    Label = "Root",
                    Value = 0,
                    SunItems = new List<SunInfo>()
                };

                // Path와 ParentPath를 기반으로 SunInfo 계층 구조 생성
                var pathMap = new Dictionary<string, SunInfo>
                        {
                            { "", root } // 루트를 기본으로 추가
                        };

                foreach (var opcTag in treeData)
                {
                    // 현재 태그 또는 폴더의 SunInfo 생성
                    var sunInfo = new SunInfo
                    {
                        Label = opcTag.Name,
                        Value = opcTag.IsFolder ? 0 : opcTag.ChangeCount, // 폴더는 값이 0
                        SunItems = new List<SunInfo>()
                    };

                    // Path를 키로 SunInfo 저장
                    pathMap[opcTag.Path] = sunInfo;

                    // ParentPath를 통해 부모를 찾고, 해당 부모의 SunItems에 추가
                    if (pathMap.TryGetValue(opcTag.ParentPath, out var parentSunInfo))
                    {
                        parentSunInfo.SunItems.Add(sunInfo);
                    }
                    else
                    {
                        // 부모가 없으면 루트에 추가
                        root.SunItems.Add(sunInfo);
                    }
                }

                // Sunburst 데이터 어댑터에 루트 데이터 설정
                DataAdapter.DataSource = new List<SunInfo> { root };
                sunburstControl1.CenterLabel.TextPattern = "Sunburst Diagram";
                sunburstControl1.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting data source: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }

    public class SunInfo
    {
        public string Label { get; set; }
        public double Value { get; set; }
        public List<SunInfo> SunItems { get; set; } = new List<SunInfo>();
    }
}
