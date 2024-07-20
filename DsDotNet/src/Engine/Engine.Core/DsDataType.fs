// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Core

open Dual.Common.Core.FS
open System


[<AutoOpen>]
module DsDataType =
    //data 타입 지원 항목 : 알파벳 순 정렬 (Alt+Shift+L, Alt+Shift+S)
    let [<Literal>] BOOL    = "Boolean"
    let [<Literal>] CHAR    = "Char"
    let [<Literal>] FLOAT32 = "Single"
    let [<Literal>] FLOAT64 = "Double"
    let [<Literal>] INT16   = "Int16"
    let [<Literal>] INT32   = "Int32"
    let [<Literal>] INT64   = "Int64"
    let [<Literal>] INT8    = "SByte"
    let [<Literal>] STRING  = "String"
    let [<Literal>] UINT16  = "UInt16"
    let [<Literal>] UINT32  = "UInt32"
    let [<Literal>] UINT64  = "UInt64"
    let [<Literal>] UINT8   = "Byte"

    let [<Literal>] PLCBOOL    = "bit"
    let [<Literal>] PLCUINT8   = "byte"
    let [<Literal>] PLCUINT16  = "word"
    let [<Literal>] PLCUINT32  = "dword"
    let [<Literal>] PLCUINT64  = "lword"

    let typeDefaultValue (typ:System.Type) =
        match typ.Name with
        | BOOL      -> box false
        | CHAR      -> box ' '
        | FLOAT32   -> box 0.0f
        | FLOAT64   -> box 0.0
        | INT16     -> box 0s
        | INT32     -> box 0
        | INT64     -> box 0L
        | INT8      -> box 0y
        | STRING    -> box ""
        | UINT16    -> box 0us
        | UINT32    -> box 0u
        | UINT64    -> box 0UL
        | UINT8     -> box 0uy
        | _  -> failwithlog "ERROR"

    let typeDefaultToString (typ:System.Type) =
        match typ.Name with
        | BOOL      -> "false"
        | CHAR      -> "' '"
        | FLOAT32   -> "0.0f"
        | FLOAT64   -> "0.0"
        | INT16     -> "0s"
        | INT32     -> "0"
        | INT64     -> "0L"
        | INT8      -> "0y"
        | STRING    -> ""
        | UINT16    -> "0us"
        | UINT32    -> "0u"
        | UINT64    -> "0UL"
        | UINT8     -> "0uy"
        | _  -> failwithlog "ERROR"

 
    type DataType =
        | DuBOOL
        | DuCHAR
        | DuFLOAT32
        | DuFLOAT64
        | DuINT16
        | DuINT32
        | DuINT64
        | DuINT8
        | DuSTRING
        | DuUINT16
        | DuUINT32
        | DuUINT64
        | DuUINT8

        member x.ToText() =
            match x with
            | DuBOOL    -> BOOL
            | DuCHAR    -> CHAR
            | DuFLOAT32 -> FLOAT32
            | DuFLOAT64 -> FLOAT64
            | DuINT16   -> INT16
            | DuINT32   -> INT32
            | DuINT64   -> INT64
            | DuINT8    -> INT8
            | DuSTRING  -> STRING
            | DuUINT16  -> UINT16
            | DuUINT32  -> UINT32
            | DuUINT64  -> UINT64
            | DuUINT8   -> UINT8

         member x.ToPLCText() =
            match x with
            | DuBOOL    -> PLCBOOL
            | DuCHAR    -> CHAR
            | DuFLOAT32 -> FLOAT32
            | DuFLOAT64 -> FLOAT64
            | DuINT16   -> INT16
            | DuINT32   -> INT32
            | DuINT64   -> INT64
            | DuINT8    -> INT8
            | DuSTRING  -> STRING
            | DuUINT16  -> PLCUINT16
            | DuUINT32  -> PLCUINT32
            | DuUINT64  -> PLCUINT64
            | DuUINT8   -> PLCUINT8

        member x.ToPLCBitSize() =
            match x with
            | DuBOOL    -> 1
            | DuCHAR    -> 8  //test ahn 확인필요
            | DuFLOAT32 -> 32
            | DuFLOAT64 -> 64
            | DuINT16   -> 16
            | DuINT32   -> 32
            | DuINT64   -> 64
            | DuINT8    -> 8
            | DuSTRING  -> (32*8)
            | DuUINT16  -> 16
            | DuUINT32  -> 32
            | DuUINT64  -> 64
            | DuUINT8   -> 8

         member x.ToPLCType() =
            match x with
            | DuBOOL    -> "BOOL"
            | DuCHAR    -> "CHAR"
            | DuFLOAT32 -> "REAL"
            | DuFLOAT64 -> "LREAL"
            | DuINT8    -> "SINT"
            | DuINT16   -> "INT"
            | DuINT32   -> "DINT"
            | DuINT64   -> "LINT"
            | DuSTRING  -> "STRING"
            | DuUINT16  -> "UINT"
            | DuUINT32  -> "UDINT"
            | DuUINT64  -> "ULINT"
            | DuUINT8   -> "BYTE"


         member x.ToStringValue (value: obj) =
            match x, value with
            | DuBOOL     , _ -> value.ToString()
            | DuCHAR     , _ -> sprintf "'%c'" (Convert.ToChar(value))
            | DuFLOAT32  , (:? float32 as v) -> sprintf "%gf" v
            | DuFLOAT64  , (:? float as v) -> sprintf "%g" v
            | DuINT16    , (:? int16 as v) -> sprintf "%ds" v
            | DuINT32    , (:? int as v) -> sprintf "%d" v
            | DuINT64    , (:? int64 as v) -> sprintf "%dL" v
            | DuINT8     , (:? sbyte as v) -> sprintf "%dy" v
            | DuSTRING   , (:? string as v) -> sprintf "\"%s\"" v
            | DuUINT16   , (:? uint16 as v) -> sprintf "%dus" v
            | DuUINT32   , (:? uint32 as v) -> sprintf "%du" v
            | DuUINT64   , (:? uint64 as v) -> sprintf "%dUL" v
            | DuUINT8    , (:? byte as v) -> sprintf "%duy" v
            | _  -> failwithf "ERROR: Unsupported type %s for value %O" (x.ToText()) value

            
        member x.ToTextLower() = x.ToText().ToLower()
        member x.ToBlockSizeNText() = 
            match x with
            | DuUINT16  -> 16, PLCUINT16 
            | DuUINT32  -> 32, PLCUINT32 
            | DuUINT64  -> 64, PLCUINT64 
            | DuUINT8   -> 8 , PLCUINT8
            | _ -> failwithf $"'{x}' not support ToBlockSize"

        member x.ToType() =
            match x with
            | DuBOOL    -> typedefof<bool>
            | DuCHAR    -> typedefof<char>
            | DuFLOAT32 -> typedefof<single>
            | DuFLOAT64 -> typedefof<double>
            | DuINT16   -> typedefof<int16>
            | DuINT32   -> typedefof<int32>
            | DuINT64   -> typedefof<int64>
            | DuINT8    -> typedefof<int8>
            | DuSTRING  -> typedefof<string>
            | DuUINT16  -> typedefof<uint16>
            | DuUINT32  -> typedefof<uint32>
            | DuUINT64  -> typedefof<uint64>
            | DuUINT8   -> typedefof<uint8>
            

             

        member x.ToValue(valueText:string) =
            
            let valueText = 
                if x = DuCHAR || x = DuSTRING || x = DuBOOL  //"false" //"' '" //""
                then  valueText 
                else valueText.TrimEnd([|'f';'s';'L';'u';'s';'U';'L';'y'|])   //"0.0f" //"0.0" //"0s" //"0" //"0L" //"0y" //"0us" //"0u" //"0UL" //"0uy"
              
            match x with

            | DuBOOL    -> valueText |> Convert.ToBoolean |> box       
            | DuCHAR    -> valueText |> Convert.ToChar    |> box 
            | DuFLOAT32 -> valueText |> Convert.ToSingle  |> box 
            | DuFLOAT64 -> valueText |> Convert.ToDouble  |> box 
            | DuINT16   -> valueText |> Convert.ToInt16   |> box 
            | DuINT32   -> valueText |> Convert.ToInt32   |> box 
            | DuINT64   -> valueText |> Convert.ToInt64   |> box 
            | DuINT8    -> valueText |> Convert.ToSByte   |> box 
            | DuSTRING  -> valueText                      |> box 
            | DuUINT16  -> valueText |> Convert.ToUInt16  |> box 
            | DuUINT32  -> valueText |> Convert.ToUInt32  |> box 
            | DuUINT64  -> valueText |> Convert.ToUInt64  |> box 
            | DuUINT8   -> valueText |> Convert.ToByte    |> box 

        member x.DefaultValue() = typeDefaultValue (x.ToType())

    let getDataType (typ:System.Type) =
        match typ.Name with
        | BOOL      -> DuBOOL
        | CHAR      -> DuCHAR
        | FLOAT32   -> DuFLOAT32
        | FLOAT64   -> DuFLOAT64
        | INT16     -> DuINT16
        | INT32     -> DuINT32
        | INT64     -> DuINT64
        | INT8      -> DuINT8
        | STRING    -> DuSTRING
        | UINT16    -> DuUINT16
        | UINT32    -> DuUINT32
        | UINT64    -> DuUINT64
        | UINT8     -> DuUINT8
        | _  -> failwithlog "ERROR"


    let getTextValueNType (x: string) =
        match x with
        | _ when x.StartsWith("\"") && x.EndsWith("\"") && x.Length > 1 ->
            Some (x.[1..x.Length-2], DuSTRING)
        | _ when x.StartsWith("'") && x.EndsWith("'") && x.Length = 3 ->
            Some (x.[1].ToString(), DuCHAR)
        | _ when x.EndsWith("f") && x |> Seq.forall (fun c -> Char.IsDigit(c) || c = '.' || c = 'f') ->
            Some (x.TrimEnd('f'), DuFLOAT32)
        | _ when x.Contains('.') && x |> Seq.forall (fun c -> Char.IsDigit(c) || c = '.') ->
            Some (x, DuFLOAT64)
        | _ when x.EndsWith("uy") && Byte.TryParse(x.TrimEnd([|'u';'y'|]))|> fst  ->
            Some (x.TrimEnd([|'u';'y'|]), DuUINT8)
        | _ when x.EndsWith("us") && UInt16.TryParse(x.TrimEnd([|'u';'s'|]))|> fst  ->
            Some (x.TrimEnd([|'u';'s'|]), DuUINT16)
        | _ when x.EndsWith("u") && UInt32.TryParse(x.TrimEnd('u'))|> fst  ->
            Some (x.TrimEnd('u'), DuUINT32)
        | _ when x.EndsWith("UL") && UInt64.TryParse(x.TrimEnd([|'U';'L'|]))|> fst  ->
            Some (x.TrimEnd([|'U';'L'|]), DuUINT64)
        | _ when x.ToLower() = "true" || x.ToLower() = "false" ->
            Some (x, DuBOOL) 
        | _ when x.ToLower() = "t" -> Some ("true", DuBOOL)
        | _ when x.ToLower() = "f" -> Some ("false", DuBOOL)
        | _ when x.EndsWith("L") && Int64.TryParse(x.TrimEnd('L'))|> fst  ->
            Some (x.TrimEnd('L'), DuINT64)
        | _ when x.EndsWith("I") && Int32.TryParse(x.TrimEnd('I'))|> fst  ->
            Some (x.TrimEnd('I'), DuINT32)
        | _ when x.EndsWith("s") && Int16.TryParse(x.TrimEnd('s'))|> fst  ->
            Some (x.TrimEnd('s'), DuINT16)
        | _ when x.EndsWith("y") && SByte.TryParse(x.TrimEnd('y'))|> fst  ->
            Some (x.TrimEnd('y'), DuINT8)

        | _ when Int32.TryParse(x) |> fst -> 
            Some (x, DuINT32)
        | _ -> None
        
    let getTrimmedValueNType(x)  = 
        let trimmedTextValueNDataType = getTextValueNType x
        match trimmedTextValueNDataType with
        | Some (v,ty) -> ty.ToValue(v), ty
        | None -> failwithlog $"TryParse error datatype {x}"

    let toValue (x:string) = getTrimmedValueNType x |> fst

    let toValueType (x:string) = getTrimmedValueNType x |> snd


    type IOType = | In | Out | Memory | NotUsed

    type SlotDataType = int *IOType * DataType

    let getBlockType(blockSlottype:string) =
        match blockSlottype.ToLower() with
        | PLCUINT8  -> DuUINT8
        | PLCUINT16 -> DuUINT16
        | PLCUINT32 -> DuUINT32
        | PLCUINT64 -> DuUINT64
        | _ -> failwithf $"'size bit {blockSlottype}' not support getBlockType"


    let tryTextToDataType(typeName:string) =
        match typeName.ToLower() with
        //system1   | system2   | plc
        | "boolean" | "bool"    | "bit"  ->  DuBOOL      |> Some
        | "char"                         ->  DuCHAR      |> Some
        | "float32" | "single"           ->  DuFLOAT32   |> Some
        | "float64" | "double"           ->  DuFLOAT64   |> Some
        | "int16"   | "short"            ->  DuINT16     |> Some
        | "int32"   | "int"              ->  DuINT32     |> Some
        | "int64"   | "long"             ->  DuINT64     |> Some
        | "int8"    | "sbyte"            ->  DuINT8      |> Some
        | "string"                       ->  DuSTRING    |> Some
        | "uint16"  | "ushort"  |"word"  ->  DuUINT16    |> Some
        | "uint32"  | "uint"    |"dword" ->  DuUINT32    |> Some
        | "uint64"  | "ulong"   |"lword" ->  DuUINT64    |> Some
        | "uint8"   | "byte"    |"byte"  ->  DuUINT8     |> Some
        | _ -> None


    let textToDataType(typeName:string) : DataType =
        tryTextToDataType typeName
        |> Option.defaultWith(fun () -> failwithf $"'{typeName}' DataToType Error check type")

    let textToSystemType(typeName:string) : System.Type =
        textToDataType typeName |> fun x -> x.ToType()
