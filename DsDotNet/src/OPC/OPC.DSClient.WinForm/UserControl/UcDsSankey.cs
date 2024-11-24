using DevExpress.XtraEditors;
using DevExpress.Charts.Sankey;
using System.Data;

namespace OPC.DSClient.WinForm.UserControl
{
    public partial class UcDsSankey : XtraUserControl
    {
        public UcDsSankey()
        {
            InitializeComponent();
            InitializeSankeyDiagram();
        }

        /// <summary>
        /// Sankey Diagram 초기 설정.
        /// </summary>
        private void InitializeSankeyDiagram()
        {
            sankeyDiagramControl1.SourceDataMember = "Source";
            sankeyDiagramControl1.TargetDataMember = "Target";
            sankeyDiagramControl1.WeightDataMember = "Weight";
        }

        /// <summary>
        /// 샘플 데이터를 설정합니다.
        /// </summary>
        public void SetSampleDataSource()
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("Source", typeof(string));
            dataTable.Columns.Add("Target", typeof(string));
            dataTable.Columns.Add("Weight", typeof(double));

            // 샘플 데이터 추가
            dataTable.Rows.Add("Category A", "Category B", 50);
            dataTable.Rows.Add("Category A", "Category C", 30);
            dataTable.Rows.Add("Category B", "Category D", 20);
            dataTable.Rows.Add("Category C", "Category D", 10);
            dataTable.Rows.Add("Category D", "Category E", 40);

            SetDataSource(dataTable);
        }

        /// <summary>
        /// 데이터 소스를 Sankey Diagram에 설정합니다.
        /// </summary>
        /// <param name="dataTable">Sankey Diagram 데이터 소스</param>
        private void SetDataSource(DataTable dataTable)
        {
          

            sankeyDiagramControl1.DataSource = dataTable;
        }
    }
}
