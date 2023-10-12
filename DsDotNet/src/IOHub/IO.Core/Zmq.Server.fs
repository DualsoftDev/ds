(* IO.Core using Zero MQ *)

namespace IO.Core
open System
open System.Threading
open System.Threading.Tasks
open NetMQ
open NetMQ.Sockets
open Dual.Common.Core.FS
open System.Collections.Generic
open System.IO
open System.Runtime.Remoting
open IO.Spec

module ZmqServerModule =
    type Server(ioSpec:IOSpec, cancellationToken:CancellationToken) =
        let port = ioSpec.ServicePort
        let streams = new Dictionary<string, BufferManager>()

        let showSamples (vendorSpec:VendorSpec) (addressExtractor:IAddressInfoProvider) =
            let v = vendorSpec
            match v.Name with
            | "Paix" ->
                match addressExtractor.GetAddressInfo("ox12.1") with
                | true, memoryType, byteOffset, bitOffset, contentBitLength ->
                    assert (memoryType = "o")
                    assert (bitOffset = 1)
                    assert (byteOffset = 12)
                    assert (contentBitLength = 1)
                | _ ->
                    failwithf($"Invalid address format: {v.Name}, {v.Dll}, {v.ClassName}")
                match addressExtractor.GetAddressInfo("ob12") with
                | true, memoryType, byteOffset, bitOffset, contentBitLength ->
                    assert (memoryType = "o")
                    assert (bitOffset = 0)
                    assert (byteOffset = 12)
                    assert (contentBitLength = 8)
                | _ ->
                    failwithf($"Invalid address format: {v.Name}, {v.Dll}, {v.ClassName}")
            | "LsXGI" ->
                match addressExtractor.GetAddressInfo("%IX30.3") with
                | true, memoryType, byteOffset, bitOffset, contentBitLength ->
                    assert (memoryType = "i")
                    assert (bitOffset = 3)
                    assert (byteOffset = 30)
                    assert (contentBitLength = 1)
                | _ ->
                    failwithf($"Invalid address format: {v.Name}, {v.Dll}, {v.ClassName}")
            | _ ->
                ()

        do
            for v in ioSpec.Vendors do
                let addressExtractor:IAddressInfoProvider =
                    let oh:ObjectHandle = Activator.CreateInstanceFrom(v.Dll, v.ClassName)
                    let obj:obj = oh.Unwrap()
                    obj :?> IAddressInfoProvider

                showSamples v addressExtractor

                for f in v.Files do
                    let dir, key =
                        match v.Location with
                        | "" | null -> ioSpec.TopLevelLocation, f.Name
                        | _ -> Path.Combine(ioSpec.TopLevelLocation, v.Location), $"{v.Location}/{f.Name}"
                    let stream = f.InitiaizeFile(dir)
                    streams.Add(key, new BufferManager(stream))

        let (|AddressPattern|_|) (str: string) =
            let memTypes = streams.Keys.JoinWith "|"
            let dataTypes = "x|b|w|d|l"
            let pattern = sprintf "^(%s)([%s])(\d+)" memTypes dataTypes
            match str with
            | RegexPattern pattern [m; d; Int32Pattern offset] -> Some(AddressSpec(m, d, offset))
            | _ -> None

        let (|AddressAssignPattern|_|) (str: string) =
            match str with
            | RegexPattern "([^=]+)=(\w+)" [AddressPattern addr; value] ->
                Some(addr, value)
            | _ -> None
            

        let mutable terminated = false
        member x.IsTerminated with get() = terminated

        member private x.handleRequest (respSocket:ResponseSocket) : IIOResult =
            let mutable request = ""
            if not <| respSocket.TryReceiveFrameString(&request) then
                null
            else
                // 메시지를 처리하는 코드, 여기에서 'message'를 사용할 수 있습니다.
                //let request = respSocket.ReceiveFrameString()
                logDebug $"Handling request: {request}"
                let tokens = request.Split(' ', StringSplitOptions.RemoveEmptyEntries) |> Array.ofSeq
                let command = tokens[0]
                let readAddress(address:string) : obj =
                    match address with
                    | AddressPattern addr ->
                        let ap = addr
                        let offset = ap.Offset
                        let stream = streams[ap.Name]

                        match ap.Type with
                        | "x" -> stream.VerifyIndices(offset / 8); stream.readBit(offset) :> obj
                        | "b" -> stream.VerifyIndices(offset * 1); stream.readU8(offset)
                        | "w" -> stream.VerifyIndices(offset * 2); stream.readU16(offset)
                        | "d" -> stream.VerifyIndices(offset * 4); stream.readU32(offset)
                        | "l" -> stream.VerifyIndices(offset * 8); stream.readU64(offset)
                        | _ -> failwithf($"Unknown data type : {ap.Type}")
                    | _ -> failwithf($"Unknown address pattern : {address}")

                let writeAddressWithValue(addressWithAssignValue:string) =
                    let parseBool (s:string) =
                        match s.ToLower() with
                        | "1" | "true" -> true
                        | "0" | "false" -> false
                        | _ -> failwithf($"Invalid boolean value: {s}")
                    match addressWithAssignValue with
                    | AddressAssignPattern (addressPattern, value) ->
                        let ap = addressPattern
                        let offset = ap.Offset
                        let stream = streams[ap.Name]

                        match ap.Type with
                        | "x" -> stream.VerifyIndices(offset / 8); stream.writeBit(offset, parseBool(value))
                        | "b" -> stream.VerifyIndices(offset * 1); stream.writeU8(offset,  Byte.Parse(value))
                        | "w" -> stream.VerifyIndices(offset * 2); stream.writeU16(offset, UInt16.Parse(value))
                        | "d" -> stream.VerifyIndices(offset * 4); stream.writeU32(offset, UInt32.Parse(value))
                        | "l" -> stream.VerifyIndices(offset * 8); stream.writeU64(offset, UInt64.Parse(value))
                        | _ -> failwithf($"Unknown data type : {ap.Type}")

                        stream.Flush()

                    | _ -> failwithf($"Unknown address with assignment pattern : {addressWithAssignValue}")

                let fetchStreamAndIndices (respSocket:ResponseSocket) =
                    let stream =
                        let name = respSocket.ReceiveFrameString().ToLower()
                        streams[name]
                    let indices =
                        let address = respSocket.ReceiveFrameBytes()
                        ByteConverter.BytesToTypeArray<int>(address)
                    stream, indices
                let fetchForRead = fetchStreamAndIndices
                let fetchForWrite (respSocket:ResponseSocket) =
                    let stream, indices = fetchForRead respSocket
                    let values = respSocket.ReceiveFrameBytes()
                    stream, indices, values

                let args = tokens[1..] |> map(fun s -> s.ToLower())
                match command with
                | "read" ->
                    let result =
                        args |> map (fun a -> $"{a}={readAddress(a)}")
                        |> joinWith " "
                    ReadResultString(result)

                | "write" ->
                    args |> iter (fun a -> writeAddressWithValue(a))
                    WriteResultOK()

                | "rx" ->
                    let stream, indices = fetchForRead respSocket
                    stream.VerifyIndices(indices |> map (fun n -> n / 8))
                    let result = indices |> map (stream.readBit)
                    ReadResultArray<bool>(result)
                | "rb" ->
                    let stream, indices = fetchForRead respSocket
                    stream.VerifyIndices(indices)
                    let result = indices |> map (stream.readU8)
                    ReadResultArray<byte>(result)

                | "rw" ->
                    let stream, indices = fetchForRead respSocket
                    stream.VerifyIndices(indices |> map (fun n -> n * 2))
                    let result = indices |> map (stream.readU16)
                    ReadResultArray<uint16>(result)

                | "rd" ->
                    let stream, indices = fetchForRead respSocket
                    stream.VerifyIndices(indices |> map (fun n -> n * 4))
                    let result = indices |> map (stream.readU32)
                    ReadResultArray<uint32>(result)

                | "rl" ->
                    let stream, indices = fetchForRead respSocket
                    stream.VerifyIndices(indices |> map (fun n -> n * 8))
                    let result = indices |> map (stream.readU64)
                    ReadResultArray<uint64>(result)

                | "wx" ->
                    let stream, indices, values = fetchForWrite respSocket
                    stream.VerifyIndices(indices |> map (fun n -> n / 8))
                    if indices.Length <> values.Length then
                        failwithf($"The number of indices and values should be the same.")

                    for i in [0..indices.Length-1] do
                        let value =
                            match values.[i] with
                            | 1uy -> true
                            | 0uy -> false
                            | _ -> failwithf($"Invalid value: {values.[i]}")
                        stream.writeBit(indices[i], value)

                    stream.Flush()
                    WriteResultOK()
                | "wb" ->
                    let stream, indices, values = fetchForWrite respSocket
                    stream.VerifyIndices(indices)
                    if indices.Length <> values.Length then
                        failwithf($"The number of indices and values should be the same.")

                    Array.zip indices values |> iter ( fun (index, value) -> stream.writeU8(index, value))
                    stream.Flush()
                    WriteResultOK()

                | "ww" ->
                    let stream, indices, values = fetchForWrite respSocket
                    stream.VerifyIndices(indices |> map (fun n -> n * 2))
                    if indices.Length <> values.Length / 2 then
                        failwithf($"The number of indices and values should be the same.")

                    Array.zip indices (ByteConverter.BytesToTypeArray<uint16>(values)) |> iter ( fun (index, value) -> stream.writeU16(index, value))
                    stream.Flush()
                    WriteResultOK()

                | "wd" ->
                    let stream, indices, values = fetchForWrite respSocket
                    stream.VerifyIndices(indices |> map (fun n -> n * 4))
                    if indices.Length <> values.Length / 4 then
                        failwithf($"The number of indices and values should be the same.")

                    Array.zip indices (ByteConverter.BytesToTypeArray<uint32>(values)) |> iter ( fun (index, value) -> stream.writeU32(index, value))
                    stream.Flush()
                    WriteResultOK()

                | "wl" ->
                    let stream, indices, values = fetchForWrite respSocket
                    stream.VerifyIndices(indices |> map (fun n -> n * 8))
                    if indices.Length <> values.Length / 8 then
                        failwithf($"The number of indices and values should be the same.")

                    Array.zip indices (ByteConverter.BytesToTypeArray<uint64>(values)) |> iter ( fun (index, value) -> stream.writeU64(index, value))
                    stream.Flush()
                    WriteResultOK()

                | _ ->
                    ReadResultError $"Unknown request: {request}"




        member x.Run() =
            // start a separate thread to run the server
            Task.Run(fun () ->
                logInfo $"Starting server on port {port}..."
                use respSocket = new ResponseSocket()
                respSocket.Bind($"tcp://*:{port}")
                
                while not cancellationToken.IsCancellationRequested do
                    try
                        let response = x.handleRequest respSocket
                        match response with
                        | null ->
                            // 현재, request 가 없는 경우
                            // Async.Sleep(???)
                            ()
                        | :? ReadResultString as ok ->
                            respSocket.SendFrame(ok.Result)
                        | :? WriteResultOK as ok ->
                            respSocket.SendFrame("OK")
                        | :? ReadResultArray<byte> as ok ->
                            respSocket.SendMoreFrame("OK").SendFrame(ok.Results)
                        | :? ReadResultArray<uint16> as ok ->
                            respSocket.SendMoreFrame("OK").SendFrame(ByteConverter.ToBytes<uint16>(ok.Results))
                        | :? IIOResultNG as ng ->
                            respSocket.SendFrame(ng.Error)
                        | _ ->
                            failwithf($"Unknown response type: {response.GetType()}")
                    with ex ->
                        logError $"Error occured while handling request: {ex.Message}"
                        respSocket.SendFrame(ex.Message)
                Console.WriteLine("Cancellation request detected!")
                (x :> IDisposable).Dispose()
                terminated <- true
            )

        interface IDisposable with
            member x.Dispose() =
                logDebug "Disposing server..."
                streams.Values |> iter (fun stream -> stream.FileStream.Dispose())
