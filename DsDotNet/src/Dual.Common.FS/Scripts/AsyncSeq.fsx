#r "nuget: FSharpx.Async" 

open FSharp.Control
open FSharpx.Control

let asyncS = asyncSeq {
    do! Async.Sleep(1000)
    yield 1
    do! Async.Sleep(1000)
    yield 2
    yield! asyncSeq {
        yield 3
        yield 4
    }
}

async {
    for n in asyncS do
        printfn "%d" n
} |> Async.RunSynchronously


AsyncSeq.ofSeq