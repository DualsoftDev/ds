namespace PLC.CodeGen.LS

open Engine.Core


module ModuleInitializer =
    let Initialize() =
        printfn "PLC.CodeGen.LS Module is being initialized..."

        fwdCreateSymbolInfo <- XGITag.createSymbolInfo

        RuntimeDS.TargetChangedSubject.Subscribe(fun newRuntimeTarget ->
            match newRuntimeTarget with
            | XGI ->
                ()
            | WINDOWS ->
                ()
            | _ ->
                ()
        ) |> ignore
