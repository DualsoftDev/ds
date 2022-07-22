global using System;
global using System.Collections.Generic;
global using System.Reactive.Subjects;
global using System.Diagnostics;
global using System.Linq;

global using log4net;
global using TagDic = System.Collections.Generic.Dictionary<string, Engine.Core.Tag>;
global using Engine.Common;

namespace Engine.Core;

public static class Global
{
    public static ILog Logger { get; set; }
    public static Subject<BitChange> BitChangedSubject { get; } = new();

    public static Subject<OpcTagChange> OpcTagChangedSubject { get; } = new();
}

