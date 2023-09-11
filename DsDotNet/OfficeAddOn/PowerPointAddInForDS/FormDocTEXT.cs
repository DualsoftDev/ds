using System.Drawing;
using System.Windows.Forms;

namespace PowerPointAddInForDS
{
    public partial class FormDocText : Form
    {
        public FormDocText()
        {
            InitializeComponent();
        }

        public RichTextBox TextEdit => richTextBox_ds;
        public void AppendTextColor(string text, Color color)
        {
            var box = richTextBox_ds;

            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }
    }
}