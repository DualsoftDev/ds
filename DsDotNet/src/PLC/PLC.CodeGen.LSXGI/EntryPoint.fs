namespace PLC.CodeGen.LSXGI

open Engine.Core


module ModuleInitializer =
    let Initialize() =
        printfn "Module is being initialized..."

        fwdCreateSymbolInfo <- XGITag.createSymbolInfo

        Runtime.TargetChangedSubject.Subscribe(fun newRuntimeTarget ->
            match newRuntimeTarget with
            | XGI ->
                ()
            | WINDOWS ->
                ()
            | _ ->
                ()
        ) |> ignore
