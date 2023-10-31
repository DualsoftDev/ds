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

        member private x.handleRequest (respSocket:ResponseSocket) : IOResult =
            let mutable request = ""
            if not <| respSocket.TryReceiveFrameString(&request) then
                Ok null
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

                /// "write p/ob1=1 p/ix2=0" : 비효율성 인정한 version.  buffer manager 및 dataType 의 다양성 공존
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
                    noop()
                    let result =
                        args |> map (fun a -> $"{a}={readAddress(a)}")
                        |> joinWith " "
                    Ok result
                | "r" ->
                    let result = readAddress(tokens[1])
                    match result with
                    | :? bool   
                    | :? byte   
                    | :? uint16 
                    | :? uint32 
                    | :? uint64 ->
                        Ok result
                    | _ ->
                        let errMsg = $"Unknown type {tokens[1]}"
                        logError "%s" errMsg
                        Error errMsg


                | "write" ->
                    args |> iter (fun a -> writeAddressWithValue(a))
                    Ok (WriteOK())

                | "rx" ->
                    let bm, indices = fetchForReadBit respSocket
                    bm.VerifyIndices(indices |> map (fun n -> n / 8))
                    let result = indices |> map (bm.readBit)
                    Ok result
                | "rb" ->
                    let bm, indices = fetchForRead respSocket
                    bm.VerifyIndices(indices)
                    let result = indices |> map (bm.readU8)
                    Ok result

                | "rw" ->
                    let bm, indices = fetchForRead respSocket
                    bm.VerifyIndices(indices |> map (fun n -> n * 2))
                    let result = indices |> map (bm.readU16)
                    Ok result

                | "rd" ->
                    let bm, indices = fetchForRead respSocket
                    bm.VerifyIndices(indices |> map (fun n -> n * 4))
                    let result = indices |> map (bm.readU32)
                    Ok result

                | "rl" ->
                    let bm, indices = fetchForRead respSocket
                    bm.VerifyIndices(indices |> map (fun n -> n * 8))
                    let result = indices |> map (bm.readU64)
                    Ok result

                | "wx" ->
                    let bm, indices, values = fetchForWrite respSocket
                    bm.Verify(indices |> map (fun n -> n / 8), values.Length)

                    for i in [0..indices.Length-1] do
                        let value =
                            match values.[i] with
                            | 1uy -> true
                            | 0uy -> false
                            | _ -> failwithf($"Invalid value: {values.[i]}")
                        bm.writeBit(indices[i], value)

                    bm.Flush()
                    Ok (WriteOK())
                | "wb" ->
                    let bm, indices, values = fetchForWrite respSocket
                    bm.Verify(indices, values.Length / 1)

                    Array.zip indices values |> iter ( fun (index, value) -> bm.writeU8(index, value))
                    bm.Flush()
                    Ok (WriteOK())

                | "ww" ->
                    let bm, indices, values = fetchForWrite respSocket
                    bm.Verify(indices |> map (fun n -> n * 2), values.Length / 2)

                    Array.zip indices (ByteConverter.BytesToTypeArray<uint16>(values)) |> iter ( fun (index, value) -> bm.writeU16(index, value))
                    bm.Flush()
                    Ok (WriteOK())

                | "wd" ->
                    let bm, indices, values = fetchForWrite respSocket
                    bm.Verify(indices |> map (fun n -> n * 4), values.Length / 4)

                    let xxx = Array.zip indices (ByteConverter.BytesToTypeArray<uint32>(values)) 
                    Array.zip indices (ByteConverter.BytesToTypeArray<uint32>(values)) |> iter ( fun (index, value) -> bm.writeU32(index, value))
                    bm.Flush()
                    Ok (WriteOK())

                | "wl" ->
                    let bm, indices, values = fetchForWrite respSocket
                    bm.Verify(indices |> map (fun n -> n * 8), values.Length / 8)

                    Array.zip indices (ByteConverter.BytesToTypeArray<uint64>(values)) |> iter ( fun (index, value) -> bm.writeU64(index, value))
                    bm.Flush()
                    Ok (WriteOK())

                | "cl" ->
                    let name = respSocket.ReceiveFrameString().ToLower()
                    let bm = bufferManagers[name]
                    bm.clear()
                    bm.Flush()
                    Ok (WriteOK())
                | _ ->
                    Error $"Unknown request: {request}"




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
                        | Ok obj ->

                            //if obj = null || obj :? WriteOK then
                            //    noop()
                            //else
                            //    noop()

                            match obj with
                            | null
                            | :? NoMoreInputOK ->
                                // 현재, request 가 없는 경우
                                // Async.Sleep(???)
                                ()

                            | _ ->
                                let more = respSocket.SendMoreFrame("OK")
                                match obj with
                                | :? WriteOK as ok ->
                                    more.SendFrame("OK")
                                | :? string as ok ->
                                    more.SendFrame(ok)
                                | :? byte as ok ->
                                    more.SendFrame([|ok|])
                                | :? uint16 as ok ->
                                    more.SendFrame(BitConverter.GetBytes(ok))
                                | :? uint32 as ok ->
                                    more.SendFrame(BitConverter.GetBytes(ok))
                                | :? uint64 as ok ->
                                    more.SendFrame(BitConverter.GetBytes(ok))
                                | _ ->
                                    let t = obj.GetType()
                                    let isArray = t.IsArray
                                    let objType = t.GetElementType()
                                    verify isArray
                                    if objType = typeof<byte> then
                                        more.SendFrame(obj :?> byte[])
                                    elif objType = typeof<uint16> then
                                        more.SendFrame(ByteConverter.ToBytes<uint16>(obj :?> uint16[]))
                                    elif objType = typeof<uint32> then
                                        more.SendFrame(ByteConverter.ToBytes<uint32>(obj :?> uint32[]))
                                    elif objType = typeof<uint64> then
                                        more.SendFrame(ByteConverter.ToBytes<uint64>(obj :?> uint64[]))
                                    else
                                        failwithlogf "ERROR"

                        | Error errMsg ->
                            respSocket.SendMoreFrame("ERR").SendFrame(errMsg)
                    with ex ->
                        logError $"Error occured while handling request: {ex.Message}"
                        respSocket.SendMoreFrame("ERR").SendFrame(ex.Message)

                logInfo("Cancellation request detected!")
                (x :> IDisposable).Dispose()
                terminated <- true

            Task.Factory.StartNew(f, TaskCreationOptions.LongRunning)

        interface IDisposable with
            member x.Dispose() =
                logDebug "Disposing server..."
                bufferManagers.Values |> iter (fun stream -> stream.FileStream.Dispose())
