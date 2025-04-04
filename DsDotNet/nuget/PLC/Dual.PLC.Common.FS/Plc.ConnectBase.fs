namespace Dual.PLC.Common.FS

open System
open System.Net.Sockets

[<AbstractClass>]
type PlcEthernetBase(ip: string, port: int) =
    let mutable client: TcpClient option = None
    let mutable connected = false

    member _.Ip = ip
    member _.Port = port
    member _.IsConnected = connected

    member this.Connect() =
        try
            let tcpClient = new TcpClient()
            tcpClient.Connect(ip, port)
            client <- Some tcpClient
            connected <- true
            true
        with _ -> false

    member this.Disconnect() =
        match client with
        | Some tcpClient ->
            tcpClient.Close()
            connected <- false
            true
        | None -> false

    member this.GetStream() =
        match client with
        | Some tcp when connected -> tcp.GetStream()
        | _ -> failwith "PLC 연결이 되어 있지 않습니다."

    member this.SendFrame(frame: byte[]) =
        let stream = this.GetStream()
        stream.Write(frame, 0, frame.Length)

    member this.ReceiveFrame(bufferSize: int) : byte[] =
        let stream = this.GetStream()
        let buffer = Array.zeroCreate<byte> bufferSize
        let _ = stream.Read(buffer, 0, buffer.Length)
        buffer

    /// 각 제조사마다 구현해야 할 부분
    abstract member CreateReadFrame: address:string * dataType:PlcDataSizeType -> byte[]
    abstract member CreateWriteFrame: address:string * dataType:PlcDataSizeType * value:obj -> byte[]
    abstract member ParseReadResponse: byte[] * PlcDataSizeType -> obj

    member this.Read(address: string, dataType: PlcDataSizeType) : obj =
        let frame = this.CreateReadFrame(address, dataType)
        this.SendFrame(frame)
        let buffer = this.ReceiveFrame(256)
        this.ParseReadResponse(buffer, dataType)

    member this.Write(address: string, dataType: PlcDataSizeType, value: obj) : bool =
        try
            let frame = this.CreateWriteFrame(address, dataType, value)
            this.SendFrame(frame)
            let _ = this.ReceiveFrame(256)
            true
        with _ -> false
