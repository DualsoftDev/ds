using System;
using System.Windows.Forms;

namespace Dual.Common.Winform.DevX2414.Forms
{
    public partial class FormOKCacnelSample : DevExpress.XtraEditors.XtraForm
    {
        public FormOKCacnelSample()
        {
            InitializeComponent();
        }

        private void FormOKCacnelSample_Load(object sender, EventArgs e)
        {
            // btn 의 속성에서 DialogResult 를 설정하면, 해당 버튼을 클릭했을 때 자동으로 폼이 닫히고 DialogResult 가 설정됨.
            btnOK.Click += (s, e) => Close();       // DialogResult = System.Windows.Forms.DialogResult.OK;
            btnCancel.Click += (s, e) => Close();   // DialogResult = System.Windows.Forms.DialogResult.Cacnel;

            // btn 의 Anchor 속성
            btnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
    }
}