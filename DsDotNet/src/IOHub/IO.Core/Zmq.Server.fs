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

        /// e.g {"p/o", <Paix Output Buffer manager>}
        let bufferManagers = new Dictionary<string, BufferManager>()

        /// tag 별 address 정보를 저장하는 dictionary
        let tagDic = new Dictionary<string, AddressSpec>()

        //let showSamples (vendorSpec:VendorSpec) (addressExtractor:IAddressInfoProvider) =
        //    let v = vendorSpec
        //    match v.Name with
        //    | "Paix" ->
        //        match addressExtractor.GetAddressInfo("ox12.1") with
        //        | true, memoryType, byteOffset, bitOffset, contentBitLength ->
        //            assert (memoryType = "o")
        //            assert (bitOffset = 1)
        //            assert (byteOffset = 12)
        //            assert (contentBitLength = 1)
        //        | _ ->
        //            failwithf($"Invalid address format: {v.Name}, {v.Dll}, {v.ClassName}")
        //        match addressExtractor.GetAddressInfo("ob12") with
        //        | true, memoryType, byteOffset, bitOffset, contentBitLength ->
        //            assert (memoryType = "o")
        //            assert (bitOffset = 0)
        //            assert (byteOffset = 12)
        //            assert (contentBitLength = 8)
        //        | _ ->
        //            failwithf($"Invalid address format: {v.Name}, {v.Dll}, {v.ClassName}")
        //    | "LsXGI" ->
        //        match addressExtractor.GetAddressInfo("%IX30.3") with
        //        | true, memoryType, byteOffset, bitOffset, contentBitLength ->
        //            assert (memoryType = "i")
        //            assert (bitOffset = 3)
        //            assert (byteOffset = 30)
        //            assert (contentBitLength = 1)
        //        | _ ->
        //            failwithf($"Invalid address format: {v.Name}, {v.Dll}, {v.ClassName}")
        //    | _ ->
        //        ()

        do
            for v in ioSpec.Vendors do
                v.AddressResolver <-
                    let oh:ObjectHandle = Activator.CreateInstanceFrom(v.Dll, v.ClassName)
                    let obj:obj = oh.Unwrap()
                    obj :?> IAddressInfoProvider

                //showSamples v v.AddressResolver

                for f in v.Files do
                    let dir, key =
                        match v.Location with
                        | "" | null -> ioSpec.TopLevelLocation, f.Name
                        | _ -> Path.Combine(ioSpec.TopLevelLocation, v.Location), $"{v.Location}/{f.Name}"
                    f.InitiaizeFile(dir)
                    let bufferManager = new BufferManager(f)
                    let key = if v.Location.NonNullAny() then $"{v.Location}/{f.Name}" else f.Name
                    bufferManagers.Add(key, bufferManager)

        let getVendor (addr:string) : (VendorSpec * string) =
            match addr with
            | RegexPattern "^([^/]+)/([^/]+)$" [vendor; address] ->
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
                logDebug $"Handling request: {request}"
                let tokens = request.Split(' ', StringSplitOptions.RemoveEmptyEntries) |> Array.ofSeq
                let command = tokens[0]

                let readAddress(address:string) : obj =
                    match address with
                    | AddressPattern ap ->
                        let byteOffset = ap.OffsetByte
                        let bufferManager = ap.IOFileSpec.BufferManager :?> BufferManager
                        bufferManager.VerifyIndices([|byteOffset|])

                        match ap.DataType with
                        | PLCMemoryBitSize.Bit   -> bufferManager.readBit(byteOffset * 8 + ap.OffsetBit) :> obj
                        | PLCMemoryBitSize.Byte  -> bufferManager.readU8(byteOffset)
                        | PLCMemoryBitSize.Word  -> bufferManager.readU16(byteOffset)
                        | PLCMemoryBitSize.DWord -> bufferManager.readU32(byteOffset)
                        | PLCMemoryBitSize.LWord -> bufferManager.readU64(byteOffset)
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
                        let bufferManager = ap.IOFileSpec.BufferManager :?> BufferManager
                        bufferManager.VerifyIndices([|byteOffset|])

                        match ap.DataType with
                        | PLCMemoryBitSize.Bit   -> bufferManager.writeBit(byteOffset, ap.OffsetBit, parseBool(value))
                        | PLCMemoryBitSize.Byte  -> bufferManager.writeU8(byteOffset,  Byte.Parse(value))
                        | PLCMemoryBitSize.Word  -> bufferManager.writeU16(byteOffset, UInt16.Parse(value))
                        | PLCMemoryBitSize.DWord -> bufferManager.writeU32(byteOffset, UInt32.Parse(value))
                        | PLCMemoryBitSize.LWord -> bufferManager.writeU64(byteOffset, UInt64.Parse(value))
                        | _ -> failwithf($"Unknown data type : {ap.DataType}")

                        bufferManager.Flush()

                    | _ -> failwithf($"Unknown address with assignment pattern : {addressWithAssignValue}")

                let fetchBufferManagerAndIndices (isBitIndex:bool) (respSocket:ResponseSocket) =
                    let bufferManager =
                        let name = respSocket.ReceiveFrameString().ToLower()
                        bufferManagers[name]
                    let indices =
                        let address = respSocket.ReceiveFrameBytes()
                        ByteConverter.BytesToTypeArray<int>(address)
                    for byteIndex in indices |> map (fun n -> if isBitIndex then n / 8 else n) do
                        if bufferManager.FileStream.Length < byteIndex then
                            failwithf($"Invalid address: {byteIndex}")
                    bufferManager, indices

                let fetchForRead = fetchBufferManagerAndIndices false
                let fetchForReadBit = fetchBufferManagerAndIndices true

                let fetchForWrite (respSocket:ResponseSocket) =
                    let bm, indices = fetchForRead respSocket
                    let values = respSocket.ReceiveFrameBytes()
                    bm, indices, values

                let args = tokens[1..] |> map(fun s -> s.ToLower())
                match command with
                | "read" ->
                    let result =
                        args |> map (fun a -> $"{a}={readAddress(a)}")
                        |> joinWith " "
                    ReadResultString(result)
                | "r" ->
                    let result = readAddress(tokens[1])
                    match result with
                    | :? bool   as n -> ReadResultSingle<bool>(n)
                    | :? byte   as n -> ReadResultSingle<byte>(n)
                    | :? uint16 as n -> ReadResultSingle<uint16>(n)
                    | :? uint32 as n -> ReadResultSingle<uint32>(n)
                    | :? uint64 as n -> ReadResultSingle<uint64>(n)
                    | _ -> failwithf $"Unknown type {tokens[1]}"


                | "write" ->
                    args |> iter (fun a -> writeAddressWithValue(a))
                    WriteResultOK()

                | "rx" ->
                    let bm, indices = fetchForReadBit respSocket
                    bm.VerifyIndices(indices |> map (fun n -> n / 8))
                    let result = indices |> map (bm.readBit)
                    ReadResultArray<bool>(result)
                | "rb" ->
                    let bm, indices = fetchForRead respSocket
                    bm.VerifyIndices(indices)
                    let result = indices |> map (bm.readU8)
                    ReadResultArray<byte>(result)

                | "rw" ->
                    let bm, indices = fetchForRead respSocket
                    bm.VerifyIndices(indices |> map (fun n -> n * 2))
                    let result = indices |> map (bm.readU16)
                    ReadResultArray<uint16>(result)

                | "rd" ->
                    let bm, indices = fetchForRead respSocket
                    bm.VerifyIndices(indices |> map (fun n -> n * 4))
                    let result = indices |> map (bm.readU32)
                    ReadResultArray<uint32>(result)

                | "rl" ->
                    let bm, indices = fetchForRead respSocket
                    bm.VerifyIndices(indices |> map (fun n -> n * 8))
                    let result = indices |> map (bm.readU64)
                    ReadResultArray<uint64>(result)

                | "wx" ->
                    let bm, indices, values = fetchForWrite respSocket
                    bm.VerifyIndices(indices |> map (fun n -> n / 8))
                    if indices.Length <> values.Length then
                        failwithf($"The number of indices and values should be the same.")

                    for i in [0..indices.Length-1] do
                        let value =
                            match values.[i] with
                            | 1uy -> true
                            | 0uy -> false
                            | _ -> failwithf($"Invalid value: {values.[i]}")
                        bm.writeBit(indices[i], value)

                    bm.Flush()
                    WriteResultOK()
                | "wb" ->
                    let bm, indices, values = fetchForWrite respSocket
                    bm.VerifyIndices(indices)
                    if indices.Length <> values.Length then
                        failwithf($"The number of indices and values should be the same.")

                    Array.zip indices values |> iter ( fun (index, value) -> bm.writeU8(index, value))
                    bm.Flush()
                    WriteResultOK()

                | "ww" ->
                    let bm, indices, values = fetchForWrite respSocket
                    bm.VerifyIndices(indices |> map (fun n -> n * 2))
                    if indices.Length <> values.Length / 2 then
                        failwithf($"The number of indices and values should be the same.")

                    Array.zip indices (ByteConverter.BytesToTypeArray<uint16>(values)) |> iter ( fun (index, value) -> bm.writeU16(index, value))
                    bm.Flush()
                    WriteResultOK()

                | "wd" ->
                    let bm, indices, values = fetchForWrite respSocket
                    bm.VerifyIndices(indices |> map (fun n -> n * 4))
                    if indices.Length <> values.Length / 4 then
                        failwithf($"The number of indices and values should be the same.")

                    let xxx = Array.zip indices (ByteConverter.BytesToTypeArray<uint32>(values)) 
                    Array.zip indices (ByteConverter.BytesToTypeArray<uint32>(values)) |> iter ( fun (index, value) -> bm.writeU32(index, value))
                    bm.Flush()
                    WriteResultOK()

                | "wl" ->
                    let bm, indices, values = fetchForWrite respSocket
                    bm.VerifyIndices(indices |> map (fun n -> n * 8))
                    if indices.Length <> values.Length / 8 then
                        failwithf($"The number of indices and values should be the same.")

                    Array.zip indices (ByteConverter.BytesToTypeArray<uint64>(values)) |> iter ( fun (index, value) -> bm.writeU64(index, value))
                    bm.Flush()
                    WriteResultOK()

                | "cl" ->
                    let name = respSocket.ReceiveFrameString().ToLower()
                    let bm = bufferManagers[name]
                    bm.clear()
                    bm.Flush()
                    WriteResultOK()
                | _ ->
                    ReadResultError $"Unknown request: {request}"




        member x.Run() =
            // start a separate thread to run the server
            let f() =
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

                        | :? ReadResultSingle<byte> as ok ->
                            respSocket.SendMoreFrame("OK").SendFrame([|ok.Result|])
                        | :? ReadResultSingle<uint16> as ok ->
                            respSocket.SendMoreFrame("OK").SendFrame(BitConverter.GetBytes(ok.Result))
                        | :? ReadResultSingle<uint32> as ok ->
                            respSocket.SendMoreFrame("OK").SendFrame(BitConverter.GetBytes(ok.Result))
                        | :? ReadResultSingle<uint64> as ok ->
                            respSocket.SendMoreFrame("OK").SendFrame(BitConverter.GetBytes(ok.Result))


                        | :? ReadResultArray<byte> as ok ->
                            respSocket.SendMoreFrame("OK").SendFrame(ok.Results)
                        | :? ReadResultArray<uint16> as ok ->
                            respSocket.SendMoreFrame("OK").SendFrame(ByteConverter.ToBytes<uint16>(ok.Results))
                        | :? ReadResultArray<uint32> as ok ->
                            respSocket.SendMoreFrame("OK").SendFrame(ByteConverter.ToBytes<uint32>(ok.Results))
                        | :? ReadResultArray<uint64> as ok ->
                            respSocket.SendMoreFrame("OK").SendFrame(ByteConverter.ToBytes<uint64>(ok.Results))




                        | :? IIOResultNG as ng ->
                            respSocket.SendFrame(ng.Error)
                        | _ ->
                            failwithf($"Unknown response type: {response.GetType()}")
                    with ex ->
                        logError $"Error occured while handling request: {ex.Message}"
                        respSocket.SendFrame(ex.Message)

                logInfo("Cancellation request detected!")
                (x :> IDisposable).Dispose()
                terminated <- true

            Task.Factory.StartNew(f, TaskCreationOptions.LongRunning)

        interface IDisposable with
            member x.Dispose() =
                logDebug "Disposing server..."
                bufferManagers.Values |> iter (fun stream -> stream.FileStream.Dispose())
