namespace PLC.CodeGen.LSXGI

open Engine.Core
open Engine.Common.FS


module ModuleInitializer =
    let private createXgiVariableWithTypeAndValue (typ:System.Type) (name:string) (boxedValue:BoxedObjectHolder) : IVariable =
        verify (Runtime.Target = XGI)
        let v = boxedValue.Object
        createXgiVariable typ name (unbox v) "no comment"

    let private createXgiVariableWithType (typ:System.Type) (name:string) : IVariable =
        verify (Runtime.Target = XGI)
        let v = typeDefaultValue typ
        createXgiVariableWithTypeAndValue typ name (unbox v)

    let Initialize() =
        printfn "Module is being initialized..."

        fwdCreateSymbolInfo <- XGITag.createSymbolInfo

        Runtime.TargetChangedSubject.Subscribe(fun newRuntimeTarget ->
            match newRuntimeTarget with
            | XGI ->
                fwdCreateVariableWithType <- createXgiVariableWithType
                fwdCreateVariableWithTypeAndValue <- createXgiVariableWithTypeAndValue
            | WINDOWS ->
                fwdCreateVariableWithType <- createWindowsVariableWithType
                fwdCreateVariableWithTypeAndValue <- createWindowsVariableWithTypeAndValue
            | _ ->
                ()
        ) |> ignore
