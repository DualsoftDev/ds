using DevExpress.Mvvm.Native;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraTreeMap;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace OPC.DSClient.WinForm.UserControl
{
    public partial class UcDsTreemap : XtraUserControl
    {
        public Dictionary<string, DsUnit> DicPathMap = new Dictionary<string, DsUnit>();
        private ToolTipController toolTipController;

        public UcDsTreemap()
        {
            InitializeComponent();
            InitializeTreemap();
        }

        /// <summary>
        /// Treemap 초기화
        /// </summary>
        private void InitializeTreemap()
        {
            // ToolTipController 설정
            toolTipController = new ToolTipController
            {
                ToolTipType = ToolTipType.SuperTip
            };
            toolTipController.BeforeShow += ToolTipController_BeforeShow;
            treeMapControl1.ToolTipController = toolTipController;
        }

        /// <summary>
        /// Treemap에 데이터를 설정합니다.
        /// </summary>
        /// <param name="opcTagManager">OPC 태그 매니저</param>
        public void SetDataSource(OpcTagManager opcTagManager)
        {
            try
            {
                // 데이터 소스를 생성합니다.
                List<DsUnit> dataSource = new List<DsUnit>();

                // DicPathMap을 생성하며 데이터 소스를 채웁니다.
                DicPathMap = CommonUIManager.GetPathMap(opcTagManager, dataSource);

                if (dataSource.Count > 0)
                {
                    // TreeMapHierarchicalDataAdapter를 설정합니다.
                    var adapter = new TreeMapHierarchicalDataAdapter
                    {
                        DataSource = dataSource,
                        Mappings =
                        {
                            new TreeMapHierarchicalDataMapping
                            {
                                ValueDataMember = "Value",         // 값 데이터 매핑
                                LabelDataMember = "Label",         // 이름 데이터 매핑
                                ChildrenDataMember = "DsUnits",    // 자식 데이터 매핑
                                Type = typeof(DsUnit)              // 데이터 타입 지정
                            }
                        }
                    };

                    // TreemapControl에 어댑터를 설정합니다.
                    treeMapControl1.DataAdapter = adapter;

                    // Treemap의 레이아웃과 색상 설정
                    treeMapControl1.LayoutAlgorithm = new TreeMapSquarifiedLayoutAlgorithm();
                    //treeMapControl1.Colorizer = new TreeMapGradientColorizer
                    //{
                    //    StartColor = Color.LightGreen,
                    //    EndColor = Color.DarkGreen,
                    //};

                    // Treemap 갱신
                    treeMapControl1.Refresh();
                }
                else
                {
                    MessageBox.Show("No data available for the Treemap.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting Treemap data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

  
        /// <summary>
        /// Treemap 툴팁 설정
        /// </summary>
        private void ToolTipController_BeforeShow(object sender, ToolTipControllerShowEventArgs e)
        {
            if (e.SelectedObject is TreeMapItem treeMapItem)
            {
                e.ToolTip = $"Name: {treeMapItem.Label}\nValue: {treeMapItem.Value}";
            }
            else
            {
                e.ToolTip = "No additional information available.";
            }
        }
    }
}
