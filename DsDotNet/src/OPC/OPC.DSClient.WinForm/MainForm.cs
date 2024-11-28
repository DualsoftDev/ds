using DevExpress.LookAndFeel;
using DevExpress.XtraBars.FluentDesignSystem;
using DevExpress.XtraEditors;
using DevExpress.XtraSplashScreen;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.Controls;
using Opc.Ua.Configuration;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace OPC.DSClient.WinForm
{
    public partial class MainForm : FluentDesignForm
    {
        private readonly ConnectServerCtrl ConnectServerCTRL = new();
        private readonly OpcTagManager _OpcTagManager = new();
        public MainForm()
        {
            InitializeComponent();
            LoadSkinSettings();



            InitializeEvents();
            InitializeMenu();
            InitializeOPC();
            SetupNavigationPages();


            var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
            Text = $"DS Pilot v{version}";
            nFrame1.SelectedPageChanged += NavigationFrame1_SelectedPageChanged;
            nFrame1.SelectedPage = nPage7;
        }

        private void InitializeOPC()
        {
            var application = new ApplicationInstance
            {
                ApplicationName = "UA Reference Client",
                ApplicationType = ApplicationType.Client,
                ConfigSectionName = "DsOpcClient"
            };

            // 애플리케이션 구성 로드
            application.LoadApplicationConfiguration(false).Wait();

            // 인증서 확인
            if (!application.CheckApplicationInstanceCertificate(false, 0).Result)
                throw new Exception("Invalid application certificate!");

            // OPC 서버 연결 설정
            ConnectServerCTRL.Configuration = application.ApplicationConfiguration;
            ConnectServerCTRL.ServerUrl = "opc.tcp://192.168.9.203:2747/DS";
            ConnectServerCTRL.ConnectComplete += OnConnectComplete;
            ConnectServerCTRL.ReconnectComplete += OnConnectComplete;

            // 연결 시도
            var timeoutMs = 5000;
            ConnectWithTimeout(timeoutMs); // 타임아웃 5초
        }

        private void ConnectWithTimeout(int timeoutMs)
        {
            ConnectServerCTRL.DiscoverTimeout = timeoutMs;
            ConnectServerCTRL.Connect();
            Task.Run(async () =>
            {
                await Task.Delay(timeoutMs);
                if (ConnectServerCTRL.Session == null)
                {
                    XtraMessageBox.Show("Connection failed. " +
                        "\nPlease check the OPC server address and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }


        private void SetupNavigationPages()
        {
            nPage1.Controls.Add(ucDsTree1);
            nPage2.Controls.Add(ucDsTable1);
            nPage3.Controls.Add(ucDsSunburst1);
            nPage4.Controls.Add(ucDsTreemap1);
            nPage5.Controls.Add(ucDsHeatmap1);
            nPage6.Controls.Add(ucDsSankey1);
            nPage7.Controls.Add(ucDsDataGridFlow);
            nPage8.Controls.Add(ucDsDataGridIO);
            nPage9.Controls.Add(ucDsTextEdit1);
            //nPage10.Controls.Add(ucDsTextEdit1);
        }


        private void NavigationFrame1_SelectedPageChanged(object sender, DevExpress.XtraBars.Navigation.SelectedPageChangedEventArgs e)
        {
            if (e.Page is DevExpress.XtraBars.Navigation.NavigationPage tabPage) // NavigationPage로 캐스팅
            {
                if (tabPage.Controls.Count > 0 && tabPage.Controls[0] is XtraUserControl userControl)
                {
                    Global.SelectedUserControl = userControl;
                    ResetAceSelection(); // 모든 컨트롤 상태 초기화

                    // 선택된 페이지에 따라 컨트롤 상태 업데이트
                    var selectedControl = tabPage.Name switch
                    {
                        "nPage1" => ace_Tree,
                        "nPage2" => ace_Table,
                        "nPage3" => ace_Sunburst,
                        "nPage4" => ace_Treemap,
                        "nPage5" => ace_Heatmap,
                        "nPage6" => ace_Sankey,
                        "nPage7" => ace_DataGridFlow,
                        "nPage8" => ace_DataGridIO,
                        "nPage9" => ace_TextEdit,
                        "nPage10" => ace_HMI,
                        _ => null
                    };

                    if (selectedControl != null)
                        selectedControl.Appearance.Normal.BackColor = Color.DarkBlue;
                }
            }
            else
            {
                Global.SelectedUserControl = null; // 유효하지 않은 경우 null 처리
                ResetAceSelection(); // 모든 컨트롤 상태 초기화
            }
            void ResetAceSelection()
            {
                //new[] { ace_Tree, ace_Table, ace_Sunburst, ace_Treemap, ace_Heatmap, ace_Sankey, ace_DataGridFlow, ace_DataGridIO, ace_TextEdit, ace_HMI }
                foreach (var aceControl in accordionControl1.Elements)
                    aceControl.Appearance.Normal.BackColor = Color.Transparent;
            }
        }


        private void SaveSkinSettings(string skinName)
        {
            Properties.Settings.Default.SelectedSkin = skinName;
            Properties.Settings.Default.Save();
        }

        private void LoadSkinSettings()
        {
            var savedSkin = Properties.Settings.Default.SelectedSkin;
            if (!string.IsNullOrEmpty(savedSkin))
                UserLookAndFeel.Default.SetSkinStyle("The Bezier", savedSkin);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (XtraMessageBox.Show(this, "Are you sure you want to exit?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }

            base.OnFormClosing(e);
            SaveSkinSettings(UserLookAndFeel.Default.ActiveSvgPaletteName);
        }

        private void OnConnectComplete(object? sender, EventArgs e)
        {
            var session = ConnectServerCTRL.Session;
            if (session != null && !_OpcTagManager.OpcTags.Any())
            {
                SplashScreenManager.ShowForm(this, typeof(SplashScreenDS));

                _OpcTagManager.LoadTags(session);

                ucDsSankey1.SetDataSource(_OpcTagManager);
                ucDsTable1.SetDataSource(_OpcTagManager);
                ucDsTree1.SetDataSource(_OpcTagManager);
                ucDsHeatmap1.SetDataSource(_OpcTagManager);
                ucDsSunburst1.SetDataSource(_OpcTagManager);
                ucDsTreemap1.SetDataSource(_OpcTagManager);
                ucDsDataGridFlow.SetDataSource(_OpcTagManager, true);
                ucDsDataGridIO.SetDataSource(_OpcTagManager, false);
                ucDsTextEdit1.SetDataSource(_OpcTagManager);

                SplashScreenManager.CloseForm();
            }
        }

     
    }
}
