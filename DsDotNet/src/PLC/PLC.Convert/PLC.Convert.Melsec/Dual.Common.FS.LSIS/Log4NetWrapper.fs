namespace Dual.Common.FS.LSIS
open System
open System.Reflection
open log4net.Config
open System.IO
open log4net

[<AutoOpen>]
module Log4NetWrapper =

    /// Log4net logging level
    type LogLevel =
        | Info = 1
        | Warn = 2
        | Error = 3
        | Debug = 4
    /// logging entry
    and LogEntry(level:LogLevel, message:string) =
        let time = DateTime.Now
        member x.DateTime = time
        member x.Level = level
        member x.Message = message

    /// loggging event
    let logEvent = new Event<LogEntry>()

    let consoleColorChanger(color) =
        let crBackup = System.Console.ForegroundColor
        System.Console.ForegroundColor <- color
        let disposable =
            { new IDisposable with
                member x.Dispose() = System.Console.ForegroundColor <- crBackup }
        disposable


    let private logWithLogger level (logger:log4net.ILog) logfn fmt =
        // http://stackoverflow.com/questions/27004355/wrapping-printf-and-still-take-parameters
        // https://blogs.msdn.microsoft.com/chrsmith/2008/10/01/f-zen-colored-printf/
        Printf.kprintf
            (fun s ->
                if logger = null then
                    System.Console.WriteLine s
                else
                    if level = LogLevel.Error then
                        use changer = consoleColorChanger ConsoleColor.Magenta
                        logfn s
                    else
                        logfn s )
            fmt


    /// Global logger
    let mutable gLogger : log4net.ILog = null
    let SetLogger(logger) = gLogger <- logger

    let logInfo fmt = logWithLogger LogLevel.Info gLogger gLogger.Info fmt
    let logDebug fmt = logWithLogger LogLevel.Debug gLogger gLogger.Debug fmt
    let logWarn fmt = logWithLogger LogLevel.Warn gLogger gLogger.Warn fmt
    let logError fmt = logWithLogger LogLevel.Error gLogger gLogger.Error fmt

    /// failwith logging
    let failwithlog msg =
        logError "%s" msg
        failwith msg

    /// failwith formatted logging : %s 등을 사용할 수 있음
    let failwithlogf fmt =
        //logError fmt
        failwithf fmt

    let raiseExceptionWithLog exn msg =
        logError "%s" msg
        raise exn

    /// Configure log4net
    let configureLog4Net (loggerName:string) log4netConfigFile =
        XmlConfigurator.Configure(new FileInfo(log4netConfigFile)) |> ignore
        gLogger <- LogManager.GetLogger(loggerName);
        gLogger
