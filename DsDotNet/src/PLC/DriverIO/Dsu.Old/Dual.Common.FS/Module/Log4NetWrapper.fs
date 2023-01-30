// https://jakubwajs.wordpress.com/2019/11/28/logging-with-log4net-in-net-core-3-0-console-app/
// Logger 를 argument 로 passing 하는 방법
// : https://stackoverflow.com/questions/25448775/pass-curried-kprintf-as-function-argument

// 실제 print 하지 않는 logging 을 효율적으로 하는 방법
// https://stackoverflow.com/questions/11559440/how-to-manage-debug-printing-in-f

// App.config 샘플
// https://gist.github.com/dpan/9229147
// https://jakubwajs.wordpress.com/2019/11/28/logging-with-log4net-in-net-core-3-0-console-app/
// 

namespace Old.Dual.Common

open System
open log4net.Config
open log4net.Core
open System.IO
open log4net
open System.Diagnostics

[<AutoOpen>]
module Log4NetWrapper =

    /// logging entry
    type LogEntry(level:Level, message:string) =
        let time = DateTime.Now
        member x.DateTime = time
        member x.Level    = level
        member x.Message  = message

    /// loggging event
    let logEvent = new Event<LogEntry>()

    let consoleColorChanger(color) =
        let crBackup = Console.ForegroundColor
        Console.ForegroundColor <- color
        let disposable =
            { new IDisposable with
                member x.Dispose() = Console.ForegroundColor <- crBackup }
        disposable


    let private logWithLogger level (logger:log4net.ILog) logfn fmt =
        // http://stackoverflow.com/questions/27004355/wrapping-printf-and-still-take-parameters
        // https://blogs.msdn.microsoft.com/chrsmith/2008/10/01/f-zen-colored-printf/
        Printf.kprintf
            (fun s ->
                Trace.WriteLine s
                if logger = null then
                    Console.WriteLine s
                logfn s )
            fmt


    /// Global logger
    let mutable gLogger : log4net.ILog = null
    let SetLogger(logger) = gLogger <- logger

    let private nullLog (s:string) = ()
    let private debug() = if gLogger = null then nullLog else gLogger.Debug
    let private info()  = if gLogger = null then nullLog else gLogger.Info
    let private warn()  = if gLogger = null then nullLog else gLogger.Warn
    let private error() = if gLogger = null then nullLog else gLogger.Error
    let private fatal() = if gLogger = null then nullLog else gLogger.Fatal

    let logInfo  fmt = logWithLogger Level.Info  gLogger (info())  fmt
    let logDebug fmt = logWithLogger Level.Debug gLogger (debug()) fmt
    let logWarn  fmt = logWithLogger Level.Warn  gLogger (warn())  fmt
    let logError fmt = logWithLogger Level.Error gLogger (error()) fmt
    let logFatal fmt = logWithLogger Level.Error gLogger (fatal()) fmt
    //let logCritical fmt = logWithLogger Level.Critical gLogger gLogger.Error fmt

    /// failwith logging
    let failwithlog msg =
        logError "%s" msg
        failwith msg

    // see Prelude.failwithf

    /// failwith formatted logging : %s 등을 사용할 수 있음
    let failwithlogf format =
        Printf.ksprintf failwithlog format

    let raiseExceptionWithLog exn msg =
        logError "%s" msg
        raise exn

    /// Configure log4net
    let configureLog4Net (loggerName:string) log4netConfigFile =
        XmlConfigurator.Configure(new FileInfo(log4netConfigFile)) |> ignore
        gLogger <- LogManager.GetLogger(loggerName);
        gLogger
