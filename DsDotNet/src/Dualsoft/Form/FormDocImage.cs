using DevExpress.XtraEditors;
using System.Windows.Forms;

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