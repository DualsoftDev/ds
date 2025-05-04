namespace XgtProtocol

open System
open System.Net
open System.Text
open Dual.PLC.Common.FS

type XgtEthernet(ip: string, port: int, timeoutMs: int) =
    inherit PlcEthernetBase(ip, port, timeoutMs)

    let frameID = byte (ip.Split('.').[3] |> int)

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
        frame.[14] <- frameID
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

    member x.CreateReadFrame(address: string, dataType: PlcDataSizeType) =
        x.CreateMultiReadFrame([| address |], dataType) 

    member _.ParseMultiReadResponse(buffer: byte[], count: int, dataType: PlcDataSizeType, readBuffer: byte[]) =
        if buffer.Length < 32 then
            failwith "응답 데이터가 너무 짧습니다."
        if buffer.[20] <> 0x55uy then
            failwith "응답 명령어가 아닙니다."

        let errorState = BitConverter.ToUInt16(buffer, 26)
        if errorState <> 0us then
            let errorCode = buffer.[26]
            failwith $"❌ PLC 응답 에러: 0x{errorCode:X2} - {getXgtErrorDescription errorCode}"

        // 타입당 바이트 수 계산
        let elementSizeBits = PlcDataSizeType.TypeBitSize dataType
        let elementSizeBytes = (elementSizeBits + 7) / 8

        let expectedSize = count * elementSizeBytes
        if readBuffer.Length < expectedSize then
            failwith $"readBuffer 크기 부족: {readBuffer.Length} < {expectedSize}"

        let mutable srcOffset = 30
        let mutable dstOffset = 0

        for _ in 0 .. count - 1 do
            srcOffset <- srcOffset + 2         // block size(2 bytes) skip
            Array.Copy(buffer, srcOffset, readBuffer, dstOffset, elementSizeBytes)
            dstOffset <- dstOffset + elementSizeBytes
            srcOffset <- srcOffset + 8         // fixed LWord data size


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
        frame.[14] <- frameID
        frame.[16] <- byte (0x16 + valueBytes.Length)
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

    member _.ParseReadResponse(buffer: byte[], dataType: PlcDataSizeType) : obj =
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


            /// 단일 주소 데이터 읽기
    member this.Read(address: string, dataType: PlcDataSizeType) : obj =
        let frame = this.CreateReadFrame(address, dataType)
        this.SendFrame(frame)
        let buffer = this.ReceiveFrame(256)
        this.ParseReadResponse(buffer, dataType)

    /// 복수 주소 읽기 기본 구현
    member this.Reads(addresses: string[], dataType: PlcDataSizeType, readBuffer:byte[]) =
        if addresses.Length = 0 then failwith "주소가 없습니다."
        let frame = this.CreateMultiReadFrame(addresses, dataType)
        this.SendFrame(frame)
        let buffer = this.ReceiveFrame(512) // 제조사에 따라 버퍼 크기 조정
        this.ParseMultiReadResponse(buffer, addresses.Length, dataType, readBuffer)

    /// 단일 주소 데이터 쓰기
    member this.Write(address: string, dataType: PlcDataSizeType, value: obj) : bool =
        try
            let frame = this.CreateWriteFrame(address, dataType, value)
            this.SendFrame(frame)
            let _ = this.ReceiveFrame(256)
            true
        with _ -> false