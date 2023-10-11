(* IO.Core using Zero MQ *)

namespace IO.Core
open System
open System.Threading
open NetMQ
open NetMQ.Sockets
open System.IO
open Newtonsoft.Json
open Dual.Common.Core.FS
open System.Collections.Generic

module Zmq =
    type DataTypeConverter() =
        static member ToBytes<'T> (values: 'T[]) =
            values
            |> Array.collect (fun value ->
                match box value with
                | :? byte as v -> [| v |] // 이미 바이트이므로 배열로 변환
                | :? uint16 as v -> System.BitConverter.GetBytes(v)
                | :? uint32 as v -> System.BitConverter.GetBytes(v)
                | :? uint64 as v -> System.BitConverter.GetBytes(v)
                | :? int32  as v -> System.BitConverter.GetBytes(v)
                | _ -> failwithf "Type %O is not supported" typeof<'T>)

        static member BytesToTypeArray<'T> (bytes: byte[]) : 'T[] =
            if sizeof<'T> = 0 || bytes.Length % sizeof<'T> <> 0 then
                failwithf "The length of the byte array should be a multiple of %d for %O conversion." sizeof<'T> typeof<'T>

            Array.init (bytes.Length / sizeof<'T>) (fun i ->
                let value : obj = 
                    match typedefof<'T> with
                    | t when t = typedefof<uint16> -> box (System.BitConverter.ToUInt16(bytes, i * sizeof<'T>))
                    | t when t = typedefof<int32> -> box (System.BitConverter.ToInt32(bytes, i * sizeof<'T>))
                    | _ -> failwithf "Type %O is not supported for conversion from bytes" typeof<'T>
                value)
            |> Array.map (fun v -> unbox<'T> v)  // unbox to the target type

    //let bytesToInts (bytes: byte[]) =
    //    if bytes.Length % 4 <> 0 then
    //        failwith "The length of the byte array should be a multiple of 4 for int32 conversion."

    //    Array.init (bytes.Length / 4) (fun i ->
    //        System.BitConverter.ToInt32(bytes, i * 4))


    type Server(memoryFilesSpec:IOSpec, cancellationToken:CancellationToken) =
        let port = memoryFilesSpec.ServicePort
        let dir = memoryFilesSpec.Location
        let streams = new Dictionary<string, BufferManager>()
        do
            for mfs in memoryFilesSpec.Files do
                let stream = mfs.InitiaizeFile(dir)
                streams.Add(mfs.Name, new BufferManager(stream))

        let (|AddressPattern|_|) (str: string) =
            let memTypes = streams.Keys.JoinWith "|"
            let dataTypes = "x|b|w|d|l"
            let pattern = sprintf "([%s])([%s])(\d+)" memTypes dataTypes
            match str with
            | RegexPattern pattern [m; d; Int32Pattern offset] -> Some(AddressSpec(m, d, offset))
            | _ -> None

        let (|AddressAssignPattern|_|) (str: string) =
            match str with
            | RegexPattern "(\w+)=(\w+)" [AddressPattern addr; value] ->
                Some(addr, value)
            | _ -> None
            

        member private x.handleRequest (respSocket:ResponseSocket) : IIOResult =
            let request = respSocket.ReceiveFrameString()
            logDebug $"Handling request: {request}"
            let tokens = request.Split(' ', StringSplitOptions.RemoveEmptyEntries) |> Array.ofSeq
            let command = tokens[0]
            let args = tokens[1..] |> map(fun s -> s.ToLower())
            let readAddress(address:string) : obj =
                match address with
                | AddressPattern addr ->
                    let ap = addr
                    let offset = ap.Offset
                    let stream = streams[ap.Name]

                    match ap.Type with
                    | "x" -> stream.readBit(offset) :> obj
                    | "b" -> stream.readU8(offset)
                    | "w" -> stream.readU16(offset)
                    | "d" -> stream.readU32(offset)
                    | "l" -> stream.readU64(offset)
                    | _ -> failwithf($"Unknown data type : {ap.Type}")
                | _ -> failwithf($"Unknown address pattern : {address}")

            let writeAddressWithValue(addressWithAssignValue:string) =
                match addressWithAssignValue with
                | AddressAssignPattern (addressPattern, value) ->
                    let ap = addressPattern
                    let offset = ap.Offset
                    let stream = streams[ap.Name]

                    match ap.Type with
                    | "x" -> stream.writeBit(offset, bool.Parse(value))
                    | "b" -> stream.writeU8(offset,  Byte.Parse(value))
                    | "w" -> stream.writeU16(offset, UInt16.Parse(value))
                    | "d" -> stream.writeU32(offset, UInt32.Parse(value))
                    | "l" -> stream.writeU64(offset, UInt64.Parse(value))
                    | _ -> failwithf($"Unknown data type : {ap.Type}")

                | _ -> failwithf($"Unknown address with assignment pattern : {addressWithAssignValue}")

            match command with
            | "read" ->
                let result =
                    args |> map (fun a -> $"{a}={readAddress(a)}")
                    |> joinWith " "
                ReadResultString(result)

            | "write" ->
                args |> iter (fun a -> writeAddressWithValue(a))
                WriteResultOK()

            | "rb" ->
                let name = respSocket.ReceiveFrameString().ToLower()
                let indices =
                    let address = respSocket.ReceiveFrameBytes()
                    DataTypeConverter.BytesToTypeArray<int>(address)
                let result = indices |> map (streams[name].readU8)
                ReadResultArray<byte>(result)

            | "rw" ->
                let name = respSocket.ReceiveFrameString().ToLower()
                let indices =
                    let address = respSocket.ReceiveFrameBytes()
                    DataTypeConverter.BytesToTypeArray<int>(address)
                let result = indices |> map (streams[name].readU16)
                ReadResultArray<uint16>(result)

            | "wb" ->
                let name = respSocket.ReceiveFrameString().ToLower()
                let indices, values =
                    let address = respSocket.ReceiveFrameBytes()
                    let values = respSocket.ReceiveFrameBytes()
                    DataTypeConverter.BytesToTypeArray<int>(address), values
                if indices.Length <> values.Length then
                    failwithf($"The number of indices and values should be the same.")

                Array.zip indices values |> iter ( fun (index, value) -> streams[name].writeU8(index, value))
                WriteResultOK()
            | _ ->
                ReadResultError $"Unknown request: {request}"

        member x.Run() =
            // start a separate thread to run the server
            Thread(ThreadStart(fun () ->
                use respSocket = new ResponseSocket()
                respSocket.Bind($"tcp://*:{port}")
                
                while not cancellationToken.IsCancellationRequested do
                    let response = x.handleRequest respSocket
                    match response with
                    | :? ReadResultString as ok ->
                        respSocket.SendFrame(ok.Result)
                    | :? WriteResultOK as ok ->
                        respSocket.SendFrame("OK")
                    | :? ReadResultArray<byte> as ok ->
                        respSocket.SendMoreFrame("OK").SendFrame(ok.Results)
                    | :? ReadResultArray<uint16> as ok ->
                        respSocket.SendMoreFrame("OK").SendFrame(DataTypeConverter.ToBytes<uint16>(ok.Results))
                    | :? IIOResultNG as ng ->
                        respSocket.SendFrame(ng.Error)
                    | _ ->
                        failwithf($"Unknown response type: {response.GetType()}")
            )) |> tee (fun t -> t.Start())

        interface IDisposable with
            member x.Dispose() =
                streams.Values |> iter (fun stream -> stream.FileStream.Dispose())

    /// serverAddress: "tcp://localhost:5555" or "tcp://*:5555"
    type Client(serverAddress:string) =
        let reqSocket = new RequestSocket()
        do
            reqSocket.Connect(serverAddress)
                
        interface IDisposable with
            member x.Dispose() =
                reqSocket.Close()

        member x.SendRequest(request:string) : string =
            reqSocket.SendFrame(request)
            reqSocket.ReceiveFrameString()

        member x.ReadBytes(name:string, offsets:int[]) : byte[] =
            reqSocket
                .SendMoreFrame("rb")
                .SendMoreFrame(name)
                .SendFrame(DataTypeConverter.ToBytes<int>(offsets))
            let result = reqSocket.ReceiveFrameString()
            match result with
            | "OK" ->
                let buffer = reqSocket.ReceiveFrameBytes()
                buffer
            | _ ->
                failwithf($"Error: {result}")


        member x.ReadUInt16s(name:string, offsets:int[]) : uint16[] =
            let xxx = DataTypeConverter.ToBytes<int>(offsets)
            // 데이터를 요청하는 메시지 전송
            reqSocket
                .SendMoreFrame("rw")
                .SendMoreFrame(name)
                .SendFrame(DataTypeConverter.ToBytes<int>(offsets))

            // 서버로부터 응답 수신
            let result = reqSocket.ReceiveFrameString()
            match result with
            | "OK" ->
                let buffer = reqSocket.ReceiveFrameBytes()
                DataTypeConverter.BytesToTypeArray<uint16>(buffer) // 바이트 배열을 uint16 배열로 변환
            | _ ->
                failwithf($"Error: {result}")


        member x.WriteBytes(name:string, offsets:int[], values:byte[]) =
            reqSocket
                .SendMoreFrame("wb")
                .SendMoreFrame(name)
                .SendMoreFrame(DataTypeConverter.ToBytes<int>(offsets))
                .SendFrame(values)

            let result = reqSocket.ReceiveFrameString()
            match result with
            | "OK" -> ()
            | _ ->
                failwithf($"Error: {result}")


        member x.WriteUInt16s(name:string, offsets:int[], values:uint16[]) =
            reqSocket
                .SendMoreFrame("wu16")
                .SendMoreFrame(name)
                .SendMoreFrame(DataTypeConverter.ToBytes<int>(offsets))
                .SendFrame(DataTypeConverter.ToBytes<uint16>(values))

            let result = reqSocket.ReceiveFrameString()
            match result with
            | "OK" -> ()
            | _ ->
                failwithf($"Error: {result}")

module Main =
    [<EntryPoint>]
    let main _ = 
        let ioSpec:IOSpec =
            "appsettings.json"
            |> File.ReadAllText
            |> JsonConvert.DeserializeObject<IOSpec>

        let port = ioSpec.ServicePort
        let cts = new CancellationTokenSource()
        let server = new Zmq.Server(ioSpec, cts.Token)
        let serverThread = server.Run()

        let client = new Zmq.Client($"tcp://localhost:{port}")

        let rr0 = client.SendRequest("read Mw100 Mx30 Md1234")
        let result = client.SendRequest("read Mw100 Mx30")
        let result2 = client.SendRequest("read Mw100 Mb70 Mx30 Md50 Ml50")
        //let result3 = client.SendRequest("read [Mw100..Mw30]")
        let wr = client.SendRequest("write Mw100=1 Mx30=false Md1234=1234")
        let rr = client.SendRequest("read Mw100 Mx30 Md1234")
        let xxx = result

        let wr2 = client.WriteBytes("M", [|0; 1; 2; 3|], [|0uy; 1uy; 2uy; 3uy|])
        let bytes:byte[] = client.ReadBytes("M", [|0; 1; 2; 3|])
        let words:uint16[] = client.ReadUInt16s("M", [|0; 1; 2; 3|])


        let wr2 = client.WriteBytes("M", [|0; 1; 2; 3|], [|1uy; 0uy; 55uy; 0uy|])
        let bytes:byte[] = client.ReadBytes("M", [|0; 1; 2; 3|])
        let words:uint16[] = client.ReadUInt16s("M", [|0; 1; 2; 3|])
        serverThread.Join()
     
        
        0