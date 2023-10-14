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
        let streams = new Dictionary<FileStream, BufferManager>()
        let streams2 = new Dictionary<string, BufferManager>()
        let tagDic = new Dictionary<string, AddressSpec>()

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
                v.AddressResolver <-
                    let oh:ObjectHandle = Activator.CreateInstanceFrom(v.Dll, v.ClassName)
                    let obj:obj = oh.Unwrap()
                    obj :?> IAddressInfoProvider

                showSamples v v.AddressResolver

                for f in v.Files do
                    let dir, key =
                        match v.Location with
                        | "" | null -> ioSpec.TopLevelLocation, f.Name
                        | _ -> Path.Combine(ioSpec.TopLevelLocation, v.Location), $"{v.Location}/{f.Name}"
                    f.InitiaizeFile(dir)
                    let bufferManager = new BufferManager(f)
                    streams.Add(f.FileStream, bufferManager)
                    let key = if v.Location.NonNullAny() then $"{v.Location}/{f.Name}" else f.Name
                    streams2.Add(key, bufferManager)

        let getVendor (addr:string) : (VendorSpec * string) =
            match addr with
            | RegexPattern "^([^/]+)/(\w+)$" [vendor; address] ->
                let v =
                    ioSpec.Vendors
                    |> Seq.find (fun v -> v.Location = vendor)
                v, address
            | _ ->
                let v =
                    ioSpec.Vendors
                    |> Seq.find (fun v -> v.Location = "")
                v, addr

        // e.g "p/ob1"
        let (|AddressPattern|_|) (str: string) =
            if tagDic.ContainsKey(str) then
                Some(tagDic.[str])
            else
                option {
                    let (vendor, address) = getVendor str
                    match vendor.AddressResolver.GetAddressInfo(address) with
                    | true, memType, byteOffset, bitOffset, contentBitLength ->
                        let! f = vendor.Files |> Seq.tryFind(fun f -> f.Name = memType)
                        let addressSpec = AddressSpec(f, bitSizeToEnum(contentBitLength), byteOffset, bitOffset)
                        tagDic.Add(str, addressSpec)
                        return addressSpec
                    | _ ->
                        return! None
                }

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
                    | AddressPattern ap ->
                        let byteOffset = ap.OffsetByte
                        let stream = streams[ap.IOFileSpec.FileStream]
                        if ap.IOFileSpec.Length < byteOffset then
                            failwith $"Invalid address: {address}"

                        match ap.DataType with
                        | PLCMemoryBitSize.Bit   -> stream.readBit(byteOffset * 8 + ap.OffsetBit) :> obj
                        | PLCMemoryBitSize.Byte  -> stream.readU8(byteOffset)
                        | PLCMemoryBitSize.Word  -> stream.readU16(byteOffset)
                        | PLCMemoryBitSize.DWord -> stream.readU32(byteOffset)
                        | PLCMemoryBitSize.LWord -> stream.readU64(byteOffset)
                        | _ ->
                            failwithf($"Unknown data type : {ap.DataType}")
                    | _ ->
                        failwithf($"Unknown address pattern : {address}")

                let writeAddressWithValue(addressWithAssignValue:string) =
                    let parseBool (s:string) =
                        match s.ToLower() with
                        | "1" | "true" -> true
                        | "0" | "false" -> false
                        | _ -> failwithf($"Invalid boolean value: {s}")
                    match addressWithAssignValue with
                    | AddressAssignPattern (addressPattern, value) ->
                        let ap = addressPattern
                        let byteOffset = ap.OffsetByte
                        let stream = streams[ap.IOFileSpec.FileStream]
                        if ap.IOFileSpec.Length <= byteOffset then
                            failwith $"Invalid address: {addressPattern}"

                        match ap.DataType with
                        | PLCMemoryBitSize.Bit   -> stream.writeBit(byteOffset, ap.OffsetBit, parseBool(value))
                        | PLCMemoryBitSize.Byte  -> stream.writeU8(byteOffset,  Byte.Parse(value))
                        | PLCMemoryBitSize.Word  -> stream.writeU16(byteOffset, UInt16.Parse(value))
                        | PLCMemoryBitSize.DWord -> stream.writeU32(byteOffset, UInt32.Parse(value))
                        | PLCMemoryBitSize.LWord -> stream.writeU64(byteOffset, UInt64.Parse(value))
                        | _ -> failwithf($"Unknown data type : {ap.DataType}")

                        stream.Flush()

                    | _ -> failwithf($"Unknown address with assignment pattern : {addressWithAssignValue}")

                let fetchStreamAndIndices (isBitIndex:bool) (respSocket:ResponseSocket) =
                    let stream =
                        let name = respSocket.ReceiveFrameString().ToLower()
                        streams2[name]
                    let indices =
                        let address = respSocket.ReceiveFrameBytes()
                        ByteConverter.BytesToTypeArray<int>(address)
                    for byteIndex in indices |> map (fun n -> if isBitIndex then n / 8 else n) do
                        if stream.FileStream.Length <= byteIndex then
                            failwithf($"Invalid address: {byteIndex}")
                    stream, indices

                let fetchForRead = fetchStreamAndIndices false
                let fetchForReadBit = fetchStreamAndIndices true

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
                    let stream, indices = fetchForReadBit respSocket
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
