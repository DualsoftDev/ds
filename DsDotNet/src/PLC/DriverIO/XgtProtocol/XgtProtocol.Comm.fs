namespace XgtProtocol

open System
open System.Net
open System.Net.Sockets
open System.Text
open Dual.PLC.Common.FS


/// XGT PLC 통신 프로토콜 구현
type XgtEthernet(ip: string, port: int) =
    let mutable client: TcpClient option = None
    let mutable connected = false
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


    member this.Ip = ip
    member this.IsConnected = connected

    member this.Connect() =
        try
            let tcpClient = new TcpClient()
            tcpClient.Connect(IPAddress.Parse(ip), port)
            client <- Some tcpClient
            connected <- true
            true
        with _ -> false

    member this.ReConnect() =
        if not connected then this.Connect()
        else true

    member this.Disconnect() =
        match client with
        | Some tcpClient ->
            tcpClient.Close()
            connected <- false
            true
        | None -> false

    member private this.CreateReadFrame(address: string, dataType: DataType) =
        let device = address.Substring(1, 2)
        let addr = address.Substring(3).PadLeft(5, '0')
        let frame = Array.zeroCreate<byte> 38
        Array.Copy(Encoding.ASCII.GetBytes("LSIS-XGT"), 0, frame, 0, 8)
        frame.[12] <- 0xA0uy
        frame.[13] <- 0x33uy
        frame.[14] <- frameID
        frame.[16] <- 0x12uy
        frame.[20] <- 0x54uy
        frame.[22] <- 
            match dataType with
            | Bit -> 0x00uy
            | Byte -> 0x01uy
            | Word -> 0x02uy
            | DWord -> 0x03uy
            | LWord -> 0x04uy
        frame.[26] <- 0x01uy
        frame.[28] <- 0x08uy
        frame.[30] <- byte '%'
        frame.[31] <- byte device.[0]
        frame.[32] <- byte device.[1]
        for i in 0..4 do
            frame.[33 + i] <- byte addr.[i]
        frame

    /// 여러 주소를 한번에 읽기 위한 랜덤 리드 프레임 생성
    member private this.CreateMutiReadFrame(addresses: string[], dataType:DataType) : byte[] =
        if addresses.Length = 0 || addresses.Length > 16 then
            failwith "지원되는 주소 개수는 1 ~ 16 개입니다."

        let encodeVariable (addr: string) =
            let device = addr.Substring(0, 3)
            let addressFull = device + addr.Substring(3).PadLeft(5, '0')
            let bytes = Encoding.ASCII.GetBytes(addressFull)
            if bytes.Length <> 8 then failwith $"주소 길이 이상: {addr}"
            8, bytes

        let variableBlocks = addresses |> Array.map (fun (addr) -> encodeVariable addr)

        let bodyLength = 8 + (10 * addresses.Length) //   (8bytes(conifg) + (2bytes + 8bytes)  * addresses.Length
        let totalLength = 20 + bodyLength
        let frame = Array.zeroCreate<byte> totalLength

        // Header: "LSIS-XGT"
        let header = Encoding.ASCII.GetBytes("LSIS-XGT")
        Array.Copy(header, 0, frame, 0, header.Length)

        frame.[12] <- 0xA0uy
        frame.[13] <- 0x33uy
        frame.[14] <- frameID
        frame.[16] <- byte bodyLength 

           // 체크섬 설정
        let checksum =
            frame
            |> Seq.take 19
            |> Seq.fold (fun acc b -> acc + int b) 0
            |> fun sum -> byte (sum &&& 0xFF)

        frame.[19] <- checksum
        frame.[20] <- 0x54uy

        frame.[22] <- 
            match dataType with
            | Bit -> 0x00uy
            | Byte -> 0x01uy
            | Word -> 0x02uy
            | DWord -> 0x03uy
            | LWord -> 0x04uy

        frame.[26] <- byte variableBlocks.Length
        let mutable offset = 28
        for (lenBytes, varBytes) in variableBlocks do
            frame.[offset] <- 0x08uy
            Array.Copy(varBytes, 0, frame, offset+2, 8)
            offset <- offset + (2+lenBytes)

     
        frame

        /// 실제 데이터 읽기 구현 (단일 주소)
    member private this.CreateWriteFrame(address: string, dataType: DataType, value: byte[]) =
        let device = address.Substring(1, 2)
        let addr = address.Substring(3).PadLeft(5, '0')
        let frame = Array.zeroCreate<byte> (42 + value.Length)
        Array.Copy(Encoding.ASCII.GetBytes("LSIS-XGT"), 0, frame, 0, 8)
        frame.[12] <- 0xA0uy
        frame.[13] <- 0x33uy
        frame.[14] <- frameID
        frame.[16] <- byte (0x16 + value.Length)
        frame.[20] <- 0x58uy
        frame.[22] <- 
            match dataType with
            | Bit -> 0x00uy
            | Byte -> 0x01uy
            | Word -> 0x02uy
            | DWord -> 0x03uy
            | LWord -> 0x04uy
        frame.[26] <- 0x01uy
        frame.[28] <- 0x08uy
        frame.[30] <- byte '%'
        frame.[31] <- byte device.[0]
        frame.[32] <- byte device.[1]
        for i in 0..4 do
            frame.[33 + i] <- byte addr.[i]
        frame.[38] <- byte value.Length
        frame.[39] <- 0x00uy
        Array.Copy(value, 0, frame, 40, value.Length)
        frame


    member this.ReadData(address: string, dataType: DataType) : obj =
            let buffer = Array.zeroCreate<byte> 256
            try
                this.ReadData([|address|], dataType, buffer)
                match dataType with
                | Bit -> buffer.[0] = 1uy |> box
                | Byte -> buffer.[0] |> box
                | Word -> BitConverter.ToUInt16(buffer, 0) |> box
                | DWord -> BitConverter.ToUInt32(buffer, 0) |> box
                | LWord -> BitConverter.ToUInt64(buffer, 0) |> box
            with
            |  ex ->
                failwithf $"PLC 통신 오류: {ex.Message}"

    member this.ReadData(addresses: string[], dataType:DataType, readBuffer: byte[]) =
        match client with
        | Some tcpClient when connected ->
            let stream = tcpClient.GetStream()
            let frame = this.CreateMutiReadFrame(addresses, dataType)
            stream.Write(frame, 0, frame.Length)
            let buffer = Array.zeroCreate<byte> 256
            let bytesRead = stream.Read(buffer, 0, buffer.Length)

            let errorState = BitConverter.ToUInt16(buffer, 26)
            if errorState <> 0us then
                let errorCode = buffer.[26]
                let errorMsg = getXgtErrorDescription errorCode
                failwithf $"❌ PLC 응답 에러: 0x{errorCode:X2} - {errorMsg}"

            // 최소 응답 헤더 크기 검사 (32바이트 이상이어야 함)
            if bytesRead < 32 then  failwith "응답 데이터가 너무 짧습니다."
            // 응답 명령어 확인 (0x0055)
            if buffer.[20] <> 0x55uy then failwith "응답 명령어가 아닙니다." 

            let blockCnt = int buffer.[28] // 블록 개수
            let mutable srcOffset = 30
            let mutable dstOffset = 0

            for _ in 0 .. blockCnt-1 do   
                srcOffset <- srcOffset + 2
                Array.Copy(buffer, srcOffset, readBuffer, dstOffset, 8)
                dstOffset <- dstOffset + 8
                srcOffset <- srcOffset + 8

        |_-> failwith "PLC 연결이 되어 있지 않습니다."


    member this.WriteData(address: string, dataType: DataType, value: obj) : bool =
        match client with
        | Some tcpClient when connected ->
            try
                let stream = tcpClient.GetStream()
                let valueBytes =
                    match dataType with
                    | Bit -> [| if unbox<bool> value then 0x01uy else 0x00uy |]
                    | Byte -> [| unbox<byte> value |]
                    | Word -> BitConverter.GetBytes(unbox<uint16> value)
                    | DWord -> BitConverter.GetBytes(unbox<uint32> value)
                    | LWord -> BitConverter.GetBytes(unbox<uint64> value)
                let frame = this.CreateWriteFrame(address, dataType, valueBytes)
                stream.Write(frame, 0, frame.Length)
                let _ = stream.Read(Array.zeroCreate<byte> 256, 0, 256)
                true
            with _ -> false
        | _ -> false
