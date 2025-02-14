using System.IO;


using log4net;
using System.Diagnostics;
using log4net.Appender;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using log4net.Core;
using System;

namespace Dual.Common.Core
{
    [Obsolete("DcLogger 로 대체 (Dual.Common.Base.CS)")]
    public class Log4NetLogger
    {
        [Obsolete("DcLogger 로 대체 (Dual.Common.Base.CS)")]
        public static ILog Logger { get => throw new Exception("DcLogger 로 대체 (Dual.Common.Base.CS)"); set => throw new Exception("DcLogger 로 대체 (Dual.Common.Base.CS)"); }

        [Obsolete("DcLogger 로 대체 (Dual.Common.Base.CS)")]
        public static string Initialize(string configFile, string loggerName) => throw new Exception("DcLogger 로 대체 (Dual.Common.Base.CS)");
        [Obsolete("DcLogger 로 대체 (Dual.Common.Base.CS)")]
        public static void AppendUI(object IAppenderObj) => throw new Exception("DcLogger 로 대체 (Dual.Common.Base.CS)");
        [Obsolete("DcLogger 로 대체 (Dual.Common.Base.CS)")]
        public static string GetLogFileName(string appenderName = "FileLogger") => throw new Exception("DcLogger 로 대체 (Dual.Common.Base.CS)");
        [Obsolete("DcLogger 로 대체 (Dual.Common.Base.CS)")]
        public static IEnumerable<string> GetLogFilePaths(string appenderName = "FileLogger") => throw new Exception("DcLogger 로 대체 (Dual.Common.Base.CS)");
        [Obsolete("DcLogger 로 대체 (Dual.Common.Base.CS)")]
        public static void ChangeLogLevel(Level level) => throw new Exception("DcLogger 로 대체 (Dual.Common.Base.CS)");
        [Obsolete("DcLogger 로 대체 (Dual.Common.Base.CS)")]
        public static ILog PrepareLog4Net(string loggerName) => throw new Exception("DcLogger 로 대체 (Dual.Common.Base.CS)");
        [Obsolete("DcLogger 로 대체 (Dual.Common.Base.CS)")]
        public static void Debug(string message) => throw new Exception("DcLogger 로 대체 (Dual.Common.Base.CS)");
        [Obsolete("DcLogger 로 대체 (Dual.Common.Base.CS)")]
        public static void Info (string message) => throw new Exception("DcLogger 로 대체 (Dual.Common.Base.CS)");
        [Obsolete("DcLogger 로 대체 (Dual.Common.Base.CS)")]
        public static void Warn (string message) => throw new Exception("DcLogger 로 대체 (Dual.Common.Base.CS)");
        [Obsolete("DcLogger 로 대체 (Dual.Common.Base.CS)")]
        public static void Alarm(string message) => throw new Exception("DcLogger 로 대체 (Dual.Common.Base.CS)");
        [Obsolete("DcLogger 로 대체 (Dual.Common.Base.CS)")]
        public static void Fail (string message) => throw new Exception("DcLogger 로 대체 (Dual.Common.Base.CS)");
        [Obsolete("DcLogger 로 대체 (Dual.Common.Base.CS)")]
        public static void Error(string message) => throw new Exception("DcLogger 로 대체 (Dual.Common.Base.CS)");
    }
}
