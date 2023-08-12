using DevExpress.XtraEditors;

namespace DSModeler.Form
{
    public partial class FormDocPLCLS : DevExpress.XtraEditors.XtraForm
    {
        public FormDocPLCLS()
        {
            InitializeComponent();
        }

        public MemoEdit MemoEditDS => memoEdit_DS;
    }
}