using DevExpress.XtraBars.Docking2010.Customization;
using DevExpress.XtraVerticalGrid;
using Dual.Common.Core;
using Dual.Common.Core.FS;
using Dual.Common.Winform;
using Model.Import.Office;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static Dual.Common.Core.FS.MessageEvent;
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

        private async void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MBox.AskYesNo("종료하시겠습니까?", K.AppName) == DialogResult.No)
                e.Cancel = true;
            else
            {
                LayoutForm.SaveLayout(dockManager);
                await SIM.Reset(DicCpu);  //뒤에 실행안됨 주의   test ahn
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
        private async void ace_Play_Click(object sender, EventArgs e) => await SIM.Play(DicCpu);
        private async void ace_Step_Click(object sender, EventArgs e) => await SIM.Step(DicCpu);
        private async void ace_Stop_Click(object sender, EventArgs e) => await SIM.Stop(DicCpu);
        private async void ace_Reset_Click(object sender, EventArgs e) => await SIM.Reset(DicCpu);

        private void ace_pcWindow_Click(object sender, EventArgs e) => DocControl.CreateDocDS(this, tabbedView1);

        private void ace_PLCXGI_Click(object sender, EventArgs e) => DocControl.CreateDocPLCLS(this, tabbedView1);
        private void spinEdit_Speed_EditValueChanged(object sender, EventArgs e) => Global.SimSpeed = Convert.ToInt32(spinEdit_Speed.EditValue);
        private void simpleButton_OpenPLC_Click(object sender, EventArgs e)
        {
            if (IsLoadedPPT())
                Process.Start(Path.GetDirectoryName(LastFiles.Get().First()));
        }

        private void ace_ExportExcel_Click(object sender, EventArgs e)
        {
            if (!IsLoadedPPT()) return;
            var pathXLS = Path.ChangeExtension(LastFiles.Get().First(), "xlsx");

            ExportIOTable.ToFiie(new List<DsSystem>() { ActiveSys }, pathXLS);
            Global.Logger.Info($"{pathXLS} 생성완료!!");
            Process.Start($"{pathXLS}");
        }

    
    }
}