using DevExpress.XtraEditors;
using DevExpress.XtraVerticalGrid;
using DSModeler.Tree;
using Dual.Common.Core;
using Dual.Common.Core.FS;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows.Forms;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Cpu.RunTime;
using static Model.Import.Office.ImportPPTModule;

namespace DSModeler
{
    public partial class FormMain : XtraForm
    {
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
            InitializationUIControl();

            if (!Global.IsDebug)
                DevExpress.XtraSplashScreen.SplashScreenManager.CloseForm();
        } 

        private void InitializationUIControl()
        {
            LogicLog.InitControl(gridLookUpEdit_Log, gridLookUpEdit1View_Log);
            gridLookUpEdit_Log.Properties.DataSource = LogicLog.ValueLogs;

            LogicTree.InitControl(gridLookUpEdit_Expr, gridLookUpEdit1View_Expr);
            ratingControl_Speed.EditValue = SIMProperty.GetSpeed();

            var regSpeed = DSRegistry.GetValue(K.LayoutMenuFooter);
            toggleSwitch_menuNonFooter.IsOn = Convert.ToBoolean(regSpeed) != false;
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
                LayoutForm.SaveLayout(dockManager);
                SIMControl.Disconnect();
                EventCPU.CPUUnsubscribe();
            }
        }


        private void ace_Play_Click(object s, EventArgs e) => SIMControl.Play(ace_Play);
        private void ace_Step_Click(object s, EventArgs e) => SIMControl.Step(ace_Play);
        private void ace_Stop_Click(object s, EventArgs e) => SIMControl.Stop(ace_Play);
        private void ace_Reset_Click(object s, EventArgs e) => SIMControl.Reset(ace_Play, ace_HMI);
        private void ace_pcWindow_Click(object s, EventArgs e) => DocControl.CreateDocDS(this, tabbedView1);
        private void ace_PLCXGI_Click(object s, EventArgs e) => DocControl.CreateDocPLCLS(this, tabbedView1);
        private void ratingControl_Speed_EditValueChanged(object s, EventArgs e) => SIMProperty.SetSpeed(Convert.ToInt32(ratingControl_Speed.EditValue));
           
        private void simpleButton_OpenPLC_Click(object s, EventArgs e) => PLC.OpenPLCFolder();
        private void ace_ExportExcel_Click(object s, EventArgs e) => XLS.ExportExcel();
        private void ace_ImportPPT_Click(object s, EventArgs e) => ImportPowerPointWapper(null);
        private void ace_pptReload_Click(object sender, EventArgs e) => ImportPowerPointWapper(Files.GetLast());
        private void simpleButton_layoutReset_Click(object s, EventArgs e) => LayoutForm.RestoreLayoutFromXml(dockManager);

       
    }
}