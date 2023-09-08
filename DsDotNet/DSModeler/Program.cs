using DevExpress.XtraSplashScreen;
using DSModeler.Utils;
using Dual.Common.Core;
using Dual.Common.Winform;
using Engine.Core;
using System;
using System.Configuration;
using System.Windows.Forms;

namespace DSModeler
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
#if DEBUG
            Global.IsDebug = true;
#endif
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            Log4NetLogger.Initialize(config.FilePath, "DSModelerLogger");  // "App.config"

            var exceptionHander = new Action<Exception>(ex =>
            {
                DsProcessEvent.DoWork(100);
                Log4NetLogger.Logger.Error($":::: Unhandled exception\r\n{ex}");
                if (Global.IsDebug)
                    MBox.Error(ex.Message, "Error");
            });

            UnhandledExceptionHandler.DefaultActionOnUnhandledException = exceptionHander;
            UnhandledExceptionHandler.DefaultActionOnUnhandledThreadException = exceptionHander;
            UnhandledExceptionHandler.DefaultActionOnUnhandledUnobservedTaskException = exceptionHander;
            UnhandledExceptionHandler.InstallUnhandledExceptionHandler();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            EditorSkin.InitSetting("The Bezier", "Mercury Ice");
            var main = new FormMain();

            if (!Global.IsDebug)
                SplashScreenManager.ShowForm(main, typeof(SplashScreenDS));
            Application.Run(main);
        }
    }
}