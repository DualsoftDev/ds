namespace Dual.Common

open System
open System.Threading

//? 필요한가?
// http://www.fssnip.net/5V/title/F-Future-using-lazy-and-a-threading-event
/// F# Future using lazy and a threading event. Supports creating futures from functions or asyncs. Eager evaluation of can be specified.
module LazyFuture =
    let ofAsync eager' (computation:Async<'a>) =
        let result = ref Unchecked.defaultof<_>
        let gate = new ManualResetEventSlim (false)
        
        let init =
            async { let! res = computation |> Async.Catch
                    result := res
                    gate.Set () }

        if eager' then init |> Async.Start

        lazy
            if not (eager') then init |> Async.Start
            gate.Wait ()
            match !result with
            | Choice1Of2 r -> r
            | Choice2Of2 e -> raise e

    let create eager' computation = ofAsync eager' (async { return computation () })





//! F# Lazy 한계
//x * lazy (bla) cannot be transparently used in expressions instead of bla
//x * Results of lazy evaluation are not cached
// * Parameters of lazy functions are evaluated eagerly
// https://ikriv.com/blog/?p=24
// http://www.fssnip.net/y/title/Using-the-lazy-Keyword

open FSharpx.Collections

module private TestMe =
    let v = LazyFuture.create false (fun () -> Thread.Sleep(3000); 10) 
    v.Force () |> printfn "%A"

    let w = Lazy<int>.Create(fun () -> printfn "Evaluating x..."; 10)
    let z = w.Value

    let w = Lazy<int>.Create(fun () -> Thread.Sleep(3000); printfn "Evaluating x..."; 10)
    let z = w.Value

    let lazyX =
        lazy
            let x = 1
            let y = x + 1
            y + 200

    let x = lazyX.Force()


    let showLazyCacheAndTransparency() =
        let lazySideEffect =
            lazy
                ( let temp = 2 + 2
                  printfn "Evaluating %i" temp
                  temp )
          
        printfn "Force value the first time: "
        let actualValue1 = lazySideEffect.Force()
        printfn "Force value the second time: "
        let actualValue2 = lazySideEffect.Force()

        let z = actualValue1 + 3
        let xxx = lazySideEffect.Value + 3
        ()

    let showLazyList() =
        let xs = FSharpx.Collections.LazyList.unfold(fun x -> Some(x, x+1)) 0
        let first20 = LazyList.take 20 xs
        ()

        // Sequence of all integers
        let allIntsSeq = seq { for i = 0 to System.Int32.MaxValue do yield i }
        ()
