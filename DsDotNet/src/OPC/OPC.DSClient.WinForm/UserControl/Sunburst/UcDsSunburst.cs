using DevExpress.Charts.Model;
using DevExpress.Mvvm.Native;
using DevExpress.XtraEditors;
using DevExpress.XtraSpreadsheet.DocumentFormats.Xlsb;
using DevExpress.XtraTreeMap;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using static OPC.DSClient.WinForm.UserControl.SunburstRotationHelper;

namespace OPC.DSClient.WinForm.UserControl
{
    public partial class UcDsSunburst : XtraUserControl
    {
        public SunburstHierarchicalDataAdapter DataAdapter { get; private set; }
        public Dictionary<string, SunInfo> DicPathMap = new Dictionary<string, SunInfo>();
        private readonly System.Timers.Timer uiUpdateTimer;
        private bool isUpdatePending = false;
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                uiUpdateTimer?.Dispose();
            }
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        public UcDsSunburst()
        {
            InitializeComponent();
            InitializeSunburst();

            // UI 갱신 타이머 설정
            uiUpdateTimer = new System.Timers.Timer(500); // 500ms
            uiUpdateTimer.Elapsed += (s, e) =>
            {
                if (isUpdatePending)
                {
                    Invoke((Action)UpdateSunburstUI);
                    isUpdatePending = false;
                }
            };
            uiUpdateTimer.Start();
        }

        private void InitializeSunburst()
        {
            DataAdapter = new SunburstHierarchicalDataAdapter
            {
                Mappings =
                {
                    new SunburstHierarchicalDataMapping
                    {
                        ChildrenDataMember = "SunItems",
                        LabelDataMember = "Label",
                        ValueDataMember = "Value",
                    }
                }
            };
            DataAdapter.Mappings[0].Type = typeof(SunInfo);
            sunburstControl1.DataAdapter = DataAdapter;
            sunburstControl1.StartAngle = 0;
            sunburstControl1.HoleRadiusPercent = 15;
            sunburstControl1.ToolTipTextPattern = "{L}: {V}";
            sunburstControl1.HighlightMode = SunburstHighlightMode.PathFromRoot;
            sunburstControl1.SelectionMode = DevExpress.XtraTreeMap.ElementSelectionMode.None;


            sunburstControl1.Colorizer = new DsSunburstPaletteColorizer();
            _ = new SunburstRotationHelper(sunburstControl1);
        }



        internal void SetDataSource(OpcTagManager opcTagManager)
        {
            if (opcTagManager == null ||
                (opcTagManager.OpcTags.Count == 0 && opcTagManager.OpcFolderTags.Count == 0))
            {
                MessageBox.Show("No data available to display.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (var tag in opcTagManager.OpcTags)
            {
                tag.PropertyChanged -= OpcTag_PropertyChanged;
                tag.PropertyChanged += OpcTag_PropertyChanged;
            }

            try
            {
                var dsSystemKey = " tags";
                var dsSystemName = opcTagManager.OpcFolderTags
                                    .FirstOrDefault(f => f.Name.EndsWith(dsSystemKey))?
                                    .Name.TrimEnd(dsSystemKey.ToCharArray()) ?? "System";

                DicPathMap = opcTagManager.OpcFolderTags.ToDictionary(
                    folder => folder.Path,
                    folder => new SunInfo
                    {
                        Label = folder.Name.TrimStart('[').TrimEnd(']'),
                        Value = 1,
                        Color = Color.Gray,
                        SunItems = new List<SunInfo>()
                    });

                var flowList = new List<SunInfo>();

                foreach (var folder in opcTagManager.OpcFolderTags)
                {
                    if (folder.Name.EndsWith(dsSystemKey)) continue;

                    if (DicPathMap.TryGetValue(folder.Path, out var current))
                    {
                        if (DicPathMap.TryGetValue(folder.ParentPath, out var parent))
                            parent.SunItems.Add(current);
                        else
                            flowList.Add(current);
                    }
                }

                DicPathMap.Where(w => w.Value.SunItems.Count > 0)
                          .ForEach(f => f.Value.Value = f.Value.SunItems.Count);

                DataAdapter.DataSource = flowList;
                sunburstControl1.CenterLabel.TextPattern = dsSystemName;
                sunburstControl1.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting data source: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // UI 갱신 메서드
        private void UpdateSunburstUI()
        {
            if (sunburstControl1.DataAdapter is SunburstHierarchicalDataAdapter adapter)
            {
                var clonedAdapter = new SunburstHierarchicalDataAdapter
                {
                    Mappings =
            {
                new SunburstHierarchicalDataMapping
                {
                    ChildrenDataMember = adapter.Mappings[0].ChildrenDataMember,
                    LabelDataMember = adapter.Mappings[0].LabelDataMember,
                    ValueDataMember = adapter.Mappings[0].ValueDataMember,
                    Type = adapter.Mappings[0].Type
                }
            },
                    DataSource = adapter.DataSource
                };

                // Sunburst 컨트롤 업데이트
                sunburstControl1.DataAdapter = null;
                sunburstControl1.DataAdapter = clonedAdapter;

                // UI 갱신
                sunburstControl1.Refresh();
            }
        }
        private void OpcTag_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OpcTag.Value) && sender is OpcTag opcTag && opcTag.Value is bool bOn)
            {
                // DicPathMap에서 해당 태그의 ParentPath를 기반으로 SunInfo를 검색
                if (DicPathMap.TryGetValue(opcTag.ParentPath, out var sunInfo))
                {
                    // SunInfo의 Color 업데이트
                    var color = SunburstColor.GetColor(opcTag, bOn);
                    // UI 갱신 플래그 설정 (색상이 유효할 때만)
                    if (color != Color.Transparent)
                    {
                        sunInfo.Color = color;
                        isUpdatePending = true;
                    }
                }
            }
        }

    }

}
