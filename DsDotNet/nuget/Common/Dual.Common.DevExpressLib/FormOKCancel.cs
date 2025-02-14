using DevExpress.XtraEditors;

using System.Windows.Forms;

namespace Dual.Common.Winform
{
    public partial class FormOKCancel : XtraForm
    {
        public PanelControl Panel => panelControl1;
        public FormOKCancel()
        {
            InitializeComponent();
        }
    }
}
