using DevExpress.XtraEditors;

namespace DSModeler.Form
{
    public partial class FormDocImage : DevExpress.XtraEditors.XtraForm
    {
        public FormDocImage()
        {
            InitializeComponent();
        }
        public PictureEdit ImageControl => pictureEdit;

    }
}