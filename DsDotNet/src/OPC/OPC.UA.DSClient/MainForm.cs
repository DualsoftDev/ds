using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.Controls;

namespace OPC.UA.DSClient.Winform
{
    /// <summary>
    /// Main form for the OPC UA Client application.
    /// </summary>
    public partial class MainForm : Form
    {
        #region Private Fields
        private readonly ApplicationConfiguration m_configuration;
        private Session m_session;
        private bool m_connectedOnce;
        private DataTable m_tagDataTable = new DataTable();
        #endregion

        #region Constructors
        public MainForm(ApplicationConfiguration configuration)
        {
            InitializeComponent();
            m_configuration = configuration;
            InitializeClient();
            InitializeTagDataTable();
        }
        #endregion

        #region Initialization
        private void InitializeClient()
        {
            ConnectServerCTRL.Configuration = m_configuration;
            ConnectServerCTRL.ServerUrl = "opc.tcp://localhost:62748/DS";
            Text = m_configuration.ApplicationName;

            ConnectServerCTRL.ConnectComplete += Server_ConnectComplete;
            ConnectServerCTRL.ReconnectComplete += Server_ReconnectComplete;
            ConnectServerCTRL.ReconnectStarting += Server_ReconnectStarting;

            ConnectServerCTRL.Connect();
        }

        private void InitializeTagDataTable()
        {
            m_tagDataTable.Columns.Add("Tag Name", typeof(string));
            m_tagDataTable.Columns.Add("Value", typeof(string));
            m_tagDataTable.Columns.Add("Data Type", typeof(string));
            m_tagDataTable.Columns.Add("Timestamp", typeof(DateTime));
        }
        #endregion

        #region Event Handlers
        private async void Server_ConnectMI_Click(object sender, EventArgs e)
        {
            await SafeExecuteAsync(ConnectServerCTRL.Connect);
        }

        private void Server_DisconnectMI_Click(object sender, EventArgs e)
        {
            SafeExecute(() => ConnectServerCTRL.Disconnect());
        }

        private void Server_DiscoverMI_Click(object sender, EventArgs e)
        {
            SafeExecute(() => ConnectServerCTRL.Discover(null));
        }

        private void Server_ConnectComplete(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                m_session = ConnectServerCTRL.Session;
                if (m_session != null && !m_connectedOnce)
                {
                    m_connectedOnce = true;
                    var tagDataSource = new TagDataSource(m_session);
                    tagDataSource.UpdateTagData();
                 m_tagDataTable = tagDataSource.TagDataTable;
                }
                BrowseCTRL.Initialize(m_session, ObjectIds.ObjectsFolder, ReferenceTypeIds.Organizes, ReferenceTypeIds.Aggregates);
            });
        }

        private void Server_ReconnectStarting(object sender, EventArgs e)
        {
            SafeExecute(() => BrowseCTRL.ChangeSession(null));
        }

        private void Server_ReconnectComplete(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                m_session = ConnectServerCTRL.Session;
                BrowseCTRL.ChangeSession(m_session);

                var tagDataSource = new TagDataSource(m_session);
                tagDataSource.UpdateTagData();
                m_tagDataTable = tagDataSource.TagDataTable;
            });
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SafeExecute(() => ConnectServerCTRL.Disconnect());
        }

        private void tagViewerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_tagDataTable.Rows.Count == 0)
            {
                MessageBox.Show("No tag data available to display.", "Tag Viewer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var tagViewerForm = new TagViewerForm(m_tagDataTable);
            tagViewerForm.Show();
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Exit this application?", "Reference Client", MessageBoxButtons.YesNoCancel);
            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void Help_ContentsMI_Click(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                var helpPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "WebHelp", "overview_-_reference_client.htm");
                System.Diagnostics.Process.Start(helpPath);
            });
        }
        #endregion

        #region Private Fields
        
        private void SafeExecute(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                ClientUtils.HandleException(Text, ex);
            }
        }

        private async Task SafeExecuteAsync(Func<Task> asyncAction)
        {
            try
            {
                await asyncAction();
            }
            catch (Exception ex)
            {
                ClientUtils.HandleException(Text, ex);
            }
        }
        #endregion
    }
}