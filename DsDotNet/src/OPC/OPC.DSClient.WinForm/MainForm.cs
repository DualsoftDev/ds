using DevExpress.Data.Helpers;
using DevExpress.Mvvm.Native;
using DevExpress.XtraBars.FluentDesignSystem;
using DevExpress.XtraEditors;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.Controls;
using Opc.Ua.Configuration;
using System;
using System.Data;
using System.Windows.Forms;

namespace OPC.DSClient.WinForm
{
    public partial class MainForm : FluentDesignForm
    {
        private readonly ConnectServerCtrl ConnectServerCTRL = new ConnectServerCtrl();
        private readonly OpcTagManager _OpcTagManager = new OpcTagManager();
        
        public MainForm()
        {
            InitializeComponent();
            InitializeEvents();
            InitializeOPC();
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
                _OpcTagManager.LoadTags(session);

                ucDsTable1.SetDataSource(_OpcTagManager);
                ucDsTree1.SetDataSource(_OpcTagManager);
                ucDsHeatmap1.SetDataSource(_OpcTagManager);
                ucDsSunburst1.SetDataSource(_OpcTagManager);
                //ucDsSankey1.SetDataSource(_OpcTagManager);
                ucDsSankey1.SetSampleDataSource();
                ucDsTreemap1.SetDataSource(_OpcTagManager);
                //ucDsTreemap1.SetSampleDataSource();
            }
        }

        private void InitializeEvents()
        {
            ace_Tree.Click += (s, e) => ShowControl(ucDsTree1);
            ace_Table.Click += (s, e) => ShowControl(ucDsTable1);
            ace_Sunburst.Click += (s, e) => ShowControl(ucDsSunburst1);
            ace_Treemap.Click += (s, e) => ShowControl(ucDsTreemap1);
            ace_Heatmap.Click += (s, e) => ShowControl(ucDsHeatmap1);
            ace_Sankey.Click += (s, e) => ShowControl(ucDsSankey1);
        }

        private void ShowControl(Control control)
        {
            ucDsTree1.Hide();
            ucDsTable1.Hide();
            ucDsSunburst1.Hide();
            ucDsTreemap1.Hide();
            ucDsHeatmap1.Hide();
            ucDsSankey1.Hide();
            control.Show();
        }

    }
}
