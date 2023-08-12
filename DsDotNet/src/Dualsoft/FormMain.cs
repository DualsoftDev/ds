using DevExpress.XtraEditors;
using DevExpress.XtraWaitForm;
using Dual.Common.Core;
using Dual.Common.Core.FS;
using log4net.Repository.Hierarchy;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DSModeler
{
    public partial class FormMain : DevExpress.XtraEditors.XtraForm
    {
        public FormMain()
        {
            InitializeComponent();

            this.KeyPreview = true;
            // Handling the QueryControl event that will populate all automatically generated Documents
            this.tabbedView1.QueryControl += tabbedView1_QueryControl;
        }


        private void FormMain_Load(object sender, EventArgs e)
        {
            Text = $"Dualsoft v{Global.AppVersion}";
            LayoutForm.LoadLayout(dockManager);
            InitializationEventSetting();



        }
        void InitializationEventSetting()
        {
            this.AllowDrop = true;
            this.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            };
            this.DragDrop += (s, e) =>
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                    ImportPowerPoint(files);
            };



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


            ucLog1.InitLoad();
            Log4NetLogger.AppendUI(ucLog1);
            Global.Logger.Info($"Starting Dualsoft v{Global.AppVersion}");

            CreateDocStart();
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MBox.AskYesNo("종료하시겠습니까?", K.AppName) == DialogResult.No)
                e.Cancel = true;
            else
                LayoutForm.SaveLayout(dockManager);
        }


        private void ace_ImportPPT_Click(object sender, EventArgs e)
        {
            if (0 < ProcessEvent.CurrProcess && ProcessEvent.CurrProcess < 100)
                XtraMessageBox.Show("파일 처리중 입니다.", $"{K.AppName}", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                var files = FileOpenSave.OpenFiles();
                if (files != null)
                {
                    ImportPowerPoint(files);
                    ace_Model.Expanded = true;
                    ace_System.Expanded = true;
                    ace_Device.Expanded = false;
                }
            }
        }

        private void ace_ResetLayout_Click(object sender, EventArgs e)
        {
            dockManager.RestoreLayoutFromXml($"{Global.DefaultAppSettingFolder}\\default_layout.xml");
            dockManager.ForceInitialize();
        }

        void tabbedView1_QueryControl(object sender, DevExpress.XtraBars.Docking2010.Views.QueryControlEventArgs e)
        {
            if (e.Control == null)
                e.Control = new System.Windows.Forms.Control();
        }

        private void ace_Play_Click(object sender, EventArgs e)
        {
            if(!_DicCpu.Keys.Any()) return;

            _DicCpu.ForEach(f =>
            {
                f.Value.Run();
                RunSimMode(f.Key);
            });
        }


        private void ace_Stop_Click(object sender, EventArgs e)
        {

        }

        private void ace_Reset_Click(object sender, EventArgs e)
        {

        }
    }

}