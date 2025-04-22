namespace MelsecProtocol

open System
open System.IO
open System.Net.Sockets
open System.Collections.Generic
open System.Text
open System.Text.RegularExpressions
open Dual.PLC.Common.FS

/// MELSEC Ethernet 통신 구현
/// 3E Frame 기반, Word/Bit, K포맷, 랜덤 DWord 읽기 지원

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
        header.AddRange([| 0x50uy; 0x00uy |])
        header.AddRange([| 0x00uy; 0xFFuy; 0xFFuy; 0x03uy; 0x00uy |])
        header.AddRange(BitConverter.GetBytes(uint16 dataLength))
        header.AddRange(BitConverter.GetBytes(uint16 0x0010))
        header

    member _.GetDeviceCode(device: string) : byte =
        match MelsecProtocolCore.deviceMap.TryGetValue(device.ToUpper()) with
        | true, dev -> byte dev
        | _ -> raise (ArgumentException($"Unsupported device: {device}"))

    member this.ReadWords(device: string, address: int, count: uint16) =
        let cmd =
            let hdr = this.BuildHeader(12)
            hdr.AddRange([| 0x01uy; 0x04uy; 0x00uy; 0x00uy |])
            hdr.AddRange(BitConverter.GetBytes(uint32 address).[0..2])
            hdr.Add(this.GetDeviceCode(device))
            hdr.AddRange(BitConverter.GetBytes(count))
            hdr.ToArray()
        let res = this.SendAndReceive(cmd)
        if res.Length < 11 then failwith "Invalid response"
        let data = res.[11..]
        Array.init (int count) (fun i -> BitConverter.ToInt16(data, i * 2) |> int)

    member this.WriteWord(device: string, startAddr: int, values: int[]) =
        let wordBytes = values |> Array.collect (fun x -> BitConverter.GetBytes(int16 x))
        let cmd =
            let count = uint16 (wordBytes.Length / 2)
            let hdr = this.BuildHeader(12 + wordBytes.Length)
            hdr.AddRange([| 0x01uy; 0x14uy; 0x00uy; 0x00uy |])
            hdr.AddRange(BitConverter.GetBytes(uint32 startAddr).[0..2])
            hdr.Add(this.GetDeviceCode(device))
            hdr.AddRange(BitConverter.GetBytes(count))
            hdr.AddRange(wordBytes)
            hdr.ToArray()
        let res = this.SendAndReceive(cmd)
        if res.[9] <> 0uy || res.[10] <> 0uy then failwith "Write error"

    member this.ReadBits(device: string, address: int, count: uint16) =
        let cmd =
            let hdr = this.BuildHeader(12)
            hdr.AddRange([| 0x01uy; 0x04uy; 0x01uy; 0x00uy |])
            hdr.AddRange(BitConverter.GetBytes(uint32 address).[0..2])
            hdr.Add(this.GetDeviceCode(device))
            hdr.AddRange(BitConverter.GetBytes(count))
            hdr.ToArray()
        let res = this.SendAndReceive(cmd)
        if res.Length < 11 then failwith "Invalid response"
        let data = res.[11..]
        Array.init (int count) (fun i -> int (data.[i] &&& 0x01uy))

    member this.WriteBits(device: string, address: int, values: int[]) =
        let bitBytes = values |> Array.map (fun x -> if x <> 0 then 0x10uy else 0x00uy)
        let cmd =
            let count = uint16 bitBytes.Length
            let hdr = this.BuildHeader(12 + bitBytes.Length)
            hdr.AddRange([| 0x01uy; 0x14uy; 0x01uy; 0x00uy |])
            hdr.AddRange(BitConverter.GetBytes(uint32 address).[0..2])
            hdr.Add(this.GetDeviceCode(device))
            hdr.AddRange(BitConverter.GetBytes(count))
            hdr.AddRange(bitBytes)
            hdr.ToArray()
        let res = this.SendAndReceive(cmd)
        if res.[9] <> 0uy || res.[10] <> 0uy then failwith "Bit write error"

