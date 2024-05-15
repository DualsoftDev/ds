// Copyright (c) Dual Inc.  All Rights Reserved.
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


        member x.ToTextLower() = x.ToText().ToLower()
        member x.ToBlockSizeNText() = 
            match x with
            | DuUINT16  -> 16, "Word"
            | DuUINT32  -> 32, "DWord"
            | DuUINT64  -> 64, "LWord"
            | DuUINT8   -> 8 , "Byte"
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


    let getValueNType (x:string) =
        let trimmedValueNDataType = 
            let mutable value = 0
            match x with
            | _ when x.StartsWith("\"") && x.EndsWith("\"") && x.Length > 1 ->
                (x.[1..x.Length-2], DuSTRING) |> Some
            | _ when x.StartsWith("'") && x.EndsWith("'") && x.Length = 3 ->
                (x.[1].ToString(), DuCHAR)  |>Some
            | _ when x.Contains('.') ->
                if x.EndsWith("f") then  (x.TrimEnd('f'), DuFLOAT32)  |>Some else (x, DuFLOAT64)  |>Some
            | _ when x.EndsWith("L") ->  (x.TrimEnd('L'), DuINT64) |>Some
            | _ when x.EndsWith("u") ->  (x.TrimEnd('u'), DuUINT32) |>Some
            | _ when x.EndsWith("y") ->  (x.TrimEnd('y'), DuINT8) |>Some
            | _ when x.EndsWith("s") ->  (x.TrimEnd('s'), DuINT16) |>Some
            | _ when x.EndsWith("uy") -> (x.TrimEnd([|'u';'y'|]), DuUINT8) |>Some
            | _ when x.EndsWith("us") -> (x.TrimEnd([|'u';'s'|]), DuUINT16) |>Some
            | _ when x.EndsWith("UL") -> (x.TrimEnd([|'U';'L'|]), DuUINT64) |>Some
            | _ when x.ToLower() = "true" || x.ToLower() = "false" -> (x, DuBOOL) |>Some
            | _ when System.Int32.TryParse (x, &value)-> (x, DuINT32) |>Some
            | _ -> None
        trimmedValueNDataType
        
    let toValue (x:string) =
        let trimmedValueNDataType =  getValueNType x
        match trimmedValueNDataType with
        | Some (value, datatype) -> datatype.ToValue value
        | None -> failwithlog $"TryParse error datatype {x}"

    type IOType = | In | Out | Memory | NotUsed

    type SlotDataType = int *IOType * DataType

    let getBlockType(blockSlottype:string) =
        match blockSlottype.ToLower() with
        | "byte"  -> DuUINT8
        | "word"  -> DuUINT16
        | "dword" -> DuUINT32
        | "lword" -> DuUINT64
        | _ -> failwithf $"'size bit {blockSlottype}' not support getBlockType"


    let textToDataType(typeName:string) =
        match typeName.ToLower() with
        //system1   | system2   | plc
        | "boolean" | "bool"    | "bit"  ->  DuBOOL
        | "char"                         ->  DuCHAR
        | "float32" | "single"           ->  DuFLOAT32
        | "float64" | "double"           ->  DuFLOAT64
        | "int16"   | "short"            ->  DuINT16
        | "int32"   | "int"              ->  DuINT32
        | "int64"   | "long"             ->  DuINT64
        | "int8"    | "sbyte"            ->  DuINT8
        | "string"                       ->  DuSTRING
        | "uint16"  | "ushort"  |"word"  ->  DuUINT16
        | "uint32"  | "uint"    |"dword" ->  DuUINT32
        | "uint64"  | "ulong"   |"lword" ->  DuUINT64
        | "uint8"   | "byte"    |"byte"  ->  DuUINT8
        | _ -> failwithf $"'{typeName}' DataToType Error check type"
