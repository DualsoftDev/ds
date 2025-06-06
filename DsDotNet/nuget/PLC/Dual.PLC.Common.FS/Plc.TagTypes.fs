namespace Dual.PLC.Common.FS

open System.Runtime.CompilerServices

type PlcDataSizeType =
    | Boolean
    | SByte    | Byte
    | Int16    | UInt16
    | Int32    | UInt32  | Float     // REAL
    | Int64    | UInt64  | Double    // LREAL
    | Int128   | UInt128
    | String
    | DateTime
    | UserDefined
    static member GetAllTypes() =
        [|
            Boolean
            SByte
            Byte
            Int16
            UInt16
            Int32
            UInt32
            Int64
            UInt64      
            Int128
            UInt128
            Float
            Double
            String
            DateTime
        |]

    /// 비트 수로부터 PlcDataSizeType 추론
    static member FromBitSize(size: int) =
        match size with
        | 1   -> Boolean
        | 8   -> Byte        // 기본 Byte
        | 16  -> UInt16      // 기본 UInt16
        | 32  -> UInt32      // 기본 UInt32
        | 64  -> UInt64      // 기본 UInt64
        | _   -> failwithf "[PlcDataSizeType] Unknown data bit size: %d" size

    /// PlcDataSizeType → 비트 수
    static member TypeBitSize(dataType: PlcDataSizeType) =
        match dataType with
        | Boolean    -> 1
        | SByte      -> 8
        | Byte       -> 8
        | Int16      -> 16
        | UInt16     -> 16
        | Int32      -> 32
        | UInt32     -> 32
        | Int64      -> 64
        | UInt64     -> 64
        | Int128     -> 128
        | UInt128    -> 128
        | Float      -> 32
        | Double     -> 64
        | String     -> 8 * 64   // 예: 문자열 최대 길이 기반. 수정 가능
        | DateTime   -> 64       // 일반적으로 8바이트 (예: ticks)
        | UserDefined -> 0        // 사용자 정의 타입은 비트 수를 알 수 없음

    /// PlcDataSizeType → 바이트 수
    static member TypeByteSize(dataType: PlcDataSizeType) =
            (PlcDataSizeType.TypeBitSize dataType + 7) / 8

    static member TryFromString(txt: string) : PlcDataSizeType option =
        match txt.Trim().ToUpperInvariant() with
        | "BOOL" | "BOOLEAN" | "BIT"            -> Some PlcDataSizeType.Boolean
        | "SBYTE" | "SINT"                      -> Some PlcDataSizeType.SByte
        | "BYTE" | "USINT"                      -> Some PlcDataSizeType.Byte
        | "INT" | "INT16"                       -> Some PlcDataSizeType.Int16
        | "UINT" | "UINT16" | "WORD"            -> Some PlcDataSizeType.UInt16
        | "DINT" | "INT32"                      -> Some PlcDataSizeType.Int32
        | "UDINT" | "UINT32" | "DWORD"          -> Some PlcDataSizeType.UInt32
        | "LINT" | "INT64"                      -> Some PlcDataSizeType.Int64
        | "ULINT" | "UINT64" | "LWORD"          -> Some PlcDataSizeType.UInt64
        | "REAL" | "FLOAT" | "FLOAT32"          -> Some PlcDataSizeType.Float
        | "LREAL" | "DOUBLE" | "FLOAT64"        -> Some PlcDataSizeType.Double
        | "STRING"                              -> Some PlcDataSizeType.String
        | "DATETIME" | "DATE_AND_TIME"          -> Some PlcDataSizeType.DateTime
        | unknown -> 
            None 
    
    static member FromString(txt: string) : PlcDataSizeType =
        match PlcDataSizeType.TryFromString txt with
        | Some v -> v
        | None ->
            failwithf "Unknown PlcTag type string: %s" txt

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
