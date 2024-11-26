using System;
using System.Runtime.Versioning;
using System.Windows.Forms;
using DevExpress.XtraSplashScreen;
using Opc.Ua;
using Opc.Ua.Client.Controls;
using Opc.Ua.Configuration;
using static System.Net.Mime.MediaTypeNames;
using Application = System.Windows.Forms.Application;

[assembly: SupportedOSPlatform("windows")]

namespace OPC.DSClient.WinForm
{
    static class Program
    {
          
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            // Initialize the user interface.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ApplicationInstance.MessageDlg = new ApplicationMessageDlg();
            ApplicationInstance application = new ApplicationInstance();
            application.ApplicationName = "UA Reference Client";
            application.ApplicationType = ApplicationType.Client;
            application.ConfigSectionName = "DsOpcClient";

            try
            {
               
                // load the application configuration.
                application.LoadApplicationConfiguration(false).Wait();

                // check the application certificate.
                var certOK = application.CheckApplicationInstanceCertificate(false, 0).Result;
                if (!certOK)
                {
                    throw new Exception("Application instance certificate invalid!");
                }
                // run the application interactively.
                Application.Run(new MainForm());
            }
            catch (Exception e)
            {
                ExceptionDlg.Show(application.ApplicationName, e);
            }
        }
    }


}
