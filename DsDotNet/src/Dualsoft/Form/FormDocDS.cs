using DevExpress.XtraEditors;

namespace DSModeler.Form
{
    public partial class FormDocDS : DevExpress.XtraEditors.XtraForm
    {
        public FormDocDS()
        {
            InitializeComponent();
        }

        public MemoEdit MemoEditDS => memoEdit_DS;
    }
}