namespace Dual.PLC.Common.FS

open System.Runtime.CompilerServices

// PLC 데이터 타입 정의
type PlcDataSizeType =
    | Boolean
    | SByte
    | Byte
    | Int16
    | UInt16
    | Int32
    | UInt32
    | Int64
    | UInt64
    | Float  // = REAL
    | Double // = LREAL
    | String
    | DateTime

    static member FromBitSize(size: int) =
        match size with
        | 1  -> Boolean
        | 8  -> Byte
        | 16 -> UInt16
        | 32 -> UInt32
        | 64 -> UInt64
        | _  -> failwithf "[PlcDataSizeType] Unknown data bit size: %d" size

type ReadWriteType =
    | Read
    | Write

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
    abstract ReadWriteType: ReadWriteType
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
        match txt.Trim().ToUpperInvariant() with
        | "BOOL" | "BOOLEAN" | "BIT"            -> PlcDataSizeType.Boolean
        | "SBYTE" | "SINT"                      -> PlcDataSizeType.SByte
        | "BYTE" | "USINT"                      -> PlcDataSizeType.Byte
        | "INT" | "INT16"                       -> PlcDataSizeType.Int16
        | "UINT" | "UINT16" | "WORD"            -> PlcDataSizeType.UInt16
        | "DINT" | "INT32"                      -> PlcDataSizeType.Int32
        | "UDINT" | "UINT32" | "DWORD"          -> PlcDataSizeType.UInt32
        | "LINT" | "INT64"                      -> PlcDataSizeType.Int64
        | "ULINT" | "UINT64" | "LWORD"          -> PlcDataSizeType.UInt64
        | "REAL" | "FLOAT" | "FLOAT32"          -> PlcDataSizeType.Float
        | "LREAL" | "DOUBLE" | "FLOAT64"        -> PlcDataSizeType.Double
        | "STRING"                              -> PlcDataSizeType.String
        | "DATETIME" | "DATE_AND_TIME"          -> PlcDataSizeType.DateTime
        | unknown -> failwithf "Unknown OPC type string: %s" unknown
