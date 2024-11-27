using DevExpress.Mvvm.Native;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraSpreadsheet.Model;
using DevExpress.XtraTreeMap;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace OPC.DSClient.WinForm.UserControl
{
    public partial class UcDsTreemap : XtraUserControl
    {
        public TreeMapHierarchicalDataAdapter DataAdapter { get; private set; }
        public Dictionary<string, DsUnit> DicPathMap { get; private set; } = new Dictionary<string, DsUnit>();

        private readonly Dictionary<string, System.Timers.Timer> BlinkTimers = new();
        private readonly System.Timers.Timer uiUpdateTimer;
        private bool isUpdatePending = false;
        private ToolTipController toolTipController;

        public UcDsTreemap()
        {
            InitializeComponent();
            InitializeTreemap();

            // UI 갱신 타이머 설정
            uiUpdateTimer = new System.Timers.Timer(500); // 500ms 간격으로 UI 업데이트
            uiUpdateTimer.Elapsed += (s, e) =>
            {
                if (Global.SelectedUserControl != this) return; // 현재 선택된 UserControl이 아닌 경우 갱신하지 않음

                if (isUpdatePending)
                {
                    Invoke((Action)UpdateTreemapUI);
                    isUpdatePending = false;
                }
            };
            uiUpdateTimer.Start();
        }

        private void UpdateTreemapUI()
        {
            TreemapManager.UpdateTreemapUI(treeMapControl1);
        }

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

        private void InitializeTreemap()
        {
            treeMapControl1.LayoutAlgorithm = new TreeMapSquarifiedLayoutAlgorithm();

            // 툴팁 설정
            toolTipController = new ToolTipController
            {
                ToolTipType = ToolTipType.SuperTip
            };
            toolTipController.BeforeShow += ToolTipController_BeforeShow;
            treeMapControl1.ToolTipController = toolTipController;
            treeMapControl1.Colorizer = new DsTreemapPaletteColorizer();

        }

        public void SetDataSource(OpcTagManager opcTagManager)
        {
            try
            {
                foreach (var tag in opcTagManager.OpcTags)
                {
                    tag.PropertyChanged -= OpcTag_PropertyChanged;
                    tag.PropertyChanged += OpcTag_PropertyChanged;
                }

                DicPathMap = TreemapManager.BindDataSource(opcTagManager, treeMapControl1);
                UpdateTreemapUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting Treemap data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpcTag_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OpcDsTag.Value) && sender is OpcDsTag opcTag && opcTag.Value is bool bOn)
            {
                // FQDN 노드 업데이트
                if (DicPathMap.TryGetValue(opcTag.ParentPath, out var DsUnitFqdn) && bOn)
                {
                    // ParentPath 기반의 색상 결정
                    var color = TreemapColor.GetCategoryColor(opcTag.TagKindDefinition);
                    if (color != Color.Transparent)
                    {
                        DsUnitFqdn.Color = color;
                        isUpdatePending = true;
                    }
                }
            }
        }


        private void ToolTipController_BeforeShow(object sender, ToolTipControllerShowEventArgs e)
        {
            if (e.SelectedObject is TreeMapItem treeMapItem)
            {
                var tag = treeMapItem.Tag as DsUnit;
                if (tag == null)
                    throw new InvalidOperationException("Invalid ToolTip tag object.");
                else
                    e.ToolTip = $"Name: {tag.Label}" +
                        $"\nCount: {tag.Count}" +
                        $"\nActiveTime: {tag.ActiveTime}" +
                        $"\nWaitingTime: {tag.WaitingTime}" +
                        $"\nMovingTime: {tag.MovingTime}" +
                        $"\nMovingAVG: {tag.MovingAVG}" +
                        $"\nMovingSTD: {tag.MovingSTD}";
            }
            else
            {
                e.ToolTip = "No additional information available.";
            }
        }
    }
}
