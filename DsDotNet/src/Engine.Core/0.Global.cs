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

namespace Engine.Core;


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

    public static bool IsInUnitTest { get; }
    public static bool IsSimulationMode { get; internal set; } = true;
    static Global()
    {
        IsInUnitTest = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.FullName)
            .Any(n => n.StartsWith("Microsoft.VisualStudio.TestPlatform."))
            ;
    }

    /// <summary> Do nothing </summary>
    public static void NoOp() {}
}

