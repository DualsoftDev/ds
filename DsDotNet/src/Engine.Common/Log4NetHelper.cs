using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using log4net;
using log4net.Appender;

namespace Engine.Common
{
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
}
