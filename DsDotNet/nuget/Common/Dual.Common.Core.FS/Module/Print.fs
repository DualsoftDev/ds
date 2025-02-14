namespace Dual.Common.Core.FS

open System
open System.Diagnostics
open type Dual.Common.Base.CS.DcLogger
open type Dual.Common.Base.CS.DcApp

[<AutoOpen>]
module PrintModule =
    let private traceWrite     msg = if EnableTrace then Trace.Write msg
    let private traceWriteLine msg = if EnableTrace then Trace.WriteLine msg
    let private debugWrite     msg = if IsDebugVersion then Debug.Write msg
    let private debugWriteLine msg = if IsDebugVersion then Debug.WriteLine msg
    /// Trace.Write 는 release version 에서도 동작함.   Release version 에서 끄려면 debugfn 을 사용할 것.
    let tracef  fmt = Printf.kprintf traceWrite fmt
    /// Trace.WriteLine 는 release version 에서도 동작함.   Release version 에서 끄려면 debugfn 을 사용할 것.
    let tracefn fmt = Printf.kprintf traceWriteLine fmt
    let trace = tracefn
    /// 모든 설정과 상관없이 무조건 trace 남김
    let forceTrace fmt = Printf.kprintf Trace.WriteLine fmt

    /// Debug.Write 는 release version 에서는 동작하지 않음.
    let debugf  fmt = Printf.kprintf debugWrite fmt
    let debugfn fmt = Printf.kprintf debugWriteLine fmt

    let private cprintfWith endl c fmt =
        // http://stackoverflow.com/questions/27004355/wrapping-printf-and-still-take-parameters
        // https://blogs.msdn.microsoft.com/chrsmith/2008/10/01/f-zen-colored-printf/
        Printf.kprintf
            (fun s ->
                let old = System.Console.ForegroundColor
                try
                    System.Console.ForegroundColor <- c;
                    System.Console.Write (s + endl)
                finally
                    System.Console.ForegroundColor <- old)
            fmt

    let cprintf c          fmt = cprintfWith "" c fmt
    let cprintfn c         fmt = cprintfWith "\n" c fmt


    let printfBlack        fmt = cprintf  ConsoleColor.Black fmt
    let printfnBlack       fmt = cprintfn ConsoleColor.Black fmt
    let printfDarkBlue     fmt = cprintf  ConsoleColor.DarkBlue fmt
    let printfnDarkBlue    fmt = cprintfn ConsoleColor.DarkBlue fmt
    let printfDarkGreen    fmt = cprintf  ConsoleColor.DarkGreen fmt
    let printfnDarkGreen   fmt = cprintfn ConsoleColor.DarkGreen fmt
    let printfDarkCyan     fmt = cprintf  ConsoleColor.DarkCyan fmt
    let printfnDarkCyan    fmt = cprintfn ConsoleColor.DarkCyan fmt
    let printfDarkRed      fmt = cprintf  ConsoleColor.DarkRed fmt
    let printfnDarkRed     fmt = cprintfn ConsoleColor.DarkRed fmt
    let printfDarkMagenta  fmt = cprintf  ConsoleColor.DarkMagenta fmt
    let printfnDarkMagenta fmt = cprintfn ConsoleColor.DarkMagenta fmt
    let printfDarkYellow   fmt = cprintf  ConsoleColor.DarkYellow fmt
    let printfnDarkYellow  fmt = cprintfn ConsoleColor.DarkYellow fmt
    let printfGray         fmt = cprintf  ConsoleColor.Gray fmt
    let printfnGray        fmt = cprintfn ConsoleColor.Gray fmt
    let printfDarkGray     fmt = cprintf  ConsoleColor.DarkGray fmt
    let printfnDarkGray    fmt = cprintfn ConsoleColor.DarkGray fmt
    let printfBlue         fmt = cprintf  ConsoleColor.Blue fmt
    let printfnBlue        fmt = cprintfn ConsoleColor.Blue fmt
    let printfGreen        fmt = cprintf  ConsoleColor.Green fmt
    let printfnGreen       fmt = cprintfn ConsoleColor.Green fmt
    let printfCyan         fmt = cprintf  ConsoleColor.Cyan fmt
    let printfnCyan        fmt = cprintfn ConsoleColor.Cyan fmt
    let printfRed          fmt = cprintf  ConsoleColor.Red fmt
    let printfnRed         fmt = cprintfn ConsoleColor.Red fmt
    let printfMagenta      fmt = cprintf  ConsoleColor.Magenta fmt
    let printfnMagenta     fmt = cprintfn ConsoleColor.Magenta fmt
    let printfYellow       fmt = cprintf  ConsoleColor.Yellow fmt
    let printfnYellow      fmt = cprintfn ConsoleColor.Yellow fmt
    let printfWhite        fmt = cprintf  ConsoleColor.White fmt
    let printfnWhite       fmt = cprintfn ConsoleColor.White fmt




    let consoleColorChanger(color) =
        let crBackup = System.Console.ForegroundColor
        System.Console.ForegroundColor <- color
        let disposable =
            { new IDisposable with
                member x.Dispose() = System.Console.ForegroundColor <- crBackup }
        disposable




    //let printfnd fmt = printfn fmt

    (*
        * http://stackoverflow.com/questions/11559440/how-to-manage-debug-printing-in-f
        * http://www.fssnip.net/M
        * Akka actor 와 함께 사용하면, release version 에서 메시지가 제대로 동작하지 않음.
        *)

    // this has the same type as printf, but it doesn't print anything
    let private fakePrintf fmt =
        fprintf System.IO.StreamWriter.Null fmt


    #if DEBUG
    let printfnd fmt =
        printfn fmt
    let printfd fmt =
        printf fmt
    #else
    let printfnd fmt =
        fakePrintf fmt
    let printfd fmt =
        fakePrintf fmt
    #endif


