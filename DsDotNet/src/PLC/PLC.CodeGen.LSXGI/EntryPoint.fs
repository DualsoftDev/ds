namespace PLC.CodeGen.LSXGI

open Engine.Core
open Engine.Common.FS


module ModuleInitializer =
    let private createVariableWithTypeAndValueOnXgi (typ:System.Type) (name:string) (boxedValue:BoxedObjectHolder) : IVariable =
        verify (Runtime.Target = XGI)
        let v = boxedValue.Object
        createXgiVariable typ name (unbox v) "no comment"
        //match typ.Name with
        //| "Boolean"-> createXgiVariable name "no comment" (v :?> bool)
        //| "Byte"   -> createXgiVariable name "no comment" (v :?> uint8)
        //| "Char"   -> createXgiVariable name "no comment" (v :?> char)
        //| "Double" -> createXgiVariable name "no comment" (v :?> double)
        //| "Int16"  -> createXgiVariable name "no comment" (v :?> int16)
        //| "Int32"  -> createXgiVariable name "no comment" (v :?> int32)
        //| "Int64"  -> createXgiVariable name "no comment" (v :?> int64)
        //| "SByte"  -> createXgiVariable name "no comment" (v :?> int8)
        //| "Single" -> createXgiVariable name "no comment" (v :?> single)
        //| "String" -> createXgiVariable name "no comment" (v :?> string)
        //| "UInt16" -> createXgiVariable name "no comment" (v :?> uint16)
        //| "UInt32" -> createXgiVariable name "no comment" (v :?> uint32)
        //| "UInt64" -> createXgiVariable name "no comment" (v :?> uint64)
        //| _  -> failwith "ERROR"

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

        //fwdCreateVariableWithType <- createVariableWithTypeOnXgi
        //fwdCreateVariableWithTypeAndValue <- createVariableWithTypeAndValueOnXgi
