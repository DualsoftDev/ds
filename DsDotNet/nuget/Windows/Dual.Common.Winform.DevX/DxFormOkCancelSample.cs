using System;
using System.Windows.Forms;

namespace Dual.Common.Winform.DevX
{
    /// <summary>
    /// Control 을 Panel 에 포함하는 일반적인 form
    /// </summary>
    public partial class DxFormOkCancelSample : DevExpress.XtraEditors.XtraForm
    {
        public event EventHandler OkClicked;
        public DxFormOkCancelSample()
        {
            InitializeComponent();
            btnOK.Click += (s, e) => {
                OkClicked?.Invoke(this, EventArgs.Empty); // OkClicked 이벤트 호출
                Close(); DialogResult = DialogResult.OK;
            };
            btnCancel.Click += (s, e) => { Close(); DialogResult = DialogResult.Cancel; };
        }
    }
}