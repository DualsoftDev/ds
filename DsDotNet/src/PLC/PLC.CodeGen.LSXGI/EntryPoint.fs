namespace PLC.CodeGen.LSXGI

open Engine.Core
open Engine.Common.FS


module ModuleInitializer =
    let private createXgiVariableHelper (name:string) (boxedValue:BoxedObjectHolder) : IVariable =
        verify (Runtime.Target = XGI)
        let v = boxedValue.Object
        let noComment = ""
        createXgiVariable name v noComment

    let Initialize() =
        printfn "Module is being initialized..."

        fwdCreateSymbolInfo <- XGITag.createSymbolInfo

        Runtime.TargetChangedSubject.Subscribe(fun newRuntimeTarget ->
            match newRuntimeTarget with
            | XGI ->
                fwdCreateVariable <- createXgiVariableHelper
            | WINDOWS ->
                fwdCreateVariable <- createVariable
            | _ ->
                ()
        ) |> ignore
