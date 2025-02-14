using System.IO;


using log4net;
using System.Diagnostics;
using log4net.Appender;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using log4net.Core;
using System;
using System.Reactive.Disposables;

namespace Dual.Common.Base.CS
{
    public class DcLogger
    {
        /// <summary>
        /// Glolbal ILog 객체.<br/> - Dual.Common.Base.CS.Logger
        /// </summary>
        public static ILog Logger { get; set; }
        /// <summary>
        /// VisualStudio Output 창에 trace log 기록 여부.
        /// <br/> - Release 버젼에서도 EnableTrace 값이 true 인 경우, logging 수행 함.
        /// <br/> - Default: Debug 에서만 활성화
        /// </summary>
        public static bool EnableTrace { get; set; }

        /// <summary>
        /// Trace 를 활성/비활성화 시키는 disposable 반환.  dispose 시 원복
        /// </summary>
        public static IDisposable CreateTraceXabler(bool enable)
        {
            var backup = EnableTrace;
            EnableTrace = enable;
            return Disposable.Create(() => EnableTrace = backup);
        }

        /// <summary>
        /// Trace 를 활성화 시키는 disposable 반환.  dispose 시 원복
        /// </summary>
        public static IDisposable CreateTraceEnabler() => CreateTraceXabler(true);

        /// <summary>
        /// Trace 를 비활성화 시키는 disposable 반환.  dispose 시 원복
        /// </summary>
        public static IDisposable CreateTraceDisabler() => CreateTraceXabler(false);

        public static string Initialize(string configFile, string loggerName) {
            Logger = LogManager.GetLogger(loggerName);

            // Configure log4net
            //XmlConfigurator.Configure(new FileInfo(configFile));
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(configFile));
            Logger.Info($"Starting {loggerName}.  pid={Process.GetCurrentProcess().Id}");

            var repo = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
            var repoLogger = repo.GetLogger(loggerName, repo.LoggerFactory);
            var traceAppender = new TraceLogAppender();
            repoLogger.AddAppender(traceAppender);


            if (Logger.IsDebugEnabled) return "debug";
            if (Logger.IsInfoEnabled) return "info";
            if (Logger.IsWarnEnabled) return "warn";
            return "error";
        }
        public static void AppendUI(object IAppenderObj)
        {
            IAppender logAppender = IAppenderObj as IAppender;
            var root = ((log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository()).Root;
            root.AddAppender(logAppender);
        }


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

            return Logger;

            //var repo = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
            //var repoLogger = repo.GetLogger("EngineLogger", repo.LoggerFactory);
            //var traceAppender = new TraceLogAppender();
            //repoLogger.AddAppender(traceAppender);
        }



        public static void Debug(string message) { Logger.Debug(message); }
        public static void Info (string message) { Logger.Info (message); }
        public static void Warn (string message) { Logger.Warn (message); }
        public static void Alarm(string message) { Logger.Warn (message); }
        public static void Fail (string message) { Logger.Error(message); }
        public static void Error(string message) { Logger.Error(message); }
    }
}
