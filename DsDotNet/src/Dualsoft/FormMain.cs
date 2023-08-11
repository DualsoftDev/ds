using DevExpress.XtraEditors;
using System;
using System.Windows.Forms;

namespace Dualsoft
{
    public partial class FormMain : DevExpress.XtraEditors.XtraForm
    {
        public FormMain()
        {
            InitializeComponent();

            this.KeyPreview = true;
        }


        private void FormMain_Load(object sender, EventArgs e)
        {
            Text = $"Dualsoft v{Global.AppVersion}";
            LayoutForm.LoadLayout(dockManager);
            InitializationEventSetting();

        }
        void InitializationEventSetting()
        {
            tabbedView1.QueryControl += (s, e) =>
            {
                if (e.Control == null)  //Devexpress MDI Control
                    e.Control = new System.Windows.Forms.Control();
            };
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {
#if !DEBUG
            DevExpress.XtraSplashScreen.SplashScreenManager.CloseForm();
#endif
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult dr = XtraMessageBox.Show("종료하시겠습니까?", "종료확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            if (dr == DialogResult.No)
            {
                e.Cancel = true;
            }
            else
            {
                LayoutForm.SaveLayout(dockManager);
            }
        }


        private void CreateNewDocument()
        {
            //string docKey = "Task1";
            //BaseDocument document = tabbedView1.Documents.Where(w => w.Control.Name == docKey).FirstOrDefault();
            //if (document != null) tabbedView1.Controller.Activate(document);
            //else
            //{
            //    //UCTaskUI_Form form = new UCTaskUI_Form();
            //    //form.Name = docKey;
            //    //form.MdiParent = this;
            //    //form.Text = docKey;
            //    //form.Show();
            //    document = tabbedView1.Documents.Where(w => w.Control.Name == docKey).FirstOrDefault();
            //    document.Caption = docKey;
            //}
        }

        private void accordionControlElement_ImportPPT_Click(object sender, EventArgs e)
        {

        }

        private void accordionControlElement_ImportXls_Click(object sender, EventArgs e)
        {

        }
    }

}