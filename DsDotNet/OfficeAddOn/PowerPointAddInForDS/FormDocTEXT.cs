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

        public RichTextBox TextEdit { get; private set; }
        public void AppendTextColor(string text, Color color)
        {
            RichTextBox box = TextEdit;

            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }
    }
}