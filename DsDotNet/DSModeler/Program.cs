using Application = System.Windows.Forms.Application;

namespace DSModeler
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
            Log4NetLogger.Initialize(config.FilePath, "DSModelerLogger");  // "App.config"

            Action<Exception> exceptionHander = new(ex =>
            {
                DsProcessEvent.DoWork(100);
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

            //PLCMonitorEngine s = new();
            //Task.Run(() =>
            //{
            //    s.TestScan();
            //});
            //s.PLCTagChangedSubject.Subscribe(x =>
            //{
            //    Trace.WriteLine($"{x.Tag} => {x.Value}");

            //    //ds value update  
            //});

            Application.Run(main);
        }
    }
}