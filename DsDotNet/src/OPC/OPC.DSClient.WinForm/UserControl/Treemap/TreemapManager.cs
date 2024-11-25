using DevExpress.XtraTreeMap;
using System.Collections.Generic;
using System.Drawing;

namespace OPC.DSClient.WinForm.UserControl
{
    public static class TreemapManager
    {
        public static Dictionary<string, DsUnit> BindDataSource(OpcTagManager opcTagManager, TreeMapControl treeMapControl)
        {
            // 데이터 소스 생성
            List<DsUnit> dataSource = new List<DsUnit>();
            var dicPathMap = CommonUIManager.GetPathMap(opcTagManager, dataSource);
            CommonUIManager.FilterNoChildFolderUnits(dataSource);

            if (dataSource.Count > 0)
            {
                // TreeMapHierarchicalDataAdapter 설정
                var adapter = new TreeMapHierarchicalDataAdapter
                {
                    DataSource = dataSource,
                    Mappings =
                    {
                        new TreeMapHierarchicalDataMapping
                        {
                            ValueDataMember = "Mean",
                            LabelDataMember = "Label",
                            ChildrenDataMember = "DsUnits",
                            Type = typeof(DsUnit)
                        }
                    }
                };

                treeMapControl.DataAdapter = adapter;
            }

            return dicPathMap;
        }


        public static void UpdateTreemapUI(TreeMapControl treeMapControl)
        {
            if (treeMapControl.DataAdapter is TreeMapHierarchicalDataAdapter adapter)
            {
                var clonedAdapter = new TreeMapHierarchicalDataAdapter
                {
                    Mappings =
                    {
                        new TreeMapHierarchicalDataMapping
                        {
                            ChildrenDataMember = adapter.Mappings[0].ChildrenDataMember,
                            LabelDataMember = adapter.Mappings[0].LabelDataMember,
                            ValueDataMember = adapter.Mappings[0].ValueDataMember,
                            Type = adapter.Mappings[0].Type
                        }
                    },
                    DataSource = adapter.DataSource
                };

                treeMapControl.DataAdapter = null;
                treeMapControl.DataAdapter = clonedAdapter;
                treeMapControl.Refresh();
            }
        }
    }
}

