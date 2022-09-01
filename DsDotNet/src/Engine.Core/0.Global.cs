global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Reactive.Subjects;
global using System.Reactive.Linq;
global using System.Reactive.Disposables;
global using System.Diagnostics;

global using log4net;
global using BitDic = System.Collections.Generic.Dictionary<string, Engine.Core.IBit>;
global using TagDic = System.Collections.Generic.Dictionary<string, Engine.Core.Tag>;
global using Engine.Common;

global using static Engine.Core.GlobalShortCuts;

namespace Engine.Core;

public static class GlobalShortCuts
{
    public static void LogDebug(object message) => Global.Logger.Debug(message);
    public static void LogInfo (object message) => Global.Logger.Info(message);
    public static void LogWarn (object message) => Global.Logger.Warn(message);
    public static void LogError(object message) => Global.Logger.Error(message);
    public static void DAssert(bool condition) => System.Diagnostics.Debug.Assert(condition);
}

public delegate void ExceptionHandler(Exception ex);

public static class Global
{
    public static ILog Logger { get; set; }
    public static Subject<BitChange> RawBitChangedSubject { get; } = new();
    public static IObservable<BitChange> BitChangedSubject => RawBitChangedSubject.Select(x => x);
    public static IObservable<BitChange> PortChangedSubject => RawBitChangedSubject.Where(bc => bc.Bit is PortInfo);
    public static IObservable<Tag> TagChangedSubject => RawBitChangedSubject.Where(bc => bc.Bit is Tag).Select(bc => bc.Bit as Tag);

    public static Subject<OpcTagChange> TagChangeToOpcServerSubject { get; } = new();
    public static Subject<OpcTagChange> TagChangeFromOpcServerSubject { get; } = new();

    /// <summary> Segment 상태 변경 공지.  가상부모 segment 는 제외 </summary>
    public static Subject<SegmentStatusChange> SegmentStatusChangedSubject { get; } = new();
    public static Subject<ChildStatusChange> ChildStatusChangedSubject { get; } = new();
    public static IObservable<long> TickSeconds => Observable.Interval(TimeSpan.FromSeconds(1));

    /// <summary>Engine running mode: if false, just simulation mode</summary>
    public static bool IsControlMode { get; internal set; }
    internal static bool IsInUnitTest { get; }
    internal static bool IsSingleThreadMode { get; set; }

    public static Model Model { get; set; }

    static Global()
    {
        IsInUnitTest = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.FullName)
            .Any(n => n.StartsWith("Microsoft.VisualStudio.TestPlatform."))
            ;
#if !DEBUG
        if (IsSingleThreadMode)
            throw new Exception("Running in single thread mode not allowed in production mode.");
#endif
    }

    /// <summary> Do nothing </summary>
    public static void NoOp() {}
    public static void Verify(string message, bool condition)
    {
        if (!condition)
        {
            LogError(message);
            throw new Exception(message);
        }
    }
}

