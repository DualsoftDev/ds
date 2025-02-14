// https://jakubwajs.wordpress.com/2019/11/28/logging-with-log4net-in-net-core-3-0-console-app/
// Logger 를 argument 로 passing 하는 방법
// : https://stackoverflow.com/questions/25448775/pass-curried-kprintf-as-function-argument

// 실제 print 하지 않는 logging 을 효율적으로 하는 방법
// https://stackoverflow.com/questions/11559440/how-to-manage-debug-printing-in-f

// App.config 샘플
// https://gist.github.com/dpan/9229147
// https://jakubwajs.wordpress.com/2019/11/28/logging-with-log4net-in-net-core-3-0-console-app/
//

namespace Dual.Common.Core.FS

open System
open System.Linq
open System.IO
open System.Diagnostics
open System.Reactive.Disposables
open log4net
open log4net.Config
open log4net.Core
open Dual.Common.Base.CS
open type Dual.Common.Base.CS.DcLogger


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

    [<Obsolete("DcLogger.EnableTrace 로 대체해야 함.  (Dual.Common.Base.CS)")>]
    let mutable logWithTrace = false

    let mutable private logBuffer:CircularBuffer<string> option = None
    /// 양수로 설정시, 종전 갯수내의 log 에서 중복되는 메시지는 출력하지 않는다.
    let SetSkipDuplicateLogWindowSize(windowSize:int) =
        if windowSize > 0 then
            logBuffer <- Some <| CircularBuffer<string>(windowSize)
        else
            logBuffer <- None

    /// buffer 와 중복되는 message 이면 false 반환해서 log 출력을 막는다.
    let private enqueLogOnDemand (log: string) (buffer:CircularBuffer<string>) =
        if buffer.Contains(log) then
            false
        else
            buffer.Enqueue(log) |> ignore
            true

    let private logWithLogger level (logger:log4net.ILog) logfn fmt =
        // http://stackoverflow.com/questions/27004355/wrapping-printf-and-still-take-parameters
        // https://blogs.msdn.microsoft.com/chrsmith/2008/10/01/f-zen-colored-printf/
        Printf.kprintf
            (fun s ->
                let goOn = logBuffer |> Option.map(enqueLogOnDemand s) |> Option.defaultValue true
                if goOn then
                    if EnableTrace then
                        Trace.WriteLine ("= " + s)
                    if logger = null then
                        Console.WriteLine s
                    logfn s )
            fmt


    [<Obsolete("DcLogger.Logger 로 대체 (Dual.Common.Base.CS")>]
    /// Global logger
    let mutable gLogger : log4net.ILog = null
    let SetLogger(logger) = Logger <- logger

    let private nullLog (s:string) = ()
    let private debug() = if Logger = null then nullLog else Logger.Debug
    let private info()  = if Logger = null then nullLog else Logger.Info
    let private warn()  = if Logger = null then nullLog else Logger.Warn
    let private error() = if Logger = null then nullLog else Logger.Error
    let private fatal() = if Logger = null then nullLog else Logger.Fatal

    let logInfo  fmt = logWithLogger Level.Info  Logger (info())  fmt
    let logDebug fmt = logWithLogger Level.Debug Logger (debug()) fmt
    let logWarn  fmt = logWithLogger Level.Warn  Logger (warn())  fmt
    let logError fmt = logWithLogger Level.Error Logger (error()) fmt
    let logFatal fmt = logWithLogger Level.Error Logger (fatal()) fmt
    //let logCritical fmt = logWithLogger Level.Critical gLogger gLogger.Error fmt


    let logErrorWithStackTrace msg =
        let st = StackTrace().ToString()
        logError $"{msg}{Environment.NewLine}{st}"

    /// failwith logging

    let failwithlog msg =
        logError "%s" msg
        failwith msg


    let raisewithlog (ex:Exception): unit =
        logError $"{ex}"
        raise ex

    /// failwith stack trace logging
    let failwithstack msg =
        let st = StackTrace().ToString()
        logError $"{msg}{Environment.NewLine}{st}"
        Trace.WriteLine $"{msg}{Environment.NewLine}{st}"
        failwith msg

    let verify x = if not x then failwithlog "VERIFICATION ERROR"
    let verifyNonNull x = if isItNull(x) then failwithlog "VERIFICATION ERROR"
    /// Verifies a condition and throws an exception if not met.
    let verifyM (message: string) condition =
        if not condition then
            failwith message

    // see Prelude.failwithf

    /// failwith formatted logging : %s 등을 사용할 수 있음
    let failwithlogf format =
        Printf.ksprintf failwithlog format

    /// failwith stack trace with formatted logging : %s 등을 사용할 수 있음
    let failwithstackf format =
        Printf.ksprintf failwithstack format

    let failWithLog = failwithlog
    let raiseWithLog = raisewithlog
    let failWithLogF = failwithlogf


    /// Configure log4net
    let configureLog4Net (loggerName:string) log4netConfigFile =
        XmlConfigurator.Configure(new FileInfo(log4netConfigFile)) |> ignore
        Logger <- LogManager.GetLogger(loggerName);
        Logger
