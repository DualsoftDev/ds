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
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Engine.Core;

public static class Global
{
    public static ILog Logger { get; set; }
    public static Subject<BitChange> RawBitChangedSubject { get; } = new();
    public static IObservable<BitChange> BitChangedSubject { get; set; } = RawBitChangedSubject.Select(x => x);

    public static Subject<OpcTagChange> TagChangeToOpcServerSubject { get; } = new();
    public static Subject<OpcTagChange> TagChangeFromOpcServerSubject { get; } = new();


    public static bool IsSupportParallel { get; set; }
    public static bool IsInUnitTest { get; }
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

