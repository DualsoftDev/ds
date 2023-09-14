using Diagram.View.MSAGL;
using System.Windows.Forms;


namespace PowerPointAddInForDS
{
    public partial class FormDocView : Form
    {
        public FormDocView()
        {
            InitializeComponent();

        }

        public UcView UcView { get; private set; }

    }
}