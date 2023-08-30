using DevExpress.XtraBars.Docking2010.Views.Tabbed;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using DevExpress.XtraVerticalGrid;
using DSModeler.Tree;
using Dual.Common.Core;
using Engine.Core;
using System;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Web.UI.WebControls;
using System.Windows.Forms;
using static Engine.Core.RuntimeGeneratorModule;
using GridView = DevExpress.XtraGrid.Views.Grid.GridView;

namespace DSModeler
{
    public partial class FormMain : XtraForm
    {
        public ModelLoaderModule.Model Model;

        public PropertyGridControl PropertyGrid => ucPropertyGrid1.PropertyGrid;

        public TabbedView TabbedView => tabbedView_Doc;
        public AccordionControlElement Ace_Model => ace_Model;
        public AccordionControlElement Ace_System => ace_System;
        public AccordionControlElement Ace_Device => ace_Device;
        public AccordionControlElement Ace_HMI => ace_HMI;

        public FormMain()
        {
            InitializeComponent();
            this.KeyPreview = true;
        }


        private void FormMain_Load(object sender, EventArgs e)
        {
            Text = $"Dualsoft v{Global.AppVersion}";
            LayoutForm.LoadLayout(dockManager);
            DocControl.CreateDocStart(this, tabbedView_Doc);


            InitializationEventSetting();
            InitializationLogger();
            InitializationUIControl();


            if (!Global.IsDebug)
                DevExpress.XtraSplashScreen.SplashScreenManager.CloseForm();
        }

        readonly Timer timerLongPress = new Timer { Interval = 100 };
       


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
                PcAction.Disconnect();
                EventCPU.CPUUnsubscribe();
            }
        }


        private void ace_Play_Click(object s, EventArgs e) => PcAction.Play(ace_Play);
        private void ace_Step_Click(object s, EventArgs e) => PcAction.Step(ace_Play);
        private void ace_Stop_Click(object s, EventArgs e) => PcAction.Stop(ace_Play);
        private void ace_Reset_Click(object s, EventArgs e) => PcAction.Reset(ace_Play, ace_HMI);
        private void ace_pcWindow_Click(object s, EventArgs e) => DocControl.CreateDocDS(this, tabbedView_Doc);
        private void ace_PLCXGI_Click(object s, EventArgs e) => DocControl.CreateDocPLCLS(this, tabbedView_Doc);
        private void ratingControl_Speed_EditValueChanged(object s, EventArgs e) => ControlProperty.SetSpeed(Convert.ToInt32(ratingControl_Speed.EditValue));
        private void simpleButton_OpenPLC_Click(object s, EventArgs e) => PLC.OpenPLCFolder();
        private void ace_ExportExcel_Click(object s, EventArgs e) => XLS.ExportExcel(gridControl_exprotExcel);
        private void ace_ImportPPT_Click(object s, EventArgs e) => ImportPowerPointWapper(null);
        private void ace_pptReload_Click(object sender, EventArgs e) => ImportPowerPointWapper(Files.GetLast());
        private void simpleButton_layoutReset_Click(object s, EventArgs e) => LayoutForm.RestoreLayoutFromXml(dockManager);
        private void ace_ImportXls_Click(object sender, EventArgs e) => Global.Notimplemented();
        private void ace_pcLinux_Click(object sender, EventArgs e) => Global.Notimplemented();
        private void ace_DocDiagram_Click(object sender, EventArgs e) => Global.Notimplemented();
        private void ace_PLCLogix5000_Click(object sender, EventArgs e) => Global.Notimplemented();
        private void ace_PLCWork3_Click(object sender, EventArgs e) => Global.Notimplemented();
        private void ace_ExportWebHMI_Click(object sender, EventArgs e) => HMI.Export();
        private void simpleButton_ClearLog_Click(object sender, EventArgs e) => LogicLog.ValueLogs.Clear();
        private void simpleButton_AllExpr_Click(object sender, EventArgs e) => DSFile.UpdateExprAll(this, toggleSwitch_showDeviceExpr.IsOn);
    }
}