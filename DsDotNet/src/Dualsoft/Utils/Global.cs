using log4net;


using System;
using System.IO;
using System.Reflection;

namespace Dualsoft
{
    public static class Global
    {
        public static ILog Logger { get; set; }
        public static Version ver = Assembly.GetEntryAssembly().GetName().Version;

        public static string DefaultFolder =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Dualsoft"
            );

        public static string DefaultAppSettingFolder => Path.Combine(DefaultFolder, "AppSetting");
        public static string AppVersion => $"{ver.Major}.{ver.Minor}.{ver.Build}";

    }
}


