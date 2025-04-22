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

    member private _.BuildHeader(dataLength: int) =
        let header = ResizeArray<byte>()
        header.AddRange([| 0x50uy; 0x00uy |])                        // Subheader
        header.AddRange([| 0x00uy; 0xFFuy; 0xFFuy; 0x03uy; 0x00uy |]) // Network/PC/IO/Station
        header.AddRange(BitConverter.GetBytes(uint16 dataLength))   // Request data length
        header.AddRange(BitConverter.GetBytes(uint16 0x0010))       // Monitoring timer
        header

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

    member this.ReadWords(device: string, address: int, count: uint16) =
        let cmd =
            let hdr = this.BuildHeader(12)
            hdr.AddRange([| 0x01uy; 0x04uy; 0x00uy; 0x00uy |]) // Cmd/SubCmd
            this.WriteAddress(hdr, address)
            hdr.Add(this.GetDeviceCode(device))
            hdr.AddRange(BitConverter.GetBytes(count))
            hdr.ToArray()
        let res = this.SendAndReceive(cmd)
        if res.Length < 11 then failwith "Invalid response"
        let data = res.[11..]
        Array.init (int count) (fun i -> BitConverter.ToInt16(data, i * 2) |> int)

    member this.WriteWord(device: string, address: int, values: int[]) =
        let wordBytes = values |> Array.collect (fun x -> BitConverter.GetBytes(int16 x))
        let count = uint16 (wordBytes.Length / 2)
        let cmd =
            let hdr = this.BuildHeader(12 + wordBytes.Length)
            hdr.AddRange([| 0x01uy; 0x14uy; 0x00uy; 0x00uy |]) // Cmd/SubCmd
            this.WriteAddress(hdr, address)
            hdr.Add(this.GetDeviceCode(device))
            hdr.AddRange(BitConverter.GetBytes(count))
            hdr.AddRange(wordBytes)
            hdr.ToArray()
        let res = this.SendAndReceive(cmd)
        if res.[9] <> 0uy || res.[10] <> 0uy then failwith "Write error"

    member this.ReadBits(device: string, address: int, count: uint16) =
        let cmd =
            let hdr = this.BuildHeader(12)
            hdr.AddRange([| 0x01uy; 0x04uy; 0x01uy; 0x00uy |]) // Cmd/SubCmd
            this.WriteAddress(hdr, address)
            hdr.Add(this.GetDeviceCode(device))
            hdr.AddRange(BitConverter.GetBytes(count))
            hdr.ToArray()
        let res = this.SendAndReceive(cmd)
        if res.Length < 11 then failwith "Invalid response"
        let data = res.[11..]
        Array.init (int count) (fun i -> int (data.[i] &&& 0x01uy))

    member this.WriteBits(device: string, address: int, values: int[]) =
        let bitBytes = values |> Array.map (fun x -> if x <> 0 then 0x10uy else 0x00uy)
        let count = uint16 bitBytes.Length
        let cmd =
            let hdr = this.BuildHeader(12 + bitBytes.Length)
            hdr.AddRange([| 0x01uy; 0x14uy; 0x01uy; 0x00uy |]) // Cmd/SubCmd
            this.WriteAddress(hdr, address)
            hdr.Add(this.GetDeviceCode(device))
            hdr.AddRange(BitConverter.GetBytes(count))
            hdr.AddRange(bitBytes)
            hdr.ToArray()
        let res = this.SendAndReceive(cmd)
        if res.[9] <> 0uy || res.[10] <> 0uy then failwith "Bit write error"

    /// MELSEC 랜덤 리드 구현 - DWord 단위 배치 기반
    member this.ReadDWordRandom(batches: DWBatch[]) : byte[] =
        let deviceInfos =
            batches
            |> Array.map (fun b ->
                let offset = b.BatchAddressOffset
                let code = this.GetDeviceCode(b.BatchAddressHead)
                (code, offset))

        let count = uint16 deviceInfos.Length
        let hdr = this.BuildHeader(6 + deviceInfos.Length * 4)
        hdr.AddRange([| 0x03uy; 0x04uy; 0x00uy; 0x00uy |]) // Cmd/SubCmd: 0403
        hdr.AddRange(BitConverter.GetBytes(count)) // Word count
        hdr.AddRange(BitConverter.GetBytes(uint16 0)) // DWord count

        for (code, address) in deviceInfos do
            this.WriteAddress(hdr, address)
            hdr.Add(code)

        let cmd = hdr.ToArray()
        let res = this.SendAndReceive(cmd)
        if res.Length < 11 then failwith "Invalid random response"
        res.[11..]