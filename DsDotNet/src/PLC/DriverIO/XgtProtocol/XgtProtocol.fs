namespace XgtProtocol.FS

open System
open System.Net
open System.Net.Sockets
open System.Text

type DataType =
    | Bit
    | Byte
    | Word
    | DWord
    | LWord

type XgtProtocol(ip: string, port: int) =
    let mutable client: TcpClient option = None
    let mutable connected = false
    let frameID = byte (ip.Split('.').[3] |> int)

    
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
        if not(connected) 
        then this.Connect()  
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
        frame.[38] <- byte (value.Length)
        frame.[39] <- 0x00uy
        Array.Copy(value, 0, frame, 40, value.Length)
        frame

    member this.ReadData(address: string, dataType: DataType) =
        match client with
        | Some tcpClient when connected ->
            try
                let stream = tcpClient.GetStream()
                let frame = this.CreateReadFrame(address, dataType)
                stream.Write(frame, 0, frame.Length)
                let buffer = Array.zeroCreate<byte> 256
                let bytesRead = stream.Read(buffer, 0, buffer.Length)
                if bytesRead > 32 then
                    match dataType with
                    | Bit -> buffer.[32] = 1uy |> box
                    | Byte -> buffer.[32] |> box
                    | Word -> BitConverter.ToUInt16(buffer, 32) |> box
                    | DWord -> BitConverter.ToUInt32(buffer, 32) |> box
                    | LWord -> BitConverter.ToUInt64(buffer, 32) |> box
                else
                    null
            with _ -> null
        | _ -> null

    member this.WriteData(address: string, dataType: DataType, value: obj) =
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
                let response = Array.zeroCreate<byte> 256
                let _ = stream.Read(response, 0, response.Length)
                true
            with _ -> false
        | _ -> false
