using Dsu.Common.CS.LSIS;
using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MelsecConverter
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var logger = LogManager.GetLogger("AppFx");
            FormAddressMapper.Logger = logger;

            InstallUnhandledExceptionHandler();


            // Configure log4net
            XmlConfigurator.Configure(new FileInfo(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile));

#if !DEBUG
            DevExpress.XtraSplashScreen.SplashScreenManager.ShowForm(typeof(SplashScreen1), true, true);
#endif


            var root = ((log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository()).Root;
            var form = new FormAddressMapper();
            root.AddAppender(form);


            Application.Run(form);

            void InstallUnhandledExceptionHandler()
            {
                UnhandledExceptionEventHandler unhandled = (s, e) => {
                    System.Media.SystemSounds.Beep.Play();
                    var msg = $"XXXUnhandled Exception: {e} {e.ExceptionObject}";
                    //DxMessageBox.Error(msg);
                    logger.Error(msg);
                };

                ThreadExceptionEventHandler threadedUnhandled = (s, e) => {
                    System.Media.SystemSounds.Beep.Play();
                    var msg = $"Threaded unhandled Exception: {e.Exception}";
                    //DxMessageBox.Error(msg);
                    logger.Error(msg);
                };

                EventHandler<UnobservedTaskExceptionEventArgs> onUnobservedTaskException = (s, e) =>
                {
                    System.Media.SystemSounds.Beep.Play();
                    var msg = $"Unobserved task Exception: {e.Exception}";
                    //DxMessageBox.Error(msg);
                    logger.Error(msg);

                    // set observerd 처리 안하면 re-throw 됨
                    e.SetObserved();
                };

                EmException.InstallUnhandledExceptionHandler(unhandled, threadedUnhandled, onUnobservedTaskException);
            }


        }
    }
}
