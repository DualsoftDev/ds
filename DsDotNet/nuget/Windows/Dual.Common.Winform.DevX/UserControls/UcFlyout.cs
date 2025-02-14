using System;
using System.Windows.Forms;

using DevExpress.Utils;

namespace Dual.Common.Winform.DevX.UserControls
{
    public partial class UcFlyout : DevExpress.XtraEditors.XtraUserControl
    {
        public Control InnerControl { get; private set; }
        public FlyoutPanel FlyoutPanel => flyoutPanel1;
        public FlyoutPanelControl FlyoutPanelControl => flyoutPanelControl1;
        public UcFlyout()
        {
            InitializeComponent();
        }

        public UcFlyout(Control inner)
            : this()
        {
            SetInner(inner);
        }

        public void SetInner(Control inner, bool show=true)
        {
            InnerControl = inner;
            inner.Dock = DockStyle.Fill;

            panel1.Controls.Clear();
            inner.EmbedTo(panel1);
            if (inner is Form form)
                form.FormClosed += (s, e) => flyoutPanel1.HidePopup();

            this.Height = InnerControl.Height + 20;
            if (show)
                flyoutPanel1.ShowPopup();
        }

        private void UcFlyout_Load(object sender, EventArgs e)
        {
            flyoutPanel1.Dock = DockStyle.Fill;
        }
    }
}
