

#I @"..\..\bin"
#I @"..\..\bin\netcoreapp3.1"

//#r "Dual.Common.FS.dll"

#r "nuget: FSharpPlus"
#r "nuget: Log4Net"

open FSharpPlus
//open Dual.Common
open System
open System.IO
open System.Diagnostics
open log4net
open log4net.Config
open log4net.Core


//let getLogLevel (logger:ILog) =
//    [
//        LogLevel.Error
//        LogLevel.Warn
//        LogLevel.Info
//        LogLevel.Debug
//    ] |> logger.IsEn
            
let logWithLogger (logger:log4net.ILog) logfn fmt =
    //let logfn (str:string) = ()
    // http://stackoverflow.com/questions/27004355/wrapping-printf-and-still-take-parameters
    // https://blogs.msdn.microsoft.com/chrsmith/2008/10/01/f-zen-colored-printf/
    Printf.kprintf
        (fun s ->
            //Trace.WriteLine s
            if logger = null then
                //Console.WriteLine s
                ()
            else
                    logfn s) fmt


/// Global logger
let mutable gLogger : log4net.ILog = null
let SetLogger(logger) = gLogger <- logger

let logInfo  fmt = logWithLogger gLogger gLogger.Info  fmt
let logDebug fmt = logWithLogger gLogger gLogger.Debug fmt
let logWarn  fmt = logWithLogger gLogger gLogger.Warn  fmt
let logError fmt = logWithLogger gLogger gLogger.Error fmt


let logger = LogManager.GetLogger("PLCDriverTest")
gLogger <- logger
gLogger.Error "Hello"

LogManager.GetRepository().Threshold = Level.Info;
[1..100000] |> iter (logError "%d")
