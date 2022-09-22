global using log4net;
global using System;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.Linq;

namespace Engine.Common;

public static class GlobalShortCuts
{
    public static void LogDebug(object message) => Global.Logger.Debug(message);
    public static void LogInfo(object message) => Global.Logger.Info(message);
    public static void LogWarn(object message) => Global.Logger.Warn(message);
    public static void LogError(object message) => Global.Logger.Error(message);
}

public delegate void ExceptionHandler(Exception ex);

public static class Global
{
    public static ILog Logger { get; set; }
    public static bool IsControlMode { get; internal set; }
#if DEBUG
    public static bool IsDebugMode => true;
#else
    public static bool IsDebugMode => false;
#endif

    public static bool IsDebugStopAndGoStressMode { get; }
    internal static bool IsInUnitTest { get; }
    internal static bool IsSingleThreadMode { get; set; }


    static Global()
    {
        IsInUnitTest = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.FullName)
            .Any(n => n.StartsWith("Microsoft.VisualStudio.TestPlatform."))
            ;
        if (!IsDebugMode && IsSingleThreadMode)
            throw new Exception("Running in single thread mode not allowed in production mode.");
    }

    /// <summary> Do nothing </summary>
    public static void NoOp() { }
    public static void Verify(string message, bool condition)
    {
        if (!condition)
        {
            GlobalShortCuts.LogError(message);
            throw new Exception(message);
        }
    }
}

