namespace PLC.CodeGen.LSXGI

open Engine.Core
open Engine.Common.FS


module ModuleInitializer =
    let private createVariableWithTypeOnXgi (name:string) (typ:System.Type) : IVariable =
        verify (Runtime.Target = XGI)
        match typ.Name with
        | "Boolean"-> createXgiVariable name "no comment" false
        | "Byte"   -> createXgiVariable name "no comment" 0uy
        | "Char"   -> createXgiVariable name "no comment" ' '
        | "Double" -> createXgiVariable name "no comment" 0.0
        | "Int16"  -> createXgiVariable name "no comment" 0s
        | "Int32"  -> createXgiVariable name "no comment" 0
        | "Int64"  -> createXgiVariable name "no comment" 0L
        | "SByte"  -> createXgiVariable name "no comment" 0y
        | "Single" -> createXgiVariable name "no comment" 0.0f
        | "String" -> createXgiVariable name "no comment" ""
        | "UInt16" -> createXgiVariable name "no comment" 0us
        | "UInt32" -> createXgiVariable name "no comment" 0u
        | "UInt64" -> createXgiVariable name "no comment" 0UL
        | _  -> failwith "ERROR"

    let private createVariableWithTypeAndValueOnXgi (name:string) (typ:System.Type) (boxedValue:BoxedObjectHolder) : IVariable =
        verify (Runtime.Target = XGI)
        let v = boxedValue.Object
        match typ.Name with
        | "Boolean"-> createXgiVariable name "no comment" (v :?> bool)
        | "Byte"   -> createXgiVariable name "no comment" (v :?> uint8)
        | "Char"   -> createXgiVariable name "no comment" (v :?> char)
        | "Double" -> createXgiVariable name "no comment" (v :?> double)
        | "Int16"  -> createXgiVariable name "no comment" (v :?> int16)
        | "Int32"  -> createXgiVariable name "no comment" (v :?> int32)
        | "Int64"  -> createXgiVariable name "no comment" (v :?> int64)
        | "SByte"  -> createXgiVariable name "no comment" (v :?> int8)
        | "Single" -> createXgiVariable name "no comment" (v :?> single)
        | "String" -> createXgiVariable name "no comment" (v :?> string)
        | "UInt16" -> createXgiVariable name "no comment" (v :?> uint16)
        | "UInt32" -> createXgiVariable name "no comment" (v :?> uint32)
        | "UInt64" -> createXgiVariable name "no comment" (v :?> uint64)
        | _  -> failwith "ERROR"

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
