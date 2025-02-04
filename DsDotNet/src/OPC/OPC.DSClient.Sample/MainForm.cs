using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.Controls;

namespace OPC.DSClient
{
    /// <summary>
    /// Main form for the OPC UA Client application.
    /// </summary>
    public partial class MainForm : Form
    {
        #region Private Fields
        private readonly ApplicationConfiguration m_configuration;
        private Session? m_session;
        private bool m_connectedOnce;
        #endregion

        #region Constructors
        public MainForm(ApplicationConfiguration configuration)
        {
            InitializeComponent();
            m_configuration = configuration;
            InitializeClient();
        }
        #endregion

        #region Initialization
        private void InitializeClient()
        {
            ConnectServerCTRL.Configuration = m_configuration;
            ConnectServerCTRL.ServerUrl = "opc.tcp://127.139.3.28:2747";
            //ConnectServerCTRL.ServerUrl = "opc.tcp://192.168.9.151:2747";
            //ConnectServerCTRL.ServerUrl = "opc.tcp://localhost:2747";
            Text = m_configuration.ApplicationName;

            ConnectServerCTRL.Connect();
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
            });
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SafeExecute(() => ConnectServerCTRL.Disconnect());
        }

        private void tagViewerToolStripMenuItem_Click(object sender, EventArgs e)
        {
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
                var executablePath = Path.GetDirectoryName(Application.ExecutablePath);
                if (executablePath != null)
                {
                    var helpPath = Path.Combine(executablePath, "WebHelp", "overview_-_reference_client.htm");
                    System.Diagnostics.Process.Start(helpPath);
                }
                else
                {
                    throw new InvalidOperationException("Executable path is null.");
                }
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