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
            InitializeOPC();
            SetupNavigationPages();
            var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
            Text = $"DS Pilot v{version}";  
            navigationFrame1.SelectedPageChanged += NavigationFrame1_SelectedPageChanged;
            navigationFrame1.SelectedPage = navigationPage8;
        }

        private void SetupNavigationPages()
        {
            navigationPage1.Controls.Add(ucDsTree1);
            navigationPage2.Controls.Add(ucDsTable1);
            navigationPage3.Controls.Add(ucDsSunburst1);
            navigationPage4.Controls.Add(ucDsTreemap1);
            navigationPage5.Controls.Add(ucDsHeatmap1);
            navigationPage6.Controls.Add(ucDsSankey1);
            navigationPage7.Controls.Add(ucDsDataGrid1);
            navigationPage8.Controls.Add(ucDsTextEdit1);
        }


        private void NavigationFrame1_SelectedPageChanged(object sender, DevExpress.XtraBars.Navigation.SelectedPageChangedEventArgs e)
        {
            if (e.Page is DevExpress.XtraBars.Navigation.NavigationPage tabPage) // NavigationPage로 캐스팅
            {
                if (tabPage.Controls.Count > 0 && tabPage.Controls[0] is XtraUserControl userControl)
                {
                    Global.SelectedUserControl = userControl;

                    // 선택된 페이지에 따라 컨트롤 상태 업데이트
                    UpdateAceSelection(tabPage.Name);
                }
            }
            else
            {
                Global.SelectedUserControl = null; // 유효하지 않은 경우 null 처리
                ResetAceSelection(); // 모든 컨트롤 상태 초기화
            }
        }

        private void UpdateAceSelection(string? pageName)
        {
            ResetAceSelection();

            var selectedControl = pageName switch
            {
                "navigationPage1" => ace_Tree,
                "navigationPage2" => ace_Table,
                "navigationPage3" => ace_Sunburst,
                "navigationPage4" => ace_Treemap,
                "navigationPage5" => ace_Heatmap,
                "navigationPage6" => ace_Sankey,
                "navigationPage7" => ace_DataGrid,
                "navigationPage8" => ace_TextEdit,
                _ => null
            };

            if (selectedControl != null)
                selectedControl.Appearance.Normal.BackColor = Color.DarkBlue;
        }

        private void ResetAceSelection()
        {
            foreach (var aceControl in new[] { ace_Tree, ace_Table, ace_Sunburst, ace_Treemap, ace_Heatmap, ace_Sankey, ace_DataGrid, ace_TextEdit })
                aceControl.Appearance.Normal.BackColor = Color.Transparent;
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
                UserLookAndFeel.Default.SetSkinStyle("Basic", savedSkin);
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

        private void InitializeOPC()
        {
            var application = new ApplicationInstance
            {
                ApplicationName = "UA Reference Client",
                ApplicationType = ApplicationType.Client,
                ConfigSectionName = "DsOpcClient"
            };

            application.LoadApplicationConfiguration(false).Wait();

            if (!application.CheckApplicationInstanceCertificate(false, 0).Result)
                throw new Exception("Invalid application certificate!");

            ConnectServerCTRL.Configuration = application.ApplicationConfiguration;
            ConnectServerCTRL.ServerUrl = "opc.tcp://192.168.9.203:2747/DS";
            ConnectServerCTRL.ConnectComplete += OnConnectComplete;
            ConnectServerCTRL.ReconnectComplete += OnConnectComplete;
            ConnectServerCTRL.Connect();
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
                ucDsDataGrid1.SetDataSource(_OpcTagManager);
                ucDsTextEdit1.SetDataSource(_OpcTagManager);

                SplashScreenManager.CloseForm();
            }
        }

        private void InitializeEvents()
        {
            ace_Tree.Click += (_, _) => navigationFrame1.SelectedPage = navigationPage1;
            ace_Table.Click += (_, _) => navigationFrame1.SelectedPage = navigationPage2;
            ace_Sunburst.Click += (_, _) => navigationFrame1.SelectedPage = navigationPage3;
            ace_Treemap.Click += (_, _) => navigationFrame1.SelectedPage = navigationPage4;
            ace_Heatmap.Click += (_, _) => navigationFrame1.SelectedPage = navigationPage5;
            ace_Sankey.Click += (_, _) => navigationFrame1.SelectedPage = navigationPage6;
            ace_DataGrid.Click += (_, _) => navigationFrame1.SelectedPage = navigationPage7;
            ace_TextEdit.Click += (_, _) => navigationFrame1.SelectedPage = navigationPage8;
        }
    }
}
