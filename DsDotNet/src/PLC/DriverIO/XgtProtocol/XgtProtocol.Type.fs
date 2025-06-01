namespace XgtProtocol

open System
open System.Net
open System.Text
open System.Net.Sockets
open System.Threading
open Dual.PLC.Common.FS
open System.Text.RegularExpressions

[<AutoOpen>]
module XgtEthernetType =

    /// Company Header 정의
    module CompanyHeader =
        let Id = "LSIS-XGT"
        let IdBytes =
            let bytes = Encoding.ASCII.GetBytes(Id)
            Array.append bytes (Array.create (12 - bytes.Length) 0uy)

    /// 지원 디바이스 코드 목록
    module SupportedAreaCodes =
        let areaCodesXGI = [ 'I'; 'Q'; 'F'; 'M'; 'L'; 'N'; 'K'; 'U'; 'R'; 'A'; 'W' ]
        let areaCodesXGK = [ 'P'; 'M'; 'K'; 'T'; 'C'; 'U'; 'S'; 'L'; 'N'; 'D'; 'R' ]
        let all = List.append areaCodesXGI areaCodesXGK |> List.distinct

    /// CPU Type 정의
    [<RequireQualifiedAccess>]
    type CpuType =
        | XGK_CPUH = 0x01
        | XGK_CPUS = 0x02

    /// 시스템 상태 (SystemStatus)
    [<Flags>]
    type SystemStatus =
        | STOP   = 0x02
        | RUN    = 0x04
        | PAUSE  = 0x08
        | DEBUG  = 0x10

    /// Source Frame 정보
    [<RequireQualifiedAccess>]
    type FrameSource =
        | ClientToServer = 0x33uy
        | ServerToClient = 0x11uy

    /// 명령어 코드 정의
    [<RequireQualifiedAccess>]
    type CommandCode =
        | ReadRequestEFMTB  = 0x1000us
        | ReadRequest       = 0x0054us
        | ReadResponse      = 0x0055us
        | WriteRequestEFMTB = 0x1010us
        | WriteRequest      = 0x0058us
        | WriteResponse     = 0x0059us
        | StatusRequest     = 0x00B0us
        | StatusResponse    = 0x00B1us

    /// 데이터 타입 코드 정의 (Read/Write 시 사용)
    [<RequireQualifiedAccess>]
    type DataType =
        | BIT     = 0x00us
        | BYTE    = 0x01us
        | WORD    = 0x02us
        | DWORD   = 0x03us
        | LWORD   = 0x04us

    /// 디바이스 타입 정의
    [<RequireQualifiedAccess>]
    type DeviceType =
        | P = 0x50uy
        | M = 0x51uy
        | K = 0x53uy
        | T = 0x55uy
        | C = 0x56uy
        | U = 0x57uy
        | S = 0x58uy
        | L = 0x52uy
        | N = 0x59uy
        | D = 0x5Duy
        | R = 0x5Euy
        | I = 0x60uy
        | Q = 0x61uy
        | F = 0x62uy
        | A = 0x63uy
        | W = 0x64uy

    /// 오류 코드 정의
    [<RequireQualifiedAccess>]
    type ErrorCode =
        | FrameError       = 0x00us
        | UnknownCommand   = 0x02us
        | UnknownSubCmd    = 0x03us
        | AddressError     = 0x04us
        | DataValueError   = 0x05us
        | DataSizeError    = 0x10us
        | DataTypeError    = 0x11us
        | DeviceTypeError  = 0x12us
        | TooManyBlocks    = 0x13us

    /// 응답 상태 코드
    [<RequireQualifiedAccess>]
    type ResponseStatus =
        | OK = 0x0000us
        | Error = 0xFFFFus

    let getXgtErrorDescription (code: byte) : string =
        match code with
        | 0x10uy -> "지원하지 않는 명령어입니다."
        | 0x11uy -> "명령어 포맷 오류입니다."
        | 0x12uy -> "명령어 길이 오류입니다."
        | 0x13uy -> "데이터 타입 오류입니다."
        | 0x14uy -> "변수 개수 오류입니다. (최대 16개)"
        | 0x15uy -> "변수 이름 길이 오류입니다. (최대 16자)"
        | 0x16uy -> "변수 이름 형식 오류입니다. (%, 영문, 숫자만 허용)"
        | 0x17uy -> "존재하지 않거나 접근 불가능한 변수입니다."
        | 0x18uy -> "읽기 권한이 없습니다."
        | 0x19uy -> "쓰기 권한이 없습니다."
        | 0x1Auy -> "PLC 내부 메모리 오류입니다."
        | 0x1Fuy -> "알 수 없는 오류가 발생했습니다."
        | 0x21uy -> "프레임 체크섬(BCC) 오류입니다."
        | _      -> $"알 수 없는 에러 코드: 0x{code:X2}"

    /// PlcDataSizeType을 DataType으로 변환
    let toDataTypeCode (dt: PlcDataSizeType) =
        match dt with
        | PlcDataSizeType.Boolean -> DataType.BIT
        | PlcDataSizeType.Byte    -> DataType.BYTE
        | PlcDataSizeType.UInt16  -> DataType.WORD
        | PlcDataSizeType.UInt32  -> DataType.DWORD
        | PlcDataSizeType.UInt64  -> DataType.LWORD
        | _ -> failwithf $"지원하지 않는 PlcDataSizeType: %A{dt}"

    /// PlcDataSizeType을 문자로 변환
    let toDataTypeChar (dt: PlcDataSizeType) =
        match dt with
        | PlcDataSizeType.Boolean -> 'X'
        | PlcDataSizeType.Byte    -> 'B'
        | PlcDataSizeType.UInt16  -> 'W'
        | PlcDataSizeType.UInt32  -> 'D'
        | PlcDataSizeType.UInt64  -> 'L'
        | _ -> failwithf $"지원하지 않는 PlcDataSizeType: %A{dt}"
