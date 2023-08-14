using DevExpress.XtraVerticalGrid;
using Dual.Common.Core;
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


        private void FormMain_Shown(object sender, EventArgs e)
        {
            ratingControl_Speed.EditValue = SIMProperty.GetSpeed();
        }

        private async void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MBox.AskYesNo("종료하시겠습니까?", K.AppName) == DialogResult.No)
                e.Cancel = true;
            else
            {
                LayoutForm.SaveLayout(dockManager);
                await SIMControl.Reset(DicCpu);  //뒤에 실행안됨 주의   test ahn
            }
        }




        private async void ace_Play_Click(object sender, EventArgs e) => await SIMControl.Play(DicCpu);
        private async void ace_Step_Click(object sender, EventArgs e) => await SIMControl.Step(DicCpu);
        private async void ace_Stop_Click(object sender, EventArgs e) => await SIMControl.Stop(DicCpu);
        private async void ace_Reset_Click(object sender, EventArgs e) => await SIMControl.Reset(DicCpu);

        private void ace_pcWindow_Click(object sender, EventArgs e) 
            => DocControl.CreateDocDS(this, tabbedView1);
        private void ace_PLCXGI_Click(object sender, EventArgs e)
            => DocControl.CreateDocPLCLS(this, tabbedView1);
        private void ratingControl_Speed_EditValueChanged(object sender, EventArgs e)
            => SIMProperty.SetSpeed(Convert.ToInt32(ratingControl_Speed.EditValue));
        private void toggleSwitch_simLog_Toggled(object sender, EventArgs e)
            => Global.SimLogHide = toggleSwitch_simLog.IsOn;
        private void simpleButton_OpenPLC_Click(object sender, EventArgs e)
            => PLC.OpenPLCFolder();
        private void ace_ExportExcel_Click(object sender, EventArgs e) 
            => XLS.ExportExcel();
        private void ace_ImportPPT_Click(object sender, EventArgs e)
            => ImportPowerPointWapper(null);
        private void ace_pptReload_Click(object sender, EventArgs e)
            => ImportPowerPointWapper(Files.GetLast());
        private void simpleButton_layoutReset_Click(object sender, EventArgs e)
            => LayoutForm.RestoreLayoutFromXml(dockManager);
    }
}