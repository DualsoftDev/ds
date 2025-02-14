using System.Diagnostics;

using log4net.Appender;
using log4net.Core;


namespace Dual.Common.Base.CS
{
    public class TraceLogAppender : IAppender
    {
        private string _name = "TraceLogAppender";
        public string Name { get => _name; set => _name = value; }

        public void Close() {}

        public void DoAppend(LoggingEvent logEntry)
        {
            var msg = logEntry.MessageObject.ToString();
            var level = logEntry.Level.Name;
            var now = logEntry.TimeStamp;
            Trace.WriteLine($"{now} {level} {msg}");
        }

        public static void Add(string loggerName)
        {
            if (Debugger.IsAttached)
            {
                var repo = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
                var repoLogger = repo.GetLogger(loggerName, repo.LoggerFactory);
                var traceAppender = new TraceLogAppender();
                repoLogger.AddAppender(traceAppender);
            }
        }
    }
}
