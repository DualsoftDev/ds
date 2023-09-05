using DevExpress.XtraBars;
using DevExpress.XtraBars.Docking2010.Views.Tabbed;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraEditors;
using DevExpress.XtraVerticalGrid;
using Dual.Common.Core;
using Engine.Core;
using System;
using System.Windows.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;

namespace DSModeler
{
    public partial class FormMain : XtraForm
    {
        public ModelLoaderModule.Model Model;

        public PropertyGridControl PropertyGrid => ucPropertyGrid1.PropertyGrid;

        public TabbedView TabbedView => tabbedView_Doc;
        public AccordionControlElement Ace_Model => ace_Model;
        public AccordionControlElement Ace_Play=> ace_Play;
        public AccordionControlElement Ace_System => ace_System;
        public AccordionControlElement Ace_Device => ace_Device;
        public AccordionControlElement Ace_HMI => ace_HMI;
        public BarStaticItem LogCountText => barStaticItem_logCnt;

        public HubConnection connection;


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
            _ = Task.Run(async () => await InitializationClientSignalRAsync());

            if (!Global.IsDebug)
                DevExpress.XtraSplashScreen.SplashScreenManager.CloseForm();
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
                PcAction.Disconnect();
                EventCPU.CPUUnsubscribe();
            }
        }

        private async Task InitializationClientSignalRAsync()
        {
            connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:53455/samplehub")
                .Build()
                ;
            await connection.StartAsync();
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
        private void ace_ExportWebHMI_Click(object sender, EventArgs e) => HMI.Export(this);
        private void simpleButton_ClearLog_Click(object sender, EventArgs e) => LogicLog.ValueLogs.Clear();
        private void simpleButton_AllExpr_Click(object sender, EventArgs e) => DSFile.UpdateExprAll(this, toggleSwitch_showDeviceExpr.IsOn);
        private void simpleButton_ExportDStoFile_Click(object sender, EventArgs e) => DSFile.OpenDSFolder();
    }
}