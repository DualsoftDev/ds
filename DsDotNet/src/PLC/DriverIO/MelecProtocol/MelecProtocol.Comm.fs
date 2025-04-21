namespace MelsecProtocol

open System
open System.IO
open System.Net.Sockets

// Ethernet 단순 통신 래퍼
type MxEthernet(ip: string, port: int, timeoutMs: int) =
    let client = new TcpClient()
    do
        client.SendTimeout <- timeoutMs
        client.ReceiveTimeout <- timeoutMs
        client.Connect(ip, port)
    let stream = client.GetStream()

    interface IDisposable with
        member _.Dispose() =
            stream.Dispose()
            client.Close()

    member _.Close() =
        stream.Close()
        client.Close()

    member private _.SendAndReceive(data: byte[]) : byte[] =
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

    member private _.BuildHeader(dataLength: int) =
        let list = ResizeArray<byte>()
        list.AddRange([| 0x50uy; 0x00uy |]) // 3E Frame
        list.AddRange([| 0x00uy; 0xFFuy; 0xFFuy; 0x03uy; 0x00uy |])
        list.AddRange(BitConverter.GetBytes(uint16 dataLength))
        list.AddRange(BitConverter.GetBytes(uint16 0x0010)) // CPU Timer
        list

    /// 디바이스 문자열 → 디바이스 코드 byte
    member private _. GetDeviceCode (device: string) : byte =
        match MelsecProtocolCore.deviceMap.TryGetValue(device.ToUpper()) with
        | true, dev -> byte dev
        | _ -> raise (ArgumentException($"Unsupported device: {device}"))

    member private this.BuildReadCommand(device: string, address: int, count: uint16) =
        let header = this.BuildHeader(12)
        header.AddRange([| 0x01uy; 0x04uy |]) // Command: batch read
        header.AddRange([| 0x00uy; 0x00uy |]) // Subcommand
        header.AddRange(BitConverter.GetBytes(address &&& 0xFFFFFF).[0..2])
        header.Add(this.GetDeviceCode(device))
        header.AddRange(BitConverter.GetBytes(count))
        header.ToArray()

    member this.ReadWords(device: string, address: int, count: uint16) =
        let cmd = this.BuildReadCommand(device, address, count)
        let res = this.SendAndReceive(cmd)
        if res.Length < 11 then failwith "Invalid response"
        let data = res.[11..]
        Array.init (int count) (fun i -> BitConverter.ToInt16(data, i * 2) |> int)

    member this.WriteWord(device: string, startAddr: int, values: int[]) =
        let wordCount = values.Length
        let header = this.BuildHeader(12 + wordCount * 2)
        header.AddRange([| 0x01uy; 0x14uy |]) // Command: batch write
        header.AddRange([| 0x00uy; 0x00uy |])
        header.AddRange(BitConverter.GetBytes(startAddr).[0..2])
        header.Add(this.GetDeviceCode(device))
        header.AddRange(BitConverter.GetBytes(uint16 wordCount))
        for v in values do
            header.AddRange(BitConverter.GetBytes(int16 v))
        let res = this.SendAndReceive(header.ToArray())
        if res.[9] <> 0uy || res.[10] <> 0uy then failwith "Write error"
        ()
