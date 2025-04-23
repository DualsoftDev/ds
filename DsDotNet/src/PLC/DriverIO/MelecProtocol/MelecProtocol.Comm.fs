namespace MelsecProtocol

open System
open System.IO
open System.Net.Sockets
open System.Collections.Generic

/// MELSEC Ethernet 통신 구현 (3E 프레임, 24비트 주소 대응)
type MxEthernet(ip: string, port: int, timeoutMs: int) =
    // TCP 클라이언트 초기화 및 연결 설정
    let client = new TcpClient()
    do
        client.SendTimeout <- timeoutMs
        client.ReceiveTimeout <- timeoutMs
        client.Connect(ip, port)
    let stream = client.GetStream()

    /// 공통 헤더 생성 함수
    let buildHeader() =
        ResizeArray([|
            0x50uy; 0x00uy;             // Subheader (0x5000)
            0x00uy; 0xFFuy;             // Network No. (0x00), PC No. (0xFF)
            0xFFuy; 0x03uy;             // I/O No. (0x03FF)
            0x00uy                      // Station No. (0x00)
        |])

    /// 디바이스 주소 및 코드 기록 (24비트 주소 대응)
    let writeDeviceEntry(b: ResizeArray<byte>, code: byte, address: int) =
        b.AddRange([|
            byte (address &&& 0xFF)            // Address LSB
            byte ((address >>> 8) &&& 0xFF)    // Address Middle Byte
            byte ((address >>> 16) &&& 0xFF)   // Address MSB
            code                               // Device Code (e.g., 0xA8 for D)
        |])

    /// 랜덤 리드 명령어 생성 함수
    let buildRandomReadCommand (devices: (byte * int)[]) : byte[] =
        use body = new MemoryStream()
        use bw = new BinaryWriter(body)

        bw.Write(uint16 0x0010)               // Monitoring Timer (16진수 0x0010)
        bw.Write(uint16 0x0403)               // Command (0x0403: Random Read)
        bw.Write(uint16 0x0000)               // Subcommand (0x0000)

        bw.Write(byte 0)                      // Word Count (0)
        bw.Write(byte devices.Length)         // DWord Count

        let deviceEntries = ResizeArray()
        for (code, addr) in devices do
            writeDeviceEntry(deviceEntries, code, addr)
        bw.Write(deviceEntries.ToArray())

        use ms = new MemoryStream()
        use w = new BinaryWriter(ms)

        let header = buildHeader()
        w.Write(header.ToArray())             // 헤더 작성
        w.Write(uint16 body.Length)           // 데이터 길이
        w.Write(body.ToArray())               // 바디 작성

        ms.ToArray()

    /// 쓰기 명령어 생성 함수 (비트 또는 워드)
    let buildWriteCommand (device: MxDevice, address: int, values: int[], isBit: bool) =
        let dataBytes =
            if isBit then
                values |> Array.map (fun x -> if x <> 0 then 0x10uy else 0x00uy)
            else
                values |> Array.collect BitConverter.GetBytes

        let count =
            if isBit then uint16 dataBytes.Length
            else uint16 (dataBytes.Length / 2)

        let cmd = buildHeader()
        cmd.AddRange(BitConverter.GetBytes(uint16 (12 + dataBytes.Length))) // 데이터 길이
        cmd.AddRange(BitConverter.GetBytes(uint16 0x0010))                  // Monitoring Timer
        cmd.AddRange([| 0x01uy; 0x14uy; (if isBit then 0x01uy else 0x00uy); 0x00uy |]) // Command 및 Subcommand

        writeDeviceEntry(cmd, byte device, address)                         // 디바이스 정보
        cmd.AddRange(BitConverter.GetBytes(count))                          // 쓰기 카운트
        cmd.AddRange(dataBytes)                                             // 데이터

        cmd.ToArray()

    /// 연속 리드 명령어 생성 함수 (비트 또는 워드)
    let buildSequentialReadCommand (device: MxDevice, address: int, count: int, isBit: bool) =
        use body = new MemoryStream()
        use bw = new BinaryWriter(body)

        bw.Write(uint16 0x0010)               // Monitoring Timer
        bw.Write(uint16 0x0401)               // Command (0x0401: Sequential Read)
        bw.Write(uint16 (if isBit then 0x0001 else 0x0000)) // Subcommand

        let devicePart = ResizeArray()
        writeDeviceEntry(devicePart, byte device, address)
        bw.Write(devicePart.ToArray())        // 디바이스 정보

        bw.Write(uint16 count)                // 읽을 포인트 수

        use ms = new MemoryStream()
        use w = new BinaryWriter(ms)

        let header = buildHeader()
        w.Write(header.ToArray())             // 헤더 작성
        w.Write(uint16 body.Length)           // 데이터 길이
        w.Write(body.ToArray())               // 바디 작성

        ms.ToArray()

    /// 명령어 전송 및 응답 수신
    member _.SendAndReceive(data: byte[]) : byte[] =
        stream.Write(data, 0, data.Length)
        stream.Flush()
        use ms = new MemoryStream()
        let buff = Array.zeroCreate 1024
        let mutable bytesRead = 0
        while stream.DataAvailable || bytesRead = 0 do
            let sz = stream.Read(buff, 0, buff.Length)
            if sz = 0 then raise <| IOException("No data received")
            ms.Write(buff, 0, sz)
            bytesRead <- sz
        ms.ToArray()

    interface IDisposable with
        member _.Dispose() =
            stream.Dispose()
            client.Close()

    member _.Close() =
        stream.Close()
        client.Close()

    member this.ReadDWordRandom(batch: DWBatch) : byte[] =
        let deviceInfos =
            batch.Tags
            |> Seq.distinctBy (fun t -> t.DWordTag)
            |> Seq.map (fun t ->
                let code = byte t.DeviceCode
                let offset = if MxDevice.IsBit(t.DeviceCode) then t.BitOffset else t.BitOffset / 16
                (code, offset))
            |> Seq.toArray

        let cmd = buildRandomReadCommand(deviceInfos)
        let res = this.SendAndReceive(cmd)

        if res.Length <> 11 + deviceInfos.Length * 4 then
            failwith $"Invalid random response\r\n{batch.BatchToText()}"
        else
            res.[11..]

    member this.WriteWord(device: MxDevice, address: int, value: int) =
        let cmd = buildWriteCommand(device, address, [|value|], false)
        let res = this.SendAndReceive(cmd)
        if res.[9] <> 0uy || res.[10] <> 0uy then failwith "Write error"

    member this.WriteBit(device: MxDevice, address: int, value: int) =
        let cmd = buildWriteCommand(device, address, [|value|], true)
        let res = this.SendAndReceive(cmd)
        if res.[9] <> 0uy || res.[10] <> 0uy then failwith "Bit write error"

    member this.ReadWords(device: MxDevice, address: int, count: int) : int[] =
        let cmd = buildSequentialReadCommand(device, address, count, false)
        let res = this.SendAndReceive(cmd)
        if res.Length < 11 + count * 2 then failwith "Invalid word read response"
        [| for i in 0 .. count - 1 -> BitConverter.ToUInt16(res, 11 + i * 2) |> int |]

    member this.ReadBits(device: MxDevice, address: int, count: int) : bool[] =
        let cmd = buildSequentialReadCommand(device, address, count, true)
        let res = this.SendAndReceive(cmd)
        if res.Length < 11 + count then failwith "Invalid bit read response"
        [| for i in 0 .. count - 1 -> if res.[11 + i] &&& 0x01uy = 0x01uy then true else false |]
