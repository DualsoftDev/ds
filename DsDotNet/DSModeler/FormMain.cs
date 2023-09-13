

namespace DSModeler
{
    [SupportedOSPlatform("windows")]
    public partial class FormMain : XtraForm
    {
        public ModelLoaderModule.Model Model;

        public PropertyGridControl PropertyGrid => ucPropertyGrid1.PropertyGrid;
        public TabbedView TabbedView => tabbedView_doc;
        public AccordionControlElement Ace_Model => ace_Model;
        public AccordionControlElement Ace_Play => ace_Play;
        public AccordionControlElement Ace_System => ace_System;
        public AccordionControlElement Ace_Device => ace_Device;
        public AccordionControlElement Ace_HMI => ace_HMI;
        public AccordionControlElement Ace_ExSystem => ace_ExSystem;
        public BarStaticItem LogCountText => barStaticItem_logCnt;

        public HubConnection connection;



        public FormMain()
        {
            InitializeComponent();
            KeyPreview = true;
        }


        private void FormMain_Load(object sender, EventArgs e)
        {
            Text = $"Dualsoft v{Global.AppVersion}";
            LayoutForm.LoadLayout(dockManager);

            InitializationEventSetting();
            InitializationLogger();
            InitializationUIControl();
            //_ = Task.Run(async () => await InitializationClientSignalRAsync()); //<<shin>>   //app.config에서 사용여부 추가해주세요
            if (!Global.IsDebug)
            {
                DevExpress.XtraSplashScreen.SplashScreenManager.CloseForm();
            }
        }

        private void InitializationLogger()
        {
            ucLog1.InitLoad();
            Log4NetLogger.AppendUI(ucLog1);
            Global.Logger.Info($"Starting Dualsoft v{Global.AppVersion}");
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {
            DocContr.CreateDocStart(this, TabbedView);
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MBox.AskYesNo("종료하시겠습니까?", K.AppName) == DialogResult.No)
            {
                e.Cancel = true;
            }
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
                .WithUrl("http://localhost:5000/hub/ds")
                .Build()
                ;
            await connection.StartAsync();
        }

        private void ace_Play_Click(object s, EventArgs e) => PcAction.Play(ace_Play);
        private void ace_Step_Click(object s, EventArgs e) => PcAction.Step(ace_Play);
        private void ace_Stop_Click(object s, EventArgs e) => PcAction.Stop(ace_Play);
        private void ace_Reset_Click(object s, EventArgs e) => PcAction.Reset(ace_Play, ace_HMI);

        private void simpleButton_OpenPLC_Click(object s, EventArgs e) => Global.OpenFolder(Global.ExportPathDS);
        private void ace_ImportXls_Click(object sender, EventArgs e) => Global.Notimplemented();
        private void ace_pcLinux_Click(object sender, EventArgs e) => Global.Notimplemented();
        private void ace_DocDiagram_Click(object sender, EventArgs e) => Global.Notimplemented();
        private void ace_PLCLogix5000_Click(object sender, EventArgs e) => Global.Notimplemented();
        private void ace_PLCWork3_Click(object sender, EventArgs e) => Global.Notimplemented();
        //DSFile.ZipDSFolder();  //open  <<shin>>  누번누르면 예외
        private void simpleButton_ExportDStoFile_Click(object sender, EventArgs e) => Global.OpenFolder(Global.ExportPathDS);

        private async void ace_ImportPPT_Click(object s, EventArgs e) => await ImportPowerPointWapper(null);
        private async void ace_pptReload_Click(object sender, EventArgs e) => await ImportPowerPointWapper(Files.GetLast());


        private void ace_pcWindow_Click(object s, EventArgs e) => DocContr.CreateDocDS(this, TabbedView);
        private void ace_PLCXGI_Click(object s, EventArgs e) => DocContr.CreateDocPLCLS(this, TabbedView);


        private void ratingControl_Speed_EditValueChanged(object s, EventArgs e) => ControlProperty.SetSpeed(Convert.ToInt32(ratingControl_Speed.EditValue));
        private void simpleButton_AllExpr_Click(object sender, EventArgs e) => DSFile.UpdateExprAll(this, toggleSwitch_showDeviceExpr.IsOn);
        private void ace_ExportExcel_Click(object s, EventArgs e) => XLS.ExportExcel(gridControl_exprotExcel);
        private async void ace_ExportWebHMI_Click(object sender, EventArgs e) => await HMI.ExportWebAsync(this);
        private void ace_ExportAppHMI_Click(object sender, EventArgs e) => HMI.ExportApp();
        private void simpleButton_layoutReset_Click(object s, EventArgs e) => LayoutForm.RestoreLayoutFromXml(dockManager);
        private void simpleButton_ClearLog_Click(object sender, EventArgs e) => LogicLog.ValueLogs.Clear();

        private void btn_OFF_Click(object sender, EventArgs e)
        {
        }

        private void btn_StepLongPress_Click(object sender, EventArgs e)
        {

        }
    }
}