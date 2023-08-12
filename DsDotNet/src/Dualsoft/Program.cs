using DevExpress.LookAndFeel;
using Dual.Common.Core;
using Dual.Common.Winform;
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

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            Global.LogLevel = Log4NetLogger.Initialize(config.FilePath, "DSModelerLogger");  // "App.config"

            var exceptionHander = new Action<Exception>(ex =>
            {
                Log4NetLogger.Logger.Error($":::: Unhandled exception\r\n{ex}");
#if DEBUG
                MBox.Error(ex.Message, "Error");
#endif
            });

            UnhandledExceptionHandler.DefaultActionOnUnhandledException = exceptionHander;
            UnhandledExceptionHandler.DefaultActionOnUnhandledThreadException = exceptionHander;
            UnhandledExceptionHandler.DefaultActionOnUnhandledUnobservedTaskException = exceptionHander;
            UnhandledExceptionHandler.InstallUnhandledExceptionHandler();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            EditorSkin.InitSetting(SkinSvgPalette.Bezier.VSBlue);
            var main = new FormMain();
#if !DEBUG
            SplashScreenManager.ShowForm(main, typeof(SplashScreenDS));
#endif

            Application.Run(main);
        }
    }
}