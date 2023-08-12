using DevExpress.XtraVerticalGrid;
using Dual.Common.Core;
using Dual.Common.Core.FS;
using Dual.Common.Winform;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Cpu.RunTime;

namespace DSModeler
{
    public partial class FormMain : DevExpress.XtraEditors.XtraForm
    {
        public DsSystem ActiveSys = null;  
        public Dictionary<DsSystem, DsCPU> DicCpu = new Dictionary<DsSystem, DsCPU>();
        public Dictionary<Vertex, Status4> DicStatus = new Dictionary<Vertex, Status4>();
        public PropertyGridControl PropertyGrid => ucPropertyGrid1.PropertyGrid;

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
            InitializationLogger();

#if !DEBUG
            DevExpress.XtraSplashScreen.SplashScreenManager.CloseForm();
#endif
        }

        private void InitializationLogger()
        {
            ucLog1.InitLoad();
            Log4NetLogger.AppendUI(ucLog1);
            Global.Logger.Info($"Starting Dualsoft v{Global.AppVersion}");
        }

       
        private void FormMain_Shown(object sender, EventArgs e) { }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MBox.AskYesNo("종료하시겠습니까?", K.AppName) == DialogResult.No)
                e.Cancel = true;
            else
            {
                SIM.Reset(DicCpu);
                LayoutForm.SaveLayout(dockManager);
            }
        }

        public bool IsLoadedPPT()
        {
            if (ActiveSys == null)
            {
                Global.Logger.Warn("PPT 파일을 먼저 불러오세요");
                return false;
            }
            else
                return true;
        }

        private void ace_ImportPPT_Click(object sender, EventArgs e) => ImportPowerPointWapper(null);
        private void ace_pptReload_Click(object sender, EventArgs e) => ImportPowerPointWapper(LastFiles.Get());
        private void ace_ResetLayout_Click(object sender, EventArgs e) => LayoutForm.RestoreLayoutFromXml(dockManager);
        private void ace_Play_Click(object sender, EventArgs e) => SIM.Play(DicCpu);
        private void ace_Step_Click(object sender, EventArgs e) => SIM.Step(DicCpu);
        private void ace_Stop_Click(object sender, EventArgs e) => SIM.Stop(DicCpu);
        private void ace_Reset_Click(object sender, EventArgs e) => SIM.Reset(DicCpu);

        private void ace_pcWindow_Click(object sender, EventArgs e) => DocControl.CreateDocDS(this, tabbedView1);
        private void ace_PLCXGI_Click(object sender, EventArgs e) => DocControl.CreateDocPLCLS(this, tabbedView1);

       
    }
}