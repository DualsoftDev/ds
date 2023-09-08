using Dual.Common.Winform;
using System.Drawing;
using System.Windows.Forms;

namespace DSModeler.Form
{
    public partial class FormDocText : DevExpress.XtraEditors.XtraForm
    {
        public FormDocText()
        {
            InitializeComponent();
        }

        public RichTextBox TextEdit => richTextBox_ds;
        public void AppendTextColor(string text, Color color)
        {
            var box = richTextBox_ds;
            box.Do(() =>
            {
                box.SelectionStart = box.TextLength;
                box.SelectionLength = 0;

                box.SelectionColor = color;
                box.AppendText(text);
                box.SelectionColor = box.ForeColor;
            });
        }


    }
}