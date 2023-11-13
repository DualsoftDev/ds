using Dual.Common.Core;
using log4net;

namespace IOMapViewer.Utils;

public static class K
{
    public const string AppName = "IOMapViewer";
    public const string DocI = "I";
    public const string DocO = "O";
    public const string DocM = "M";
}

public static class Global
{
    public static ILog Logger => Log4NetLogger.Logger;
    public static bool IsDebug { get; set; }
    public static int SimSpeed { get; set; } = 3;
    public static int RunCountIn { get; set; } = 0;
    public static int RunCountOut { get; set; } = 0;
    public static bool LayoutMenumExpand { get; set; }
    public static bool LayoutGraphLineType { get; set; }
    public static bool SimReset { get; set; }

    public static string DefaultFolder =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Dualsoft",
            K.AppName);


    public static Version ver = Assembly.GetEntryAssembly().GetName().Version;

    public static string DefaultAppSettingFolder => Path.Combine(DefaultFolder, "AppSetting");
    public static string AppVersion => $"{ver.Major}.{ver.Minor}.{ver.Build}";
}