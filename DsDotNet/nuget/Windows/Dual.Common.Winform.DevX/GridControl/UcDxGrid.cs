using DevExpress.XtraExport.Helpers;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

using System;
using System.Windows.Forms;

namespace Dual.Common.Winform.DevX
{
    public partial class UcDxGrid : DevExpress.XtraEditors.XtraUserControl
    {
        public bool IsDirty { get; private set; }
        public GridControl GridControl { get { return gridControl1; } }
        public GridView GridView { get { return gridView1; } }
        public object DataSource { get { return gridControl1.DataSource; } set { gridControl1.DataSource = value; } }

        public UcDxGrid()
        {
            InitializeComponent();
            gridView1.HideGroupPanel();
        }
        public UcDxGrid(object dataSource)
            : this()
        {
            gridControl1.DataSource = dataSource;
        }

        private void UcGridView_Load(object sender, EventArgs args)
        {
            gridControl1.Dock = DockStyle.Fill;
            GridView.CellValueChanged += (s, e) => IsDirty = true;
        }
    }
}
