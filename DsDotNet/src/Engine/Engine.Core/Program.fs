// Learn more about F# at http://fsharp.org
open Engine.Core
open System

[<EntryPoint>]
let main argv =
    printfn "Hello World from F#!"
    let model = Exercise.createSample()
    0 // return an integer exit code