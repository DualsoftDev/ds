namespace PLC.CodeGen.LSXGI

open Engine.Core
open Engine.Common.FS


module ModuleInitializer =
    let private createXgiVariableWithTypeAndValue (name:string) (boxedValue:BoxedObjectHolder) : IVariable =
        verify (Runtime.Target = XGI)
        let v = boxedValue.Object
        let noComment = ""
        createXgiVariable name v noComment

    let private createXgiVariableWithType (typ:System.Type) (name:string): IVariable =
        verify (Runtime.Target = XGI)
        let boxed:BoxedObjectHolder = { Object = typeDefaultValue typ }
        createXgiVariableWithTypeAndValue name boxed

    let Initialize() =
        printfn "Module is being initialized..."

        fwdCreateSymbolInfo <- XGITag.createSymbolInfo

        Runtime.TargetChangedSubject.Subscribe(fun newRuntimeTarget ->
            match newRuntimeTarget with
            | XGI ->
                fwdCreateVariableWithTypeAndValue <- createXgiVariableWithTypeAndValue
            | WINDOWS ->
                fwdCreateVariableWithTypeAndValue <- createVariable
            | _ ->
                ()
        ) |> ignore
