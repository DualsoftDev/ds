namespace Engine.Cpu

open Engine.Core
open System
open Engine.Parser.FS

[<AutoOpen>]
module RunTime =
    
    type DsCPU(text:string, statements:Statement seq) = 
        let init() =
            TypedValueSubject
                .Subscribe(fun evt -> evt.NotifyValueChanged())
            |> ignore

        do init()

        let storages = Storages()
        let statements = 
            if text <> ""
            then text |> parseCode storages 
            else statements |> Seq.toList
        
        member x.Update() = 
            for statement in statements do
            statement.Do()
