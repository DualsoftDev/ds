namespace MelsecProtocol

open System
open System.IO
open System.Net.Sockets
open System.Collections.Generic
open System.Net

/// MELSEC Ethernet 통신 구현 (3E 프레임, 24비트 주소 대응)
type MxEthernet(ip: string, port: int, timeoutMs: int, isUDP:bool) =
    let clientTcpOpt = if not isUDP then Some(new TcpClient()) else None
    let clientUdpOpt = if isUDP then Some(new UdpClient()) else None

    do
        match clientTcpOpt, clientUdpOpt with
        | Some tcp, _ ->
            tcp.SendTimeout <- timeoutMs
            tcp.ReceiveTimeout <- timeoutMs
            tcp.Connect(ip, port)
        | _, Some udp ->
            udp.Client.SendTimeout <- timeoutMs
            udp.Client.ReceiveTimeout <- timeoutMs
            udp.Connect(ip, port)
        | _ -> ()

    let stream =
        match clientTcpOpt with
        | Some tcp -> tcp.GetStream() :> Stream
        | None -> new MemoryStream() :> Stream  // dummy; not used in UDP

    // 공통 헤더 생성 함수
    let buildHeader() =
        ResizeArray([|
            0x50uy; 0x00uy;
            0x00uy; 0xFFuy;
            0xFFuy; 0x03uy;
            0x00uy
        |])

    let writeDeviceEntry(b: ResizeArray<byte>, code: byte, address: int) =
        b.AddRange([|
            byte (address &&& 0xFF)
            byte ((address >>> 8) &&& 0xFF)
            byte ((address >>> 16) &&& 0xFF)
            code
        |])

    // 이하 buildRandomReadCommand, buildWriteCommand, buildSequentialReadCommand는 그대로 사용

    // 나머지 Read/Write 함수들은 기존 코드 그대로 유지

    /// 공통 헤더 생성 함수
    let buildHeader() =
        ResizeArray([|
            0x50uy; 0x00uy;             // Subheader (0x5000)
            0x00uy; 0xFFuy;             // Network No. (0x00), PC No. (0xFF)
            0xFFuy; 0x03uy;             // I/O No. (0x03FF)
            0x00uy                      // Station No. (0x00)
        |])

        //Built-in Ethernet
    //let buildHeader() =
    //    ResizeArray([|
    //        0x50uy; 0x00uy;             // Subheader
    //        0x00uy; 0xFFuy;             // Network No., PC No.
    //        0x00uy; 0x00uy;             // I/O No. (Built-in Ethernet)
    //        0x00uy                      // Station No.
    //    |])


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


    /// 랜덤 라이트 명령어 생성 함수
    let buildRandomWriteCommand (devices: (MxDevice * int * int)[], isBit:bool) : byte[] =  //isBit 구현필요
        use body = new MemoryStream()
        use bw = new BinaryWriter(body)

        bw.Write(uint16 0x0010)               // Monitoring Timer
        bw.Write(uint16 0x1402)               // Command (0x1402: Random Write)
        bw.Write(uint16 0x0000)               // Subcommand

        bw.Write(byte devices.Length)        // Word Count
        bw.Write(byte 0)                     // DWord Count (0)

        let deviceEntries = ResizeArray()
        for (code, addr, value) in devices do
            writeDeviceEntry(deviceEntries, byte code, addr)
            deviceEntries.AddRange(BitConverter.GetBytes(uint16 value))
        bw.Write(deviceEntries.ToArray())

        use ms = new MemoryStream()
        use w = new BinaryWriter(ms)

        let header = buildHeader()
        w.Write(header.ToArray())             // 헤더 작성
        //w.Write(uint16 body.Length)           // 데이터 길이
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

    member _.SendAndReceive(data: byte[]) : byte[] =
        match clientTcpOpt, clientUdpOpt with
        | Some tcp, _ ->
            let ns = tcp.GetStream()
            ns.Write(data, 0, data.Length)
            ns.Flush()

            use ms = new MemoryStream()
            let buff = Array.zeroCreate 1024
            let mutable bytesRead = 0

            while (tcp.Available > 0) || bytesRead = 0 do
                let sz = ns.Read(buff, 0, buff.Length)
                if sz = 0 then raise <| IOException("No data received")
                ms.Write(buff, 0, sz)
                bytesRead <- sz
            ms.ToArray()

        | _, Some udp ->

            let ep = IPEndPoint(IPAddress.Any, 0)
            let mutable remote = ep
            udp.Send(data, data.Length) |> ignore
            let res = udp.Receive(&remote)
            res

        | _ -> failwith "No client initialized"

    interface IDisposable with
        member _.Dispose() =
            stream.Dispose()
            match clientTcpOpt, clientUdpOpt with
            | Some tcp, _ -> tcp.Close()
            | _, Some udp -> udp.Close()
            | _ -> ()

    member _.Close() =
        stream.Close()
        match clientTcpOpt, clientUdpOpt with
        | Some tcp, _ -> tcp.Close()
        | _, Some udp -> udp.Close()
        | _ -> ()


    member this.ReadDWordRandom(batch: DWBatch) : byte[] =
        let deviceInfos =
            batch.Tags
            |> Seq.distinctBy (fun t -> t.DWordTag)
            |> Seq.map (fun t ->
                let code = byte t.DeviceCode
                let offset = if MxDevice.IsBit(t.DeviceCode) then t.BitOffset/32*32 else t.BitOffset / 16
                (code, offset))
            |> Seq.toArray

        let cmd = buildRandomReadCommand(deviceInfos)
        let res = this.SendAndReceive(cmd)

        if res.Length <> 11 + deviceInfos.Length * 4 then
            failwith $"Invalid random response\r\n{batch.BatchToText()}"
        else
            res.[11..]

            
    member this.WriteWordRandom(devices: (MxDevice * int * int)[]) =
        let cmd = buildRandomWriteCommand(devices, false)
        let res = this.SendAndReceive(cmd)
        if res.Length < 11 || res.[9] <> 0uy || res.[10] <> 0uy then
            failwith "WriteWordRandom error"

    member this.WriteBitRandom(devices: (MxDevice * int * int)[]) =
        let cmd = buildRandomWriteCommand(devices, true)
        let res = this.SendAndReceive(cmd)
        if res.Length < 11 || res.[9] <> 0uy || res.[10] <> 0uy then
            failwith "WriteBitRandom error"

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
