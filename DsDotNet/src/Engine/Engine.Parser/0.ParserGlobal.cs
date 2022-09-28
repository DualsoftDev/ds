global using System;
global using System.Linq;
global using System.Collections.Generic;
global using System.Diagnostics;
global using static System.Diagnostics.Debug;
//global using System.Reactive.Linq;
global using log4net;
global using Antlr4.Runtime;
global using Antlr4.Runtime.Tree;
global using Engine.Core;

global using static Engine.Parser.dsParser;
global using static Engine.Parser.DsParser;
global using static Engine.Core.CoreModule;
global using static Engine.Core.Interface;

namespace Engine.Parser;
public static class Global
{
    public static ILog Logger { get; set; }
}