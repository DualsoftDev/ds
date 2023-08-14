using Dual.Common.Core;
using Engine.Core;
using log4net;
using System;
using System.IO;
using System.Reactive.Subjects;
using System.Reflection;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;

namespace DSModeler
{
    public static class K
    {
        public const string AppName = "DSModeler";
        public const string RegSkin = "RegSkin";
        public const string LastPath = "LastPath";
        public const string LastFiles = "LastFiles";
        public const string SimSpeed = "SimSpeed";
        public const string DocStartPage = "시작 페이지";
        public const string DocPLC = "PLC 생성";
        public const string DocDS = "모델 출력";
        public const string DocExpression = "수식";

    }
    public static class Global
    {
        public static ILog Logger => Log4NetLogger.Logger;
        public static string LogLevel { get; set; }
        public static int SimSpeed { get; set; } = 3;
        public static bool SimLogHide { get; set; }
        public static string ExportPathPLC { get; set; }
        public static string ExportPathXLS { get; set; }

        public static DsSystem ActiveSys { get; set; }

        public static Version ver = Assembly.GetEntryAssembly().GetName().Version;

        public static Subject<Tuple<CoreModule.Vertex, Status4>> StatusChangeSubject = new Subject<Tuple<CoreModule.Vertex, Status4>>();

        public static string DefaultFolder =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Dualsoft",
                "Modeler"
            );

        public static bool IsLoadedPPT()
        {
            if (Global.ActiveSys == null)
            {
                Global.Logger.Warn("PPT 파일을 먼저 불러오세요");
                return false;
            }
            else
                return true;
        }

        public static string DefaultAppSettingFolder => Path.Combine(DefaultFolder, "AppSetting");
        public static string AppVersion => $"{ver.Major}.{ver.Minor}.{ver.Build}";

    }

}


