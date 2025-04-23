namespace MelsecProtocol

open System
open System.IO
open System.Net.Sockets
open System.Collections.Generic
open Dual.PLC.Common.FS

/// MELSEC Ethernet 통신 구현 (3E 프레임, 24비트 주소 대응)
type MxEthernet(ip: string, port: int, timeoutMs: int) =
    let client = new TcpClient()
    do
        client.SendTimeout <- timeoutMs
        client.ReceiveTimeout <- timeoutMs
        client.Connect(ip, port)
    let stream = client.GetStream()



    /// MELSEC MC 3E 랜덤 리드 명령 구성 (F# 버전)
    let buildRandomReadCommand (devices: (byte * int)[]) : byte[] =
        use ms = new MemoryStream()
        use w = new BinaryWriter(ms)

        // Header
        w.Write(uint16 0x0050)   // Subheader
        w.Write(byte 0x00)       // Network No.
        w.Write(byte 0xFF)       // PC No.
        w.Write(uint16 0x03FF)   // I/O No.
        w.Write(byte 0x00)       // Station No.

        use body = new MemoryStream()
        use bw = new BinaryWriter(body)

        bw.Write(uint16 0x0010)  // Monitoring Timer
        bw.Write(uint16 0x0403)  // Command
        bw.Write(uint16 0x0000)  // Subcommand

        bw.Write(byte 0)                // Word count
        bw.Write(byte devices.Length)   // DWord count

        for (code, addr) in devices do
            bw.Write(byte (addr &&& 0xFF))         // LSB
            bw.Write(byte ((addr >>> 8) &&& 0xFF)) // MID
            bw.Write(byte ((addr >>> 16) &&& 0xFF))// MSB
            bw.Write(code)                         // Device code

        let bodyBytes = body.ToArray()
        w.Write(uint16 bodyBytes.Length) // Body length
        w.Write(bodyBytes)               // Body

        ms.ToArray()


    interface IDisposable with
        member _.Dispose() =
            stream.Dispose()
            client.Close()

    member _.Close() =
        stream.Close()
        client.Close()

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


    member _.GetDeviceCode(device: string) : byte =
        match MxDevice.Create(device) with
        | Some v -> byte v
        | _ -> raise (ArgumentException($"Unsupported device: {device}"))

    member private _.WriteAddress(b: ResizeArray<byte>, address: int) =
        b.AddRange([|
            byte (address &&& 0xFF)
            byte ((address >>> 8) &&& 0xFF)
            byte ((address >>> 16) &&& 0xFF)
        |])
        
    member private _.BuildHeader() =
        let header = ResizeArray<byte>()
        header.AddRange([| 0x50uy; 0x00uy |])                         // Subheader
        header.AddRange([| 0x00uy; 0xFFuy; 0xFFuy; 0x03uy; 0x00uy |]) // Network/PC/IO/Station
        header
        
    member this.WriteWord(device: string, address: int, values: int[]) =
        let wordBytes = values |> Array.collect (fun x -> BitConverter.GetBytes(int16 x))
        let count = uint16 (wordBytes.Length / 2)
        let cmd =
            let hdr = this.BuildHeader()
            hdr.AddRange(BitConverter.GetBytes(uint16 (12 + wordBytes.Length)))   // Request data length
            hdr.AddRange(BitConverter.GetBytes(uint16 0x0010))           // Monitoring timer

            hdr.AddRange([| 0x01uy; 0x14uy; 0x00uy; 0x00uy |]) // Cmd/SubCmd
            this.WriteAddress(hdr, address)
            hdr.Add(this.GetDeviceCode(device))
            hdr.AddRange(BitConverter.GetBytes(count))
            hdr.AddRange(wordBytes)
            hdr.ToArray()
        let res = this.SendAndReceive(cmd)
        if res.[9] <> 0uy || res.[10] <> 0uy then failwith "Write error"

    member this.WriteBits(device: string, address: int, values: int[]) =
        let bitBytes = values |> Array.map (fun x -> if x <> 0 then 0x10uy else 0x00uy)
        let count = uint16 bitBytes.Length
        let cmd =
            let hdr = this.BuildHeader()
            hdr.AddRange(BitConverter.GetBytes(uint16 (12 + bitBytes.Length)))   // Request data length
            hdr.AddRange(BitConverter.GetBytes(uint16 0x0010))           // Monitoring timer
            
            hdr.AddRange([| 0x01uy; 0x14uy; 0x01uy; 0x00uy |]) // Cmd/SubCmd
            this.WriteAddress(hdr, address)
            hdr.Add(this.GetDeviceCode(device))
            hdr.AddRange(BitConverter.GetBytes(count))
            hdr.AddRange(bitBytes)
            hdr.ToArray()
        let res = this.SendAndReceive(cmd)
        if res.[9] <> 0uy || res.[10] <> 0uy then failwith "Bit write error"

    /// MELSEC 랜덤 리드 구현 - DWord 단위 배치 기반
    member this.ReadDWordRandom(batch: DWBatch) : byte[] =
    // 중복 제거된 LWord 주소만 추출하여 읽기 요청
        let deviceInfos =
            batch.Tags
            |> Seq.distinctBy (fun tag -> tag.DWordTag)
            |> Seq.toArray
            |> Array.map (fun t ->
                let code = byte t.DeviceCode
                let offset = t.BitOffset / 16
                (code, offset))

        //let head = this.BuildHeader()
        //let body = ResizeArray<byte>()

        //body.AddRange(BitConverter.GetBytes(uint16 0x0010))      // Monitoring timer
        //body.AddRange([| 0x03uy; 0x04uy; 0x00uy; 0x00uy |])      // Cmd/SubCmd: 0403
        //body.Add(0x00uy)                                         // Word count
        //body.Add(byte deviceInfos.Length)                        // DWord count

        //for (code, address) in deviceInfos do
        //    this.WriteAddress(body, address)
        //    body.Add(code)

        //head.AddRange(BitConverter.GetBytes(uint16 (body.Count)))      // body length
        //head.AddRange(body)        
        //let cmd = head.ToArray()


        let cmd = buildRandomReadCommand(deviceInfos)
        let res = this.SendAndReceive(cmd)

        if res.Length <> 11 + deviceInfos.Length * 4
        then
            failwith "Invalid random response"
        else 
            res.[11..]