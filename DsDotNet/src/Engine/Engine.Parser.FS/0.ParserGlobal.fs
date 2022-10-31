namespace Engine.Parser.FS

open System
open System.Linq
open System.Collections.Generic
open System.Diagnostics
open type System.Diagnostics.Debug
//open System.Reactive.Linq
open log4net
open Antlr4.Runtime
open Antlr4.Runtime.Tree
open Antlr4.Runtime.Misc
open Engine.Common
open Engine.Core

open type Engine.Parser.dsParser
open type Engine.Parser.DsParser
//open type Engine.Core.CoreModule
//open type Engine.Core.Interface
//open type Engine.Core.SpitModuleHelper
open type Engine.Parser.Global

//type Global = {
//    Logger:ILog

//    /// <summary> Do nothing </summary>
//    public static void NoOp() { }

//    public static void LogDebug(object message) => Logger?.Debug(message)
//    public static void LogInfo(object message) => Logger?.Info(message)
//    public static void LogWarn(object message) => Logger?.Warn(message)
//    public static void LogError(object message) => Logger?.Error(message)

//    public static void Verify(string message, bool condition)
//    {
//        if (!condition)
//        {
//            LogError(message)
//            throw new Exception(message)
//        }
//    }
//}

