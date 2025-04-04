namespace Dual.PLC.Common.FS

open System.Runtime.CompilerServices

// PLC 데이터 타입 정의
type PlcDataSizeType =
    | Bit
    | Byte
    | Word
    | DWord
    | LWord

    static member FromBitSize(size: int) =
        match size with
        | 1  -> Bit
        | 8  -> Byte
        | 16 -> Word
        | 32 -> DWord
        | 64 -> LWord
        | _  -> failwithf "[PlcDataSizeType] Unknown data bit size: %d" size

// 접점/코일 종류 정의
type TerminalType = 
    | Empty
    | Coil | Set | Reset | Pulse
    | Contact | ContactNegated | Rising | Falling

    /// Is this a coil type?
    member x.IsCoilType() =
        match x with
        | Coil | Set | Reset | Pulse -> true
        | _ -> false

    /// Is this a contact type?
    member x.IsContactType() =
        match x with
        | Contact | ContactNegated | Rising | Falling -> true
        | _ -> false

    /// static helper: coil 판별
    static member IsCoilType(t: TerminalType) = t.IsCoilType()

    /// static helper: contact 판별
    static member IsContactType(t: TerminalType) = t.IsContactType()

// PLC 태그 메타 정보
type IPlcTag =
    abstract Name: string
    abstract DataType: PlcDataSizeType
    abstract Comment: string

// 읽기/쓰기 기능 포함
type IPlcTagReadWrite =
    abstract Address: string
    abstract Value: obj with get, set
    abstract SetWriteValue: obj -> unit
    abstract ClearWriteValue: unit -> unit
    abstract GetWriteValue: unit -> option<obj>

// 전체 태그 정보
type IPlcTerminal =
    inherit IPlcTag
    inherit IPlcTagReadWrite
    abstract TerminalType: TerminalType with get

// 문자열 → PlcDataSizeType 확장
[<Extension>] 
type PlcTagExt =

    [<Extension>] 
    static member ToSystemDataType(txt: string) : PlcDataSizeType =
        match txt.Trim().ToLowerInvariant() with
        // Bool
        | "boolean" | "bool" | "bit"                    -> Bit
        // Char
        | "char"                                        -> Byte
        // Float / Real
        | "float32" | "single"                          -> DWord
        | "float64" | "double" | "real"                 -> LWord
        // Signed integers
        | "int8"  | "sbyte"                             -> Byte
        | "int16" | "short"                             -> Word
        | "int32" | "int"                               -> DWord
        | "int64" | "long"                              -> LWord
        // Unsigned integers
        | "uint8"  | "byte"                             -> Byte
        | "uint16" | "ushort" | "word"                  -> Word
        | "uint32" | "uint"   | "dword"                 -> DWord
        | "uint64" | "ulong"  | "lword"                 -> LWord
        // String-like → 가장 큰 타입 할당
        | "string" | "text" | "wstring"                 -> LWord
        // Unknown
        | _ -> failwithf "[ToSystemDataType] Unknown data type string: %s" txt
