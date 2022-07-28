global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Reactive.Subjects;
global using System.Reactive.Linq;
global using System.Reactive.Disposables;
global using System.Diagnostics;

global using log4net;
global using TagDic = System.Collections.Generic.Dictionary<string, Engine.Core.Tag>;
global using Engine.Common;

namespace Engine.Core;

public static class Global
{
    public static ILog Logger { get; set; }
    public static Subject<BitChange> BitChangedSubject { get; } = new();

    public static Subject<OpcTagChange> TagChangeToOpcServerSubject { get; } = new();
    public static Subject<OpcTagChange> TagChangeFromOpcServerSubject { get; } = new();

    public static bool IsInUnitTest { get; }
    static Global()
    {
        IsInUnitTest = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.FullName)
            .Any(n => n.StartsWith("Microsoft.VisualStudio.TestPlatform."))
            ;
    }
}

