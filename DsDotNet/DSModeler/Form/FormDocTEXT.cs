namespace DSModeler.Form
{
    public partial class FormDocText : DevExpress.XtraEditors.XtraForm
    {
        public FormDocText()
        {
            InitializeComponent();
        }

        public RichTextBox TextEdit { get; private set; }
        public void AppendTextColor(string text, Color color)
        {
            RichTextBox box = TextEdit;
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