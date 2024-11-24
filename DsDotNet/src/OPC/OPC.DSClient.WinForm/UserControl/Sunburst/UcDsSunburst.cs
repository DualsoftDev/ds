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
        private readonly Dictionary<string, System.Timers.Timer> BlinkTimers = new();
        private readonly System.Timers.Timer uiUpdateTimer;
        private bool isUpdatePending = false;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                uiUpdateTimer?.Dispose();
                foreach (var timer in BlinkTimers.Values)
                {
                    timer.Dispose();
                }
                BlinkTimers.Clear();
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


            List<SunInfo> dataSource = new List<SunInfo>();
            DicPathMap = SunburstManager.GetPathMap(opcTagManager, dataSource);
            if (dataSource.Count > 0)
            {
                DataAdapter.DataSource = dataSource;
                sunburstControl1.CenterLabel.TextPattern = "Diagnostics Diagram";
                sunburstControl1.Refresh();
            }
        }

        // 깜빡임 시작
        internal void StartBlinking(string tagName, SunInfo sunInfo)
        {
            if (!BlinkTimers.ContainsKey(tagName))
            {
                var timer = new System.Timers.Timer(500); // 500ms 간격으로 깜빡임
                timer.Elapsed += (s, e) =>
                {
                    sunInfo.Color = sunInfo.Color == Color.IndianRed ? Color.LightGray : Color.IndianRed;
                    isUpdatePending = true; // UI 갱신 플래그 설정
                };
                BlinkTimers[tagName] = timer;
                timer.Start();
            }
        }

        // 깜빡임 중지
        internal void StopBlinking(string tagName)
        {
            if (BlinkTimers.TryGetValue(tagName, out var timer))
            {
                timer.Stop();
                timer.Dispose();
                BlinkTimers.Remove(tagName);
            }
        }

        private void OpcTag_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OpcTag.Value) && sender is OpcTag opcTag && opcTag.Value is bool bOn)
            {
                // SubTag 업데이트
                if (DicPathMap.TryGetValue(opcTag.Name, out var sunInfoSubTag))
                {
                    // "err" 상태의 깜빡임 처리 포함
                    var color = SunburstColor.GetSubItemColor(opcTag, bOn);
                    sunInfoSubTag.Color = color;

                    // 깜빡임 로직 추가
                    if (opcTag.TagKindDefinition.ToLower().Contains("err"))
                    {
                        if (bOn)
                        {
                            StartBlinking(opcTag.Name, sunInfoSubTag); // 에러 발생 시 깜빡임 시작
                        }
                        else
                        {
                            StopBlinking(opcTag.Name); // 에러 해제 시 깜빡임 중지
                            sunInfoSubTag.Color = Color.LightGray; // 기본 색상으로 복원
                            isUpdatePending = true;
                        }
                    }
                    else
                    {
                        sunInfoSubTag.Color = bOn ? Color.SkyBlue : Color.LightGray;
                        isUpdatePending = true;
                    }
                }

                // FQDN 노드 업데이트
                if (DicPathMap.TryGetValue(opcTag.ParentPath, out var sunInfoFqdn))
                {
                    // ParentPath 기반의 색상 결정
                    var color = SunburstColor.GetFolderColor(opcTag, bOn);
                    if (color != Color.Transparent)
                    {
                        sunInfoFqdn.Color = color;
                        isUpdatePending = true;
                    }
                }
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

                sunburstControl1.DataAdapter = null;
                sunburstControl1.DataAdapter = clonedAdapter;
                sunburstControl1.Refresh();
            }
        }
    }
}
