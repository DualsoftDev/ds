namespace Dual.Common.Core.FS

open System

[<AutoOpen>]
module DisposableBuilderModule =

    // http://www.fssnip.net/2s/title/Disposable-computation-builder
    /// Disposable computation expression
    type DisposableBuilder() =

        member x.Delay(f : unit -> IDisposable) =
            { new IDisposable with
                  member x.Dispose() = f().Dispose() }

        member x.Bind(d1 : IDisposable, f : unit -> IDisposable) =
            let d2 = f()
            { new IDisposable with
                  member x.Dispose() =
                      d1.Dispose()
                      d2.Dispose() }

        member x.Return(()) = x.Zero()
        member x.Zero() =
            { new IDisposable with
                  member x.Dispose() = () }

    /// Disposable computation expression builder.  IDisposable 객체 반환
    ///
    /// - e.g: disposable { Console.ForegroundColor <- backupColor }
    let disposable = DisposableBuilder()




module private DisposableBuilderModuleTestSample =

    let private rundemo1() =
        // Creates disposable that resets console color when disposed
        let resetColor() =
            let clr = Console.ForegroundColor
            disposable { Console.ForegroundColor <- clr }

        // Prints 'doing work' in red and resets color back
        let demo1() =
            use unwind = resetColor()
            Console.ForegroundColor <- ConsoleColor.Red
            printfn "doing work"

        demo1() // Prints 'doing work' in red
        printfn "done" // Prints 'done' in original color


    /// 함수 끝단에 수행해야 할 목록 지정에 편리함
    let private rundemo2() =
        // Create two IDisposable objects that do some cleanup
        let dispose = { new IDisposable with member x.Dispose() = printfn "\tdisposing.." }
        let cleanup1 = disposable { printfn "\tcleanup #1" }
        let cleanup2 = disposable { printfn "\tcleanup #1" }

        let demo2() =
            // Dispose of both 'cleanup1' and 'cleanup2' when the
            // method finishes. This is useful for example when working
            // with IObservable (to dispose of event registrations)
            use d =
                disposable {
                    printfn "cleanup"
                    do! dispose
                    do! cleanup1
                    do! cleanup2
                }
            printfn "foo"
        demo2()
