
#r "nuget: System.Reactive" 

open System
open System.Reactive
open System.Reactive.Linq
open System.Linq

let dump x = printfn "%A" x
let dumpExn (exn:exn) = printfn "Caught Exceptino: %O" exn

let subscription =
    Observable.Interval(TimeSpan.FromSeconds(1.0)).Subscribe(dump)

Observable
    .Range(1, 5)
    .Select(fun x -> x/(x - 3))
    .Subscribe(dump, dumpExn) // do something with the exception

let xx =
    Observable
        .Timer(DateTimeOffset.Now,TimeSpan.FromSeconds(1.0))
        .Select(fun _ -> DateTimeOffset.Now)
        .TakeUntil(DateTimeOffset.Now.AddSeconds(5.0))
        .Subscribe(printfn "TakeUntil(time):%A")


let yy =
    Observable
        .Timer(DateTimeOffset.Now,TimeSpan.FromSeconds(1.0))
        |> Observable.map (fun _ -> DateTimeOffset.Now)
        |> fun x -> x.TakeUntil(DateTimeOffset.Now.AddSeconds(5.0))
        |> fun x -> x.Subscribe(printfn "TakeUntil(time):%A")

// ------------------------ FSharp.Control.Reactive

#r "nuget: FSharp.Control.Reactive"
open FSharp.Control.Reactive
open FSharp.Control.Reactive.Builders

module FObservable = FSharp.Control.Reactive.Observable

let rec generate x =
    observe {
        yield x
        if x < 100000 then
            yield! generate (x + 1) }

let onNext x = printfn "%A" x
let onError (ex:exn) = printfn "ERROR: %O" ex
//generate 5
FObservable.range 1 5
|> FObservable.map float
|> FObservable.map (fun x ->
    failwith "ERROR"
    x / (x-3.0))
|> FObservable.subscribeWithCallbacks onNext onError ignore
|> ignore
