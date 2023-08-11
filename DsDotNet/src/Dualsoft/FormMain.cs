using DevExpress.XtraEditors;
using Dual.Common.Core;
using Dual.Common.Core.FS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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
            ProcessEvent.ProcessSubject.Subscribe(rx =>
            {
                UpdateProcessUI(rx.pro);
            });
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


     

        private void accordionControlElement_ImportPPT_Click(object sender, EventArgs e)
        {
            if (0 < ProcessEvent.CurrProcess && ProcessEvent.CurrProcess < 100)
                XtraMessageBox.Show("파일 처리중 입니다.", $"{K.AppName}", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                var files = FileOpenSave.OpenFiles();
                if (files != null)
                {
                    ImportPowerPoint(files);
                    accordionControlElement_Model.Expanded = true;
                    accordionControlElement_System.Expanded = true;
                    accordionControlElement_Device.Expanded = false;
                }
            }
        }

        private void accordionControlElement_ResetLayout_Click(object sender, EventArgs e)
        {
            dockManager.RestoreLayoutFromXml($"{Global.DefaultAppSettingFolder}\\default_layout.xml");
            dockManager.ForceInitialize();
        }
    }

}