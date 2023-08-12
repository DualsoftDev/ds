using DevExpress.XtraEditors;
using Dual.Common.Core;
using Dual.Common.Core.FS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static Engine.Core.CoreModule;
using static Engine.Cpu.RunTime;

namespace DSModeler
{
    public partial class FormMain : DevExpress.XtraEditors.XtraForm
    {
        Dictionary<DsSystem, DsCPU> _DicCpu = new Dictionary<DsSystem, DsCPU>();

        public FormMain()
        {
            InitializeComponent();
            this.KeyPreview = true;
        }


        private void FormMain_Load(object sender, EventArgs e)
        {
            Text = $"Dualsoft v{Global.AppVersion}";
            LayoutForm.LoadLayout(dockManager);
            DocControl.CreateDocStart(this, tabbedView1);

            InitializationEventSetting();

            ucLog1.InitLoad();
            Log4NetLogger.AppendUI(ucLog1);
            Global.Logger.Info($"Starting Dualsoft v{Global.AppVersion}");
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
                    _DicCpu = PPT.ImportPowerPoint(files, this, tabbedView1, ace_Model, ace_System, ace_Device, ace_HMI);
            };
            this.KeyDown += (s, e) =>
            {
                if (e.KeyData == Keys.F4)
                    ImportPowerPointWapper();
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
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MBox.AskYesNo("종료하시겠습니까?", K.AppName) == DialogResult.No)
                e.Cancel = true;
            else
                LayoutForm.SaveLayout(dockManager);
        }


        private void ace_ImportPPT_Click(object sender, EventArgs e) => ImportPowerPointWapper();
        private void ace_ResetLayout_Click(object sender, EventArgs e) => LayoutForm.RestoreLayoutFromXml(dockManager);
        private void ace_Play_Click(object sender, EventArgs e) => SIM.Play(_DicCpu);
        private void ace_Step_Click(object sender, EventArgs e) => SIM.Step(_DicCpu);
        private void ace_Stop_Click(object sender, EventArgs e) => SIM.Stop(_DicCpu);
        private void ace_Reset_Click(object sender, EventArgs e) => SIM.Reset(_DicCpu);

  
    }

}