global using System;
global using System.Linq;
global using System.Collections.Generic;
global using System.Diagnostics;
global using static System.Diagnostics.Debug;
//global using System.Reactive.Linq;
global using log4net;
global using Antlr4.Runtime;
global using Antlr4.Runtime.Tree;
global using Antlr4.Runtime.Misc;
global using Engine.Common;
global using Engine.Core;

global using static Engine.Parser.dsParser;
global using static Engine.Parser.DsParser;
global using static Engine.Core.CoreModule;
global using static Engine.Core.Interface;
global using static Engine.Core.SpitModuleHelper;
global using static Engine.Parser.Global;

namespace Engine.Parser;
public static class Global
{
    public static ILog Logger { get; set; }

    /// <summary> Do nothing </summary>
    public static void NoOp() { }

    public static void LogDebug(object message) => Logger?.Debug(message);
    public static void LogInfo(object message) => Logger?.Info(message);
    public static void LogWarn(object message) => Logger?.Warn(message);
    public static void LogError(object message) => Logger?.Error(message);

    public static void Verify(string message, bool condition)
    {
        if (!condition)
        {
            LogError(message);
            throw new Exception(message);
        }
    }
}

