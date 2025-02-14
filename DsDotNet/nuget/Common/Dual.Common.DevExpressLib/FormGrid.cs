using DevExpress.XtraGrid;

using System.Windows.Forms;

namespace Dual.Common.Winform
{
    public partial class FormGrid : Form
    {
        public GridControl GridControl => gridControl1;
        public FormGrid()
        {
            InitializeComponent();
            btnOK.Click += (s, e) => Close();
        }
    }
}
