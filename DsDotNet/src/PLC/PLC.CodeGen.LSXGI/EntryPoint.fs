namespace PLC.CodeGen.LSXGI

open Engine.Core
open Engine.Common.FS


module ModuleInitializer =
    let private createVariableWithTypeOnXgi (name:string) (typ:System.Type) : IVariable =
        verify (RuntimeTarget = XGI)
        match typ.Name with
        | "Single" -> createXgiVariable name "no comment" 0.0f
        | "Double" -> createXgiVariable name "no comment" 0.0
        | "SByte"  -> createXgiVariable name "no comment" 0y
        | "Byte"   -> createXgiVariable name "no comment" 0uy
        | "Int16"  -> createXgiVariable name "no comment" 0s
        | "UInt16" -> createXgiVariable name "no comment" 0us
        | "Int32"  -> createXgiVariable name "no comment" 0
        | "UInt32" -> createXgiVariable name "no comment" 0u
        | "Int64"  -> createXgiVariable name "no comment" 0L
        | "UInt64" -> createXgiVariable name "no comment" 0UL
        | "Boolean"-> createXgiVariable name "no comment" false
        | "String" -> createXgiVariable name "no comment" ""
        | "Char"   -> createXgiVariable name "no comment" ' '
        | _  -> failwith "ERROR"

    let private createVariableWithTypeAndValueOnXgi (name:string) (typ:System.Type) (boxedValue:BoxedObjectHolder) : IVariable =
        verify (RuntimeTarget = XGI)
        let v = boxedValue.Object
        match typ.Name with
        | "Single" -> createXgiVariable name "no comment" (v :?> single)
        | "Double" -> createXgiVariable name "no comment" (v :?> double)
        | "SByte"  -> createXgiVariable name "no comment" (v :?> int8)
        | "Byte"   -> createXgiVariable name "no comment" (v :?> uint8)
        | "Int16"  -> createXgiVariable name "no comment" (v :?> int16)
        | "UInt16" -> createXgiVariable name "no comment" (v :?> uint16)
        | "Int32"  -> createXgiVariable name "no comment" (v :?> int32)
        | "UInt32" -> createXgiVariable name "no comment" (v :?> uint32)
        | "Int64"  -> createXgiVariable name "no comment" (v :?> int64)
        | "UInt64" -> createXgiVariable name "no comment" (v :?> uint64)
        | "Boolean"-> createXgiVariable name "no comment" (v :?> bool)
        | "String" -> createXgiVariable name "no comment" (v :?> string)
        | "Char"   -> createXgiVariable name "no comment" (v :?> char)
        | _  -> failwith "ERROR"

    let Initialize() =
        printfn "Module is being initialized..."

        fwdCreateSymbol <- XGITag.createSymbol



        fwdCreateVariableWithType <- createVariableWithTypeOnXgi
        fwdCreateVariableWithTypeAndValue <- createVariableWithTypeAndValueOnXgi
