namespace PLC.CodeGen.LSXGI

open Engine.Core


module ModuleInitializer =
    let Initialize() =
        printfn "PLC.CodeGen.LSXGI Module is being initialized..."

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
