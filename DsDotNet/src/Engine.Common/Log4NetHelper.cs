using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Engine.Common.FS;
using log4net;
using log4net.Appender;
using log4net.Core;

namespace Engine.Common;

public class Log4NetHelper
{
    public static ILog Logger { get; set; }

    /// <summary>
    /// Application 의 Log4Net log file name 을 반환
    /// </summary>
    public static string GetLogFileName(string appenderName = "FileLogger")
    {
        // log4net.Config.XmlConfigurator.Configure(...) 가 수행된 이후에 실행되어야 한다.
        var rootAppender =
            LogManager.GetRepository()
            .GetAppenders()
            .OfType<FileAppender>()
            .FirstOrDefault(fa => fa.Name == appenderName)
            ;

        var logFile = rootAppender?.File ?? string.Empty;

        // e.g "F:\Git\lsis\lpb\src\Data\logLpbAgentService-20210824.txt"
        return logFile;
    }


    public static IEnumerable<string> GetLogFilePaths(string appenderName = "FileLogger")
    {
        var logFile = GetLogFileName(appenderName);
        var path = Path.GetDirectoryName(logFile);
        var name = Path.GetFileNameWithoutExtension(logFile);
        var ext = Path.GetExtension(logFile);
        var stem = name.Substring(0, name.Length - "-yyyyMMdd".Length);
        return Directory.GetFiles(path)
            .Where(p => Path.GetFileName(p).StartsWith(stem))
            ;
    }

    // https://stackoverflow.com/questions/715941/change-log4net-logging-level-programmatically
    public static void ChangeLogLevel(Level level)
    {
        var repo = ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository());
        repo.Threshold = level;
    }

    public static ILog PrepareLog4Net(string loggerName)
    {
        // Configure log4net
        var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        var configFile = new FileInfo(config.FilePath);

        log4net.Config.XmlConfigurator.ConfigureAndWatch(configFile);
        Logger = LogManager.GetLogger(loggerName);
        Logger.Info($"Starting Logging.");

        Log4NetHelper.Logger = Logger;
        Log4NetWrapper.SetLogger(Logger);
        return Logger;

        //var repo = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
        //var repoLogger = repo.GetLogger("EngineLogger", repo.LoggerFactory);
        //var traceAppender = new TraceLogAppender();
        //repoLogger.AddAppender(traceAppender);
    }

    ///// <summary>
    ///// Application 의 Log4Net log file 들을 수집해서 zipped bytes 로 반환
    ///// Log file 이 -yyyyMMdd 로 끝난다고 가정
    ///// </summary>
    //public static byte[] GetLogFileZippedBytes(string appenderName = "FileLogger")
    //{
    //    var zdir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString().Substring(0, 8));
    //    Directory.CreateDirectory(zdir);

    //    GetLogFilePaths(appenderName)
    //        .Iter(p => File.Copy(p, $@"{zdir}\{Path.GetFileName(p)}"))
    //        ;

    //    var zipped = EmZip.FolderToZippedBytes(zdir);
    //    Directory.Delete(zdir, true);
    //    return zipped;
    //}
}
