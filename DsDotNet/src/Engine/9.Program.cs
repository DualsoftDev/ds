using System.IO;
using System.Configuration;
using Engine.Common.FS;
using System.Threading;
using System.Reflection;
using System.Windows.Forms;

namespace Engine;

class Program
{
    public static ILog Logger { get; private set; }
    public static ENGINE Engine { get; set; }

    static void PrepareLog4Net()
    {
        // Configure log4net
        var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        var configFile = new FileInfo(config.FilePath);

        log4net.Config.XmlConfigurator.ConfigureAndWatch(configFile);
        Logger = LogManager.GetLogger("EngineLogger");
        Logger.Info($"Starting Engine.");
        //Logger.Debug($"Configuration:\r\n{File.ReadAllText(config.FilePath)}");
        //Logger.Debug("");

        Log4NetHelper.Logger = Logger;
        Log4NetWrapper.SetLogger(Logger);
        Global.Logger = Logger;

        //var repo = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
        //var repoLogger = repo.GetLogger("EngineLogger", repo.LoggerFactory);
        //var traceAppender = new TraceLogAppender();
        //repoLogger.AddAppender(traceAppender);
    }

    static void PrepareThreadPool()
    {
        //ThreadPool.GetMinThreads(out int minWorker, out int minIOC);
        ThreadPool.SetMinThreads(60, 60);
    }

    static void Main(string[] args)
    {

        //PrepareThreadPool();
        PrepareLog4Net();
        SimpleExceptionHandler.InstallExceptionHandler();
        DllVersionChecker.IsValidExDLL(Assembly.GetExecutingAssembly());

        //Tester.DoSampleTestVps();
        //Tester.DoSampleTest();
        //Tester.DoSampleTestAdvanceReturn();
        //Tester.DoSampleTestHatOnHat();
        Tester.DoSampleTestDiamond();
        //Tester.DoSampleTestTriangle();
        //Tester.DoSampleTestAddressesAndLayouts();
    }
}
