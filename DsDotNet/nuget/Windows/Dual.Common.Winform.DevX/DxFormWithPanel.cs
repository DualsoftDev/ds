using System;
using System.Windows.Forms;

namespace Dual.Common.Winform.DevX
{
    /// <summary>
    /// Control 을 Panel 에 포함하는 일반적인 form
    /// </summary>
    public partial class DxFormWithPanel : DevExpress.XtraEditors.XtraForm
    {
        public Panel Panel => panel1;
        /// <summary>
        /// Panel 속에 들어갈 control
        /// </summary>
        public Control Control { get; private set; }
        public event EventHandler OkClicked;
        public DxFormWithPanel(Control control, bool showOkCancel=false)
        {
            InitializeComponent();
            Control = control;
            Load += (s, e) =>
            {
                if (! showOkCancel)
                {
                    btnOK.Visible = btnCancel.Visible = false;
                    Panel.Dock = DockStyle.Fill;
                }

                Panel.Controls.Add(Control);
                Control.Dock = DockStyle.Fill;
            };
            btnCancel.Click += (s, e) => Close();
        }

        private void btnOK_Click(object sender, System.EventArgs e)
        {
            OkClicked?.Invoke(this, EventArgs.Empty); // OkClicked 이벤트 호출
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}