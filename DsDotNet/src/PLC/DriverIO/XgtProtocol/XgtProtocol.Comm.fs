namespace XgtProtocol

open System
open System.Net
open System.Text
open System
open System.Net.Sockets
open System.Text
open System.Threading
open Dual.PLC.Common.FS
open System.Text.RegularExpressions

[<AutoOpen>]
module XgtEthernetUtil = 
    //batch 128bit 단위 read용 구조
    type PLCUInt128 = struct
            val Low: uint64
            val High: uint64
            new (low, high) = { Low = low; High = high }
        end

    type WriteEFMTB = {
        DeviceType:char
        DataType:PlcDataSizeType
        BitPosition:int
        Offset :int
        value :obj
    }

    let parseUInt128FromString (s: string) : PLCUInt128 =
        let bigInt = System.Numerics.BigInteger.Parse(s)
        let bytes = bigInt.ToByteArray()
        let padded = Array.append bytes (Array.create (16 - bytes.Length) 0uy)
        let low = BitConverter.ToUInt64(padded, 0)
        let high = BitConverter.ToUInt64(padded, 8)
        PLCUInt128(low, high)

    let toUInt128Bytes (value: obj) : byte[] =
        let u128 =
            match value with
            | :? PLCUInt128 as v -> v
            | :? uint64 as v -> PLCUInt128(v, 0UL)
            | :? int as v -> PLCUInt128(uint64 v, 0UL)
            | :? string as s -> parseUInt128FromString s
            | _ -> failwithf $"UInt128으로 변환할 수 없음: {value}"

        let lowBytes = BitConverter.GetBytes(u128.Low)
        let highBytes = BitConverter.GetBytes(u128.High)
        Array.append lowBytes highBytes

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

        // 공통 파싱 함수
    let parseMultiReadResponseCore (buffer: byte[]) (count: int) (dataType: PlcDataSizeType) (readBuffer: byte[]) (dataBlockSize: int) =
        if buffer.Length < 32 then
            failwith "응답 데이터가 너무 짧습니다."

        let errorState = BitConverter.ToUInt16(buffer, 26)
        if errorState <> 0us then
            let errorCode = buffer.[26]
            failwith $"❌ PLC 응답 에러: 0x{errorCode:X2} - {getXgtErrorDescription errorCode}"

        let elementSizeBits = PlcDataSizeType.TypeBitSize dataType
        let elementSizeBytes = (elementSizeBits + 7) / 8

        let expectedSize = count * elementSizeBytes
        if readBuffer.Length < expectedSize then
            failwith $"readBuffer 크기 부족: {readBuffer.Length} < {expectedSize}"

        let mutable srcOffset = 30
        let mutable dstOffset = 0

        for _ in 0 .. count - 1 do
            srcOffset <- srcOffset + 2 // block size (2 bytes) skip
            Array.Copy(buffer, srcOffset, readBuffer, dstOffset, elementSizeBytes)
            dstOffset <- dstOffset + elementSizeBytes
            srcOffset <- srcOffset + dataBlockSize
    /// 응답 데이터 파싱
    let parseReadResponse(buffer: byte[], dataType: PlcDataSizeType) : obj =
        let errorState = BitConverter.ToUInt16(buffer, 26)
        if errorState <> 0us then
            let errorCode = buffer.[26]
            let msg = getXgtErrorDescription errorCode
            failwith $"❌ PLC 응답 에러: 0x{errorCode:X2} - {msg}"

        if buffer.Length < 32 then failwith "응답 데이터가 너무 짧습니다."
        if buffer.[20] <> 0x55uy then failwith "응답 명령어가 아닙니다."

        match dataType with
        | Boolean -> buffer.[32] = 1uy |> box
        | Byte    -> buffer.[32]       |> box
        | UInt16  -> BitConverter.ToUInt16(buffer, 32) |> box
        | UInt32  -> BitConverter.ToUInt32(buffer, 32) |> box
        | UInt64  -> BitConverter.ToUInt64(buffer, 32) |> box
        | _ -> failwith $"지원하지 않는 데이터 타입입니다: {dataType}"
    let parseMultiReadResponse(buffer, count, dataType, readBuffer) =
        parseMultiReadResponseCore buffer count dataType readBuffer 8
    /// EFMTB 명령어를 사용한 복수 주소 데이터 읽기 응답 파싱
    let parseEFMTBMultiReadResponse(buffer, count, dataType, readBuffer) =
        parseMultiReadResponseCore buffer count dataType readBuffer 16

type XgtEthernet(ip: string, port: int, timeoutMs: int) =
    inherit PlcEthernetBase(ip, port, timeoutMs)

    let frameIDLowByte = byte (ip.Split('.').[2] |> int)
    let frameIDUpperByte = byte (ip.Split('.').[3] |> int)

    /// 단일 주소 데이터 읽기 프레임 생성
    member x.CreateReadFrame(address: string, dataType: PlcDataSizeType) =
        x.CreateMultiReadFrame([| address |], dataType) 
    /// 단일 주소 데이터 쓰기 프레임 생성
    member _.CreateWriteFrame(address: string, dataType: PlcDataSizeType, value: obj) =
        let device = address.Substring(1, 2)
        let addr = address.Substring(3).PadLeft(5, '0')
        let valueBytes =
            match dataType with
            | Boolean -> [| if unbox<bool> value then 0x01uy else 0x00uy |]
            | Byte -> [| unbox<byte> value |]
            | UInt16 -> BitConverter.GetBytes(unbox<uint16> value)
            | UInt32 -> BitConverter.GetBytes(unbox<uint32> value)
            | UInt64 -> BitConverter.GetBytes(unbox<uint64> value)
            | _ -> failwithf $"{dataType}는 지원하지 않는 타입입니다."

        let frame = Array.zeroCreate<byte> (42 + valueBytes.Length)
        Array.Copy(Encoding.ASCII.GetBytes("LSIS-XGT"), 0, frame, 0, 8)
        frame.[12] <- 0xA0uy
        frame.[13] <- 0x33uy
        frame.[14] <- frameIDLowByte        // Frame ID
        frame.[15] <- frameIDUpperByte      // Frame ID
        frame.[16] <- byte (0x16 + valueBytes.Length)
        // Checksum 계산
        let checksum =
            frame
            |> Seq.take 19
            |> Seq.fold (fun acc b -> acc + int b) 0
            |> fun s -> byte (s &&& 0xFF)
        frame.[19] <- checksum

        frame.[20] <- 0x58uy
        frame.[22] <-
            match dataType with
            | Boolean -> 0x00uy
            | Byte -> 0x01uy
            | UInt16 -> 0x02uy
            | UInt32 -> 0x03uy
            | UInt64 -> 0x04uy
            | _ -> failwithf $"지원하지 않는 데이터 타입입니다: {dataType}"
        frame.[26] <- 0x01uy
        frame.[28] <- 0x08uy
        frame.[30] <- byte '%'
        frame.[31] <- byte device.[0]
        frame.[32] <- byte device.[1]
        for i in 0..4 do
            frame.[33 + i] <- byte addr.[i]
        frame.[38] <- byte valueBytes.Length
        frame.[39] <- 0x00uy
        Array.Copy(valueBytes, 0, frame, 40, valueBytes.Length)
        frame
    /// 복수 주소 데이터 읽기 프레임 생성
    member _.CreateMultiReadFrame(addresses: string[], dataType: PlcDataSizeType) : byte[] =
        if addresses.Length = 0 || addresses.Length > 16 then
            failwith "읽기 가능한 주소 수는 1~16개입니다."

        let encodeAddress (addr: string) =
            let device = addr.Substring(0, 3)
            let number = addr.Substring(3).PadLeft(5, '0')
            let full = device + number
            let bytes = Encoding.ASCII.GetBytes(full)
            if bytes.Length <> 8 then failwith $"주소 포맷 오류: {addr}"
            bytes

        let configLength = 8
        let perVarLength = 10
        let bodyLength = configLength + (perVarLength * addresses.Length)
        let totalLength = 20 + bodyLength
        let frame = Array.zeroCreate<byte> totalLength

        // Header
        Array.Copy(Encoding.ASCII.GetBytes("LSIS-XGT"), 0, frame, 0, 8)
        frame.[12] <- 0xA0uy
        frame.[13] <- 0x33uy
        frame.[14] <- frameIDLowByte        // Frame ID
        frame.[15] <- frameIDUpperByte      // Frame ID
        frame.[16] <- byte bodyLength

        // Checksum
        let checksum =
            frame |> Seq.take 19 |> Seq.fold (fun acc b -> acc + int b) 0 |> fun s -> byte (s &&& 0xFF)
        frame.[19] <- checksum

        // Body
        frame.[20] <- 0x54uy // READ
        frame.[22] <-
            match dataType with
            | Boolean -> 0x00uy
            | Byte    -> 0x01uy
            | UInt16  -> 0x02uy
            | UInt32  -> 0x03uy
            | UInt64  -> 0x04uy
            | _ -> failwith $"지원하지 않는 데이터 타입: {dataType}"
        frame.[26] <- byte addresses.Length

        let mutable offset = 28
        for addr in addresses do
            frame.[offset] <- 0x08uy        // 길이
            frame.[offset + 1] <- 0x00uy    // reserved
            Array.Copy(encodeAddress addr, 0, frame, offset + 2, 8)
            offset <- offset + 10

        frame
    /// 다중 주소 읽기 프레임 생성 (EFMTB 명령어 사용)
    member _.CreateMultiReadFrameEFMTB(addresses: string[], dataType: PlcDataSizeType) : byte[] =
        if addresses.Length < 1 || addresses.Length > 64 then
            failwith "읽기 가능한 주소 수는 1~64개입니다."

        // 장치 주소 인코딩
        let encodeAddress (addr: string) =
            let addr = addr.TrimStart('%')
            let bytes = Array.zeroCreate<byte> 8
            let deviceCode = addr.[0]
            bytes.[0] <- byte deviceCode

            // 숫자 부분 추출 (문자 제외)
            let offsetStr =
                let m = Regex.Match(addr, @"\d+")
                if m.Success then m.Value else ""
            
            let offset =
                match Int32.TryParse(offsetStr.ToString()) with
                | true, value -> value
                | _ -> failwith $"주소에서 유효한 숫자를 추출할 수 없습니다: {addr}"

            let isBit = dataType = Boolean

            if isBit then
                // Bit 단위 주소 처리 (1bit = 1 value)
                let byteOffset = offset / 8
                let bitIndex = offset % 8
                bytes.[1] <- 0x58uy                // 'X'로 추정되는 bit 코드
                bytes.[2] <- byte bitIndex
                Array.Copy(BitConverter.GetBytes(byteOffset), 0, bytes, 4, 4)
            else
                // Word 단위 주소 처리
                let byteSize =
                    match dataType with
                    | Boolean -> failwith $"bit 주소가 아닌데 Boolean 타입 요청: {addr}"
                    | Byte -> 1
                    | UInt16 -> 2
                    | UInt32 -> 4
                    | UInt64 -> 8
                    | UInt128 -> 16
                    | _ -> failwith $"지원하지 않는 데이터 타입: {dataType}"

                let byteOffset = offset * byteSize
                bytes.[1] <- 0x42uy                // Word 타입을 의미하는 코드
                bytes.[2] <- byte byteSize
                Array.Copy(BitConverter.GetBytes(byteOffset), 0, bytes, 4, 4)

            bytes

        // 프레임 길이 계산
        let configLength = 8                     // 고정 구성 길이
        let perVarLength = 8                     // 주소당 8바이트
        let bodyLength = configLength + (perVarLength * addresses.Length)
        let totalLength = 20 + bodyLength        // 헤더 포함 전체 길이

        let frame = Array.zeroCreate<byte> totalLength

        // 헤더 구성
        Array.Copy(Encoding.ASCII.GetBytes("LSIS-XGT"), 0, frame, 0, 8) // Signature
        frame.[12] <- 0x00uy       // Reserved or fixed
        frame.[13] <- 0x33uy       // EFMTB 명령 코드
        
        frame.[14] <- frameIDLowByte        // Frame ID
        frame.[15] <- frameIDUpperByte      // Frame ID
        frame.[16] <- byte bodyLength

        // Checksum 계산
        let checksum =
            frame
            |> Seq.take 19
            |> Seq.fold (fun acc b -> acc + int b) 0
            |> fun s -> byte (s &&& 0xFF)
        frame.[19] <- checksum

        // Body: 명령 종류 및 주소 개수 설정
        frame.[20] <- 0x00uy       // Header 종류
        frame.[21] <- 0x10uy       // 명령 코드
        frame.[22] <- 0x10uy       // DataType 묶음 or 기타 의미
        frame.[26] <- byte addresses.Length

        // Body: 주소 나열
        let mutable offset = 28
        for addr in addresses do
            let addrBytes = encodeAddress addr
            Array.Copy(addrBytes, 0, frame, offset, 8)
            offset <- offset + 8

        frame

    /// 다중 주소 데이터 쓰기 프레임 생성 (EFMTB 명령어 사용)
    member _.CreateMultiWriteFrameEFMTB(addresses: string[], dataType: PlcDataSizeType, values: obj[]) : byte[] =
        if addresses.Length <> values.Length then
            failwith "주소 수와 값 수가 일치하지 않습니다."
        if addresses.Length < 1 || addresses.Length > 64 then
            failwith "쓰기 가능한 주소 수는 1~64개입니다."

        // WriteInfo 생성
        let writeInfos = 
            addresses
            |> Array.mapi (fun i addr ->
                let dev, dType, bitOffset =
                    if addr.StartsWith("%") then
                        tryParseXgiTag addr |> Option.get
                    else 
                        tryParseXgkTag addr |> Option.get

                if dev.Length <> 1 
                then 
                    failwithf $"지원하지 않는 디바이스 타입입니다: {dev}"

                let dataType, bitPos, offset = 
                    match dType with
                    | 1  -> Boolean, bitOffset % 8, bitOffset / 8
                    | 8  -> Byte,   -1, bitOffset / 8
                    | 16 -> UInt16, -1, bitOffset / 8
                    | 32 -> UInt32, -1, bitOffset / 8
                    | 64 -> UInt64, -1, bitOffset / 8
                    | _ -> failwith $"지원하지 않는 데이터 크기: {dType}"

                {
                    DeviceType = dev.ToCharArray().[0] // 장치 코드 (예: 'M', 'D' 등)
                    DataType = dataType
                    BitPosition = bitPos
                    Offset = offset
                    value = values.[i]
                }
            )

        // value -> byte[]
        let getValueBytes (dt: PlcDataSizeType) (v: obj) =
            match dt with
            | Boolean -> [| if unbox<bool> v then 0x01uy else 0x00uy |]
            | Byte    -> [| unbox<byte> v |]
            | UInt16  -> BitConverter.GetBytes(unbox<uint16> v)
            | UInt32  -> BitConverter.GetBytes(unbox<uint32> v)
            | UInt64  -> BitConverter.GetBytes(unbox<uint64> v)
            | _ -> failwithf $"지원하지 않는 데이터 타입입니다: {dt}"

        // Block 생성 (하나당 10+N byte)
        let blocks =
            writeInfos
            |> Array.map (fun info ->
                let dataBytes = getValueBytes info.DataType info.value
                let sizeBytes = BitConverter.GetBytes(uint16 dataBytes.Length)
                let offsetBytes = BitConverter.GetBytes(uint32 info.Offset)
                let bitPosBytes = BitConverter.GetBytes(uint16 (if info.BitPosition >= 0 then uint16 info.BitPosition else 0us))

                Array.concat [
                    [| byte info.DeviceType |]                      // Device Type (1)
                    [| match info.DataType with                     // Data Type (1)
                       | Boolean -> 0x58uy
                       | _ -> 0x42uy 
                    |]
                    if info.DataType = Boolean 
                    then bitPosBytes 
                    else sizeBytes                                // Bit Position (2) or Size of block (2)
                                                            
                    offsetBytes                                      // Offset (4)
                    dataBytes                                        // Data (variable)
                ]
            )

        let body = Array.concat blocks
        let headerLength = 20
        let bodyLength = 8 + body.Length
        let totalLength = headerLength + bodyLength
        let frame = Array.zeroCreate<byte> totalLength

        // Header
        Array.Copy(Encoding.ASCII.GetBytes("LSIS-XGT"), 0, frame, 0, 8)
        frame.[12] <- 0xA0uy
        frame.[13] <- 0x33uy
        frame.[14] <- frameIDLowByte
        frame.[15] <- frameIDUpperByte
        frame.[16] <- byte bodyLength

        // Checksum
        let checksum =
            frame
            |> Seq.take 19
            |> Seq.fold (fun acc b -> acc + int b) 0
            |> byte
        frame.[19] <- checksum

        // Body Header
        frame.[20] <- 0x10uy           // CMD low byte
        frame.[21] <- 0x10uy           // CMD high byte (0x1010)
        frame.[22] <- 0x10uy           // AREA CODE low
        frame.[23] <- 0x00uy           // AREA CODE high
        frame.[24] <- 0x00uy           // Reserved
        frame.[25] <- 0x00uy
        frame.[26] <- byte writeInfos.Length
        frame.[27] <- 0x00uy           // Block count high

        // Body
        Array.Copy(body, 0, frame, 28, body.Length)

        frame



    /// 단일 주소 데이터 쓰기 프레임 생성 (EFMTB 명령어 사용)
    member x.CreateWriteFrameEFMTB(address: string, dataType: PlcDataSizeType, value: obj) =
        x.CreateMultiWriteFrameEFMTB([| address |], dataType, [| value |]) 

    /// 단일 주소 데이터 읽기
    member this.Read(address: string, dataType: PlcDataSizeType) : obj =
        let frame = this.CreateReadFrame(address, dataType)
        this.SendFrame(frame)
        let buffer = this.ReceiveFrame(256)
        parseReadResponse(buffer, dataType)
    /// 복수 주소 읽기 기본 구현
    member this.Reads(addresses: string[], localEthernet: bool, readBuffer:byte[]) =
        if addresses.Length = 0 then failwith "주소가 없습니다."

        let dataType = if localEthernet then PlcDataSizeType.UInt64 else PlcDataSizeType.UInt128
        let frame =
            if localEthernet 
            then 
                this.CreateMultiReadFrame(addresses, dataType)
            else
                this.CreateMultiReadFrameEFMTB(addresses, dataType)

        this.SendFrame(frame)

        let buffer = this.ReceiveFrame(1024) // 제조사에 따라 버퍼 크기 조정
        if localEthernet
        then 
            parseMultiReadResponse(buffer, addresses.Length, dataType, readBuffer)
        else 
            parseEFMTBMultiReadResponse(buffer, addresses.Length, dataType, readBuffer)

    /// 단일 주소 데이터 쓰기
    member this.Write(address: string, localEthernet: bool, dataType: PlcDataSizeType, value: obj) : bool =
        try
            let frame =
                if localEthernet
                then 
                    this.CreateWriteFrame(address, dataType, value)
                else 
                    this.CreateMultiWriteFrameEFMTB([|address|], dataType, [|value|])

            this.SendFrame(frame)
            let _ = this.ReceiveFrame(256)
            true
        with _ -> false