using Dual.Common.Core;
using Dual.Common.Winform;
using IOMapViewer.Utils;
using Application = System.Windows.Forms.Application;

namespace IOMapViewer
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
#if DEBUG
            Global.IsDebug = true;
#endif

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            Log4NetLogger.Initialize(config.FilePath, "IOMapViewer");  // "App.config"

            Action<Exception> exceptionHander = new(ex =>
            {
                Log4NetLogger.Logger.Error($":::: Unhandled exception\r\n{ex}");
                if (Global.IsDebug)
                {
                    MBox.Error(ex.Message, "Error");
                }
            });

            UnhandledExceptionHandler.DefaultActionOnUnhandledException = exceptionHander;
            UnhandledExceptionHandler.DefaultActionOnUnhandledThreadException = exceptionHander;
            UnhandledExceptionHandler.DefaultActionOnUnhandledUnobservedTaskException = exceptionHander;
            UnhandledExceptionHandler.InstallUnhandledExceptionHandler();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            EditorSkin.InitSetting("The Bezier", "Mercury Ice");
            FormMain main = new();

            if (!Global.IsDebug) SplashScreenManager.ShowForm(main, typeof(SplashScreenDS));
            Application.Run(main);
        }
    }
}