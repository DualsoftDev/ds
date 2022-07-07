using System.Diagnostics;

using log4net.Appender;
using log4net.Core;


namespace Dual.Common.Core
{
    public class TraceLogAppender : IAppender
    {
        private string _name = "TraceLogAppender";
        public string Name { get => _name; set => _name = value; }

        public void Close() {}

        public void DoAppend(LoggingEvent loggingEvent)
        {
#if DEBUG
            var msg = loggingEvent.MessageObject.ToString();
            var level = loggingEvent.Level.Name;
            var now = loggingEvent.TimeStamp;
            Trace.WriteLine($"{now} {level} {msg}");
#endif
        }
    }
}
