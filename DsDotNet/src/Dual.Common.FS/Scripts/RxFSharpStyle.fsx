
#r "nuget: FSharp.Control.Reactive"
open FSharp.Control.Reactive
open FSharp.Control.Reactive.Builders

/// mnemonic
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
