using DevExpress.Xpo.DB.Helpers;
using DevExpress.XtraBars.Docking2010.Views.Tabbed;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraVerticalGrid;
using DSModeler;
using DSModeler.Tree;
using Dual.Common.Core;
using Engine.Core;
using Microsoft.Msagl.Routing.Spline.Bundling;
using Server.HW.WMX3;
using System;
using System.Linq;
using System.Windows.Forms;
using static Engine.Core.RuntimeGeneratorModule;
using static Engine.Core.TagKindModule;
using static Engine.Cpu.RunTimeUtil;

namespace DSModeler
{
    public partial class FormMain : XtraForm
    {
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
        private void InitGridLookUpEdit(GridLookUpEdit gle, GridView gv)
        {
            gle.Properties.DisplayMember = "Display";

            gv.PreviewLineCount = 20;
            gv.OptionsSelection.EnableAppearanceFocusedCell = false;
            gv.OptionsView.ShowAutoFilterRow = true;
            gv.OptionsView.ShowGroupPanel = false;
        }

        private void InitializationUIControl()
        {
            InitGridLookUpEdit(gle_Log, gleView_Log);
            InitGridLookUpEdit(gle_Expr, gleView_Expr);
            InitGridLookUpEdit(gle_Device, gleView_Device);

            gle_Log.Properties.DataSource = LogicLog.ValueLogs;


            ratingControl_Speed.EditValue = ControlProperty.GetSpeed();

            comboBoxEdit_RunMode.Properties.Items.AddRange(RuntimePackageList.ToArray());
            var cpuRunMode = DSRegistry.GetValue(K.CpuRunMode);
            comboBoxEdit_RunMode.EditValue = cpuRunMode == null ? RuntimePackage.Simulation : cpuRunMode;

            var RunCountIn = DSRegistry.GetValue(K.RunCountIn);
            spinEdit_StartIn.Properties.MinValue = 1;
            spinEdit_StartIn.EditValue = RunCountIn == null ? 1 : Convert.ToInt32(RunCountIn);
            var RunCountOut = DSRegistry.GetValue(K.RunCountOut);
            spinEdit_StartOut.Properties.MinValue = 1;
            spinEdit_StartOut.EditValue = RunCountOut == null ? 1 : Convert.ToInt32(RunCountOut);

            var ip = DSRegistry.GetValue(K.RunHWIP);
            textEdit_IP.Text = ip == null ? K.RunDefaultIP : ip.ToString();

            var menuExpand = DSRegistry.GetValue(K.LayoutMenuExpand);
            toggleSwitch_menuExpand.IsOn = Convert.ToBoolean(menuExpand);

            var layoutGraphLineType = DSRegistry.GetValue(K.LayoutGraphLineType);
            toggleSwitch_LayoutGraph.IsOn = Convert.ToBoolean(layoutGraphLineType);

            timerLongPress.Tick += (sender, e) =>
            {
                PcControl.Step(ace_Play);
            };
            btn_StepLongPress.MouseDown += (sender, e) => timerLongPress.Start();
            btn_StepLongPress.MouseUp += (sender, e) => timerLongPress.Stop();
            btn_StepLongPress.Disposed += (sender, e) =>
            {
                timerLongPress.Stop();
                timerLongPress.Dispose();
            };

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
                PcControl.Disconnect();
                EventCPU.CPUUnsubscribe();
            }
        }


        private void ace_Play_Click(object s, EventArgs e) => PcControl.Play(ace_Play);
        private void ace_Step_Click(object s, EventArgs e) => PcControl.Step(ace_Play);
        private void ace_Stop_Click(object s, EventArgs e) => PcControl.Stop(ace_Play);
        private void ace_Reset_Click(object s, EventArgs e) => PcControl.Reset(ace_Play, ace_HMI);
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
    }
}