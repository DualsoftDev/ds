// Learn more about F# at http://fsharp.org

open System
open Engine.Core


[<EntryPoint>]
let main argv =

    let init() =
        TypedValueSubject
            .Subscribe(fun evt ->
                match evt with
                |Event (name, value) -> Console.WriteLine $"Value changed: [{name}] = {value}"
            )
        |> ignore

    0 // return an integer exit code
   