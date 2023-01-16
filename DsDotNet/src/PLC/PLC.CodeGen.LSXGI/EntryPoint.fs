namespace PLC.CodeGen.LSXGI

open Engine.Core
open Engine.Common.FS


module ModuleInitializer =
    let private createVariableWithTypeAndValueOnXgi (typ:System.Type) (name:string) (boxedValue:BoxedObjectHolder) : IVariable =
        verify (Runtime.Target = XGI)
        let v = boxedValue.Object
        createXgiVariable typ name (unbox v) "no comment"

    let private createVariableWithTypeOnXgi (typ:System.Type) (name:string) : IVariable =
        verify (Runtime.Target = XGI)
        let v = typeDefaultValue typ
        createVariableWithTypeAndValueOnXgi typ name (unbox v)

    let Initialize() =
        printfn "Module is being initialized..."

        fwdCreateSymbolInfo <- XGITag.createSymbolInfo

        Runtime.TargetChangedSubject.Subscribe(fun newRuntimeTarget ->
            match newRuntimeTarget with
            | XGI ->
                fwdCreateVariableWithType <- createVariableWithTypeOnXgi
                fwdCreateVariableWithTypeAndValue <- createVariableWithTypeAndValueOnXgi
            | WINDOWS ->
                fwdCreateVariableWithType <- createVariableWithTypeOnWindows
                fwdCreateVariableWithTypeAndValue <- createVariableWithTypeAndValueOnWindows
            | _ ->
                ()
        ) |> ignore
