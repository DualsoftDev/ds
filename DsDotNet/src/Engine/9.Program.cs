using System.IO;
using System.Configuration;
using Dual.Common;

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

        var repo = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
        var repoLogger = repo.GetLogger("EngineLogger", repo.LoggerFactory);
        var traceAppender = new TraceLogAppender();
        repoLogger.AddAppender(traceAppender);
    }
    static void Main(string[] args)
    {
        PrepareLog4Net();
        Tester.DoSampleTestVps();   // DoSampleTest
    }
}
