using DevExpress.XtraEditors;
using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Columns;
using System;
using System.Windows.Forms;

namespace OPC.DSClient.WinForm.UserControl
{
    public partial class UcDsTree : XtraUserControl
    {
        public UcDsTree()
        {
            InitializeComponent();
            InitializeTreeList();
        }

        private void InitializeTreeList()
        {
            treeList1.OptionsBehavior.Editable = false; // 읽기 전용 설정
            treeList1.OptionsBehavior.ReadOnly = true; // 노드와 데이터를 변경할 수 없도록 설정
            treeList1.OptionsView.ShowIndicator = false; // 좌측 인덱스 열 숨김
            treeList1.OptionsView.AutoWidth = false; // 열 너비 자동 조정 해제
            treeList1.OptionsView.BestFitMode = DevExpress.XtraTreeList.TreeListBestFitMode.Full; // Best Fit 적용
            treeList1.OptionsSelection.EnableAppearanceFocusedCell = false; // 선택된 셀의 포커스 효과 비활성화
            treeList1.OptionsSelection.MultiSelect = true; // 다중 선택 가능
            treeList1.OptionsFilter.AllowFilterEditor = true; // 필터 편집기 활성화
            treeList1.OptionsView.ShowAutoFilterRow = true; // 필터 입력창 활성화
            treeList1.ParentFieldName = "ParentPath"; // 부모 경로 필드
            treeList1.KeyFieldName = "Path"; // 고유 경로 필드

            // TreeList Columns
            AddTreeListColumn("Tag Name", "Name", 0);
            AddTreeListColumn("Value", "Value", 1);
            AddTreeListColumn("Data Type", "DataType", 2);
            AddTreeListColumn("Timestamp", "Timestamp", 3);


            treeList1.BeforeCollapse += (s, e) => treeList1.BeginUpdate();
            treeList1.AfterCollapse += (s, e) => treeList1.EndUpdate();
            treeList1.BeforeExpand += (s, e) => treeList1.BeginUpdate();
            treeList1.AfterExpand += (s, e) => treeList1.EndUpdate();
        }

        private void AddTreeListColumn(string caption, string fieldName, int visibleIndex)
        {
            var column = new TreeListColumn
            {
                Caption = caption,
                FieldName = fieldName,
                Visible = true,
                VisibleIndex = visibleIndex
            };

            treeList1.Columns.Add(column);
        }

        public void SetDataSource(OpcTagManager opcTagManager)
        {
            try
            {
                System.ComponentModel.BindingList<OpcDsTag> treeData = new System.ComponentModel.BindingList<OpcDsTag>();

                opcTagManager.OpcFolderTags.ForEach(f => treeData.Add(f)); // OpcFolderTags를 OpcTags에 추가 

                foreach (var item in opcTagManager.OpcTags)
                {
                    treeData.Add(item);
                }
                
                treeList1.DataSource = treeData;
                treeList1.CollapseAll(); //         모든 노드를 닫음   
                foreach (TreeListColumn column in treeList1.Columns)
                {
                    column.BestFit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tree data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
