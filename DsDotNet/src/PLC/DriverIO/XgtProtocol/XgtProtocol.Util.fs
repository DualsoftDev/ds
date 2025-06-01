namespace XgtProtocol

open System
open System.Net
open System.Text
open System.Net.Sockets
open System.Threading
open System.Text.RegularExpressions
open Dual.PLC.Common.FS
open XgtEthernetType

/// XGT Ethernet 통신 관련 유틸리티 함수들
[<AutoOpen>]
module XgtEthernetUtil =
    /// XGT 프로토콜 헤더 크기 (바이트)
    [<Literal>] 
    let HeaderSize = 20
    
    /// 128비트 부호없는 정수를 표현하는 구조체
    /// PLC 통신에서 대용량 데이터 처리를 위해 사용
    type PLCUInt128 = struct
        val Low: uint64   // 하위 64비트
        val High: uint64  // 상위 64비트
        
        /// 새로운 PLCUInt128 인스턴스 생성
        /// low: 하위 64비트 값
        /// high: 상위 64비트 값
        new (low, high) = { Low = low; High = high }

        /// 128비트 정수를 문자열로 변환
        /// BigInteger를 사용하여 정확한 문자열 표현 제공
        override this.ToString() = 
            let bytes = Array.concat [
                BitConverter.GetBytes(this.Low)
                BitConverter.GetBytes(this.High)
            ]
            let bigInt = System.Numerics.BigInteger(bytes)
            bigInt.ToString()
    end

    /// PLC 디바이스 주소 문자열을 생성하는 함수
    /// deviceType: 디바이스 타입 문자 (예: 'M', 'D', 'X' 등)
    /// plcDataSizeType: PLC 데이터 크기 타입
    /// bitOffset: 비트 오프셋 위치
    /// 반환: PLC 주소 문자열 (예: "%MD100")
    let getAddress (deviceType: char) (plcDataSizeType: PlcDataSizeType) (bitOffset: int) = 
        // 데이터 타입에 따른 인덱스 계산
        let index = 
            match plcDataSizeType with
            | PlcDataSizeType.Boolean -> bitOffset      // 비트 단위
            | PlcDataSizeType.Byte    -> bitOffset / 8  // 바이트 단위
            | PlcDataSizeType.UInt16  -> bitOffset / 16 // 16비트 워드 단위
            | PlcDataSizeType.UInt32  -> bitOffset / 32 // 32비트 더블워드 단위
            | PlcDataSizeType.UInt64  -> bitOffset / 64 // 64비트 쿼드워드 단위
            | _ -> failwithf "지원하지 않는 데이터 타입입니다: %A" plcDataSizeType
            
        // PLC 주소 형식: %디바이스타입데이터타입인덱스
        sprintf "%%%c%c%d" 
            deviceType
            (toDataTypeChar(plcDataSizeType))
            index

    /// PLC 읽기/쓰기 작업을 위한 데이터 블록 구조
    type ReadWriteBlock = {
        DeviceType: char                // PLC 디바이스 타입 ('M', 'D', 'X' 등)
        DataType: PlcDataSizeType      // 데이터 크기 타입
        BitPosition: int               // 바이트 내 비트 위치 (0-7)
        ByteOffset: int                // 바이트 오프셋
        value: obj                     // 실제 데이터 값
    } with 
        /// 전체 비트 오프셋 계산 (바이트 오프셋 * 8 + 비트 위치)
        member this.BitOffset = this.ByteOffset * 8 + this.BitPosition
        
        /// PLC 주소 문자열 생성
        member this.Address = getAddress this.DeviceType this.DataType this.BitOffset

/// 네트워크 관련 유틸리티 함수들
[<AutoOpen>]
module NetworkUtils =
    /// IP 주소에서 프레임 ID 바이트 배열을 생성
    /// XGT 프로토콜에서 클라이언트 식별을 위해 IP 주소의 마지막 두 옥텟을 사용
    /// ip: IPv4 주소 문자열 (예: "192.168.1.100")
    /// 반환: 마지막 두 옥텟의 바이트 배열
    let getFrameIDBytesFromIP (ip: string) : byte[] =
        let parts = ip.Split('.')
        if parts.Length <> 4 then 
            failwithf "잘못된 IP 주소 형식입니다: %s" ip
        [| byte (int parts.[2]); byte (int parts.[3]) |]

    /// 프레임 헤더에 회사 ID를 복사
    /// XGT 프로토콜 규격에 따른 회사 식별자 설정
    /// frame: 대상 프레임 바이트 배열 (최소 8바이트 필요)
    let copyCompanyIdToFrame (frame: byte[]) =
        if frame.Length < 8 then
            failwithf "프레임 길이가 너무 짧습니다. 최소 8바이트 필요: 현재 %d바이트" frame.Length
        Array.Copy(CompanyHeader.IdBytes, 0, frame, 0, 8)

/// 데이터 변환 관련 유틸리티 함수들
[<AutoOpen>]
module DataConverter =
    /// 문자열에서 PLCUInt128로 파싱
    /// 큰 정수 값을 문자열로부터 128비트 구조체로 변환
    /// s: 숫자 문자열
    /// 반환: PLCUInt128 구조체
    let parseUInt128FromString (s: string) : PLCUInt128 =
        let bigInt = System.Numerics.BigInteger.Parse(s)
        let bytes = bigInt.ToByteArray()
        // 16바이트로 패딩 (128비트)
        let padded = Array.append bytes (Array.create (16 - bytes.Length) 0uy)
        let low = BitConverter.ToUInt64(padded, 0)   // 하위 64비트
        let high = BitConverter.ToUInt64(padded, 8)  // 상위 64비트
        PLCUInt128(low, high)

  

    /// PLC 데이터 타입에 따른 바이트 배열 변환
    /// dt: PLC 데이터 크기 타입
    /// value: 변환할 값
    /// 반환: 해당 타입의 바이트 배열
    let toBytes (dt: PlcDataSizeType) (value: obj) : byte[] =
        match dt with
        | PlcDataSizeType.Boolean -> 
            // 불린 값을 1바이트로 변환 (true: 0x01, false: 0x00)
            [| if unbox<bool> value then 0x01uy else 0x00uy |]
        | PlcDataSizeType.Byte    -> 
            // 단일 바이트
            [| unbox<byte> value |]
        | PlcDataSizeType.UInt16  -> 
            // 16비트 부호없는 정수 (2바이트)
            BitConverter.GetBytes(unbox<uint16> value)
        | PlcDataSizeType.UInt32  -> 
            // 32비트 부호없는 정수 (4바이트)
            BitConverter.GetBytes(unbox<uint32> value)
        | PlcDataSizeType.UInt64  -> 
            // 64비트 부호없는 정수 (8바이트)
            BitConverter.GetBytes(unbox<uint64> value)
        | _ -> failwithf "지원하지 않는 데이터 타입입니다: %A" dt

/// 프레임 생성 및 처리 관련 유틸리티 함수들
[<AutoOpen>]
module FrameUtils =
    /// 체크섬 계산 함수
    /// XGT 프로토콜의 오류 검출을 위한 간단한 합계 체크섬
    /// data: 체크섬을 계산할 바이트 배열
    /// length: 계산할 데이터 길이
    /// 반환: 체크섬 바이트
    let calculateChecksum (data: byte[]) (length: int) : byte =
        data 
        |> Seq.take length 
        |> Seq.sumBy int 
        |> byte



/// ReadWriteBlock 생성을 위한 팩토리 함수들
[<AutoOpen>]
module ReadWriteBlockFactory =
    /// 주소 문자열에서 디바이스 정보 추출
    /// 두 가지 주소 형식 지원: XGI 태그 형식(%로 시작), XGK 태그 형식
    /// addr: PLC 주소 문자열
    /// dataType: 데이터 타입
    /// 반환: (디바이스명, 데이터타입, 비트오프셋) 튜플
    let getAddressInfo (addr: string) (dataType: PlcDataSizeType) = 
        if addr.StartsWith("%") then
            // XGI 태그 형식 파싱 (예: %MD100)
            LsXgiTagParser.Parse addr 
        else
            // XGK 태그 형식 파싱 (예: M100)
            LsXgkTagParser.Parse (addr, (dataType = PlcDataSizeType.Boolean))

    /// 쓰기 작업용 ReadWriteBlock 생성
    /// addr: PLC 주소 문자열
    /// dataType: 데이터 타입
    /// value: 쓸 데이터 값
    /// 반환: ReadWriteBlock 구조체
    let getReadWriteBlock (addr: string) (dataType: PlcDataSizeType) (value: obj) : ReadWriteBlock =
        let (deviceName, addressDataType, bitOffset) = getAddressInfo addr dataType
        
        // 디바이스 타입은 단일 문자여야 함
        if deviceName.Length <> 1 then 
            failwithf "지원하지 않는 디바이스 타입입니다: %s" deviceName

        // 주소에서 추출한 데이터 타입과 입력 데이터 타입 일치 확인
        let actualType = PlcDataSizeType.FromBitSize addressDataType
        if actualType <> dataType then
            failwithf "입력 데이터 타입(%A)과 주소의 데이터 타입(%A)이 일치하지 않습니다." dataType actualType

        // 비트 단위 데이터의 경우 바이트 내 위치 계산
        let bitPosition = if dataType = PlcDataSizeType.Boolean then bitOffset % 8 else 0
        let byteOffset = bitOffset / 8
        
        {
            DeviceType = deviceName.[0]
            DataType = dataType
            BitPosition = bitPosition
            ByteOffset = byteOffset
            value = value
        }

    /// 읽기 작업용 ReadWriteBlock 생성
    /// addr: PLC 주소 문자열
    /// dataType: 읽을 데이터 타입
    /// 반환: 값이 null인 ReadWriteBlock (읽기 전용)
    let getReadBlock (addr: string) (dataType: PlcDataSizeType) : ReadWriteBlock =
        let (_deviceName, addressDataType, _bitOffset) = getAddressInfo addr dataType
        // 주소에서 추출한 실제 데이터 타입으로 블록 생성 (값은 null)
        getReadWriteBlock addr (PlcDataSizeType.FromBitSize addressDataType) null