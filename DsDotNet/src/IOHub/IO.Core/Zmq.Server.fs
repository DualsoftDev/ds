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
open System.Reactive.Subjects
open System.Reactive.Linq
open IO.Spec



[<AutoOpen>]
module private ZmqServerImplModule =
    let mutable ioSpec = getNull<IOSpec>()
    /// e.g {"p/o", <Paix Output Buffer manager>}
    let streamManagers = new Dictionary<string, StreamManager>()

    /// tag 별 address 정보를 저장하는 dictionary
    let tagDic = new Dictionary<string, AddressSpec>()
    let clients = ResizeArray<ClientIdentifier>()

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

    let readAddress(clientRequstInfo:ClientRequestInfo, address:string) : obj =
        match address with
        | AddressPattern ap ->
            let byteOffset = ap.OffsetByte
            let bufferManager = ap.IOFileSpec.StreamManager :?> StreamManager
            bufferManager.VerifyIndices(clientRequstInfo, [|byteOffset|])

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
    let writeAddressWithValue(clientRequstInfo:ClientRequestInfo, addressWithAssignValue:string) =
        let parseBool (s:string) =
            match s.ToLower() with
            | "1" | "true" -> true
            | "0" | "false" -> false
            | _ -> failwithf($"Invalid boolean value: {s}")
        match addressWithAssignValue with
        | AddressAssignPattern (addressPattern, value) ->
            let ap = addressPattern
            let byteOffset = ap.OffsetByte
            let bufferManager = ap.IOFileSpec.StreamManager :?> StreamManager
            bufferManager.VerifyIndices(clientRequstInfo, [|byteOffset|])

            match ap.DataType with
            | PLCMemoryBitSize.Bit   -> bufferManager.writeBit(byteOffset, ap.OffsetBit, parseBool(value))
            | PLCMemoryBitSize.Byte  -> bufferManager.writeU8s([byteOffset, Byte.Parse(value)])
            | PLCMemoryBitSize.Word  -> bufferManager.writeU16(byteOffset, UInt16.Parse(value))
            | PLCMemoryBitSize.DWord -> bufferManager.writeU32(byteOffset, UInt32.Parse(value))
            | PLCMemoryBitSize.LWord -> bufferManager.writeU64(byteOffset, UInt64.Parse(value))
            | _ -> failwithf($"Unknown data type : {ap.DataType}")

            bufferManager.Flush()

        | _ -> failwithf($"Unknown address with assignment pattern : {addressWithAssignValue}")

    /// Client 로부터 받은 multi-message format
    [<AutoOpen>]
    module internal ClientMultiMessage =
        let ClientId  = 0
        let RequestId = 1
        let Command   = 2
        let ArgGroup1 = 3
        let ArgGroup2 = 4
        let ArgGroup3 = 5

        let TagKindName = ArgGroup1
        let Offsets     = ArgGroup2
        let Values      = ArgGroup3


    let fetchBufferManagerAndIndices (isBitIndex:bool) (multiMessages:NetMQFrame[]) =
        let bufferManager =
            let name = multiMessages[TagKindName].ConvertToString().ToLower()
            streamManagers[name]
        let indices = ByteConverter.BytesToTypeArray<int>(multiMessages[Offsets].Buffer)
        for byteIndex in indices |> map (fun n -> if isBitIndex then n / 8 else n) do
            if bufferManager.FileStream.Length < byteIndex then
                failwithf($"Invalid address: {byteIndex}")
        bufferManager, indices

    let fetchForRead = fetchBufferManagerAndIndices false
    let fetchForReadBit = fetchBufferManagerAndIndices true

    let fetchForWrite (multiMessages:NetMQFrame[]) =
        let bm, indices = fetchForRead multiMessages
        let values = multiMessages[Values].Buffer
        bm, indices, values

    /// NetMQ 의 ConvertToString() bug 대응 용 코드.  문자열의 맨 마지막에 '\0' 이 붙는 경우 강제 제거.
    let removeTrailingNullChar (str:string) =
        if str[str.Length-1] = '\000' then
            str.Substring(0, str.Length - 1)
        else
            str

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


module ZmqServerModule =
    [<AllowNullLiteral>]
    type Server(ioSpec_:IOSpec, cancellationToken:CancellationToken) =
        
        let port = ioSpec_.ServicePort

        let mutable ioChangedObservable:IObservable<IOChangeInfo> = null

        do
            ioSpec <- ioSpec_
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
                    let streamManager = new StreamManager(f)
                    let key = if v.Location.NonNullAny() then $"{v.Location}/{f.Name}" else f.Name
                    streamManagers.Add(key, streamManager)

            ioChangedObservable <-
                streamManagers.Values 
                |> map (fun sm -> sm.IOChangedSubject :> IObservable<IOChangeInfo>)
                |> Observable.Merge
            
        let mutable terminated = false

        member x.IsTerminated with get() = terminated

        member x.IOChangedObservable = ioChangedObservable
        member x.Clients = clients

        member private x.handleRequest (server:RouterSocket) : ClientIdentifier * int * IOResult =
            let mutable mqMessage:NetMQMessage = null
            if not <| server.TryReceiveMultipartMessage(&mqMessage) then
                null, -1, Ok null
            else
                let clientId = mqMessage[ClientMultiMessage.ClientId].Buffer;  // byte[]로 받음
                let reqId = mqMessage[ClientMultiMessage.RequestId].Buffer |> BitConverter.ToInt32
                let clientRequstInfo:ClientRequestInfo = {ClientId = clientId; RequestId=reqId}
                let ioResult =
                    let mms = mqMessage |> toArray
                    let command = mqMessage[ClientMultiMessage.Command].ConvertToString() |> removeTrailingNullChar;
                    logDebug $"Handling request: {command}"

                    let getArgs() =
                        let tokens = command.Split(' ', StringSplitOptions.RemoveEmptyEntries) |> Array.ofSeq
                        tokens[1..] |> map(fun s -> s.ToLower())

                    // client 에서 오는 모든 메시지는 client ID, requestId 와 command frame 을 기본 포함하므로, 
                    // 이 3개의 frame 을 제외한 frame 의 갯수에 따라 message 를 처리한다.
                    match mms.length() - 3 with
                    | 0 ->
                        match command with
                        | "REGISTER" ->
                            clients.Add clientId
                            Ok null
                        | "UNREGISTER" ->
                            clients.Remove clientId |> ignore
                            Ok null
                        | StartsWith "read" ->      // e.g read p/ob1 p/ow1 p/olw3
                            noop()
                            let result =
                                getArgs() |> map (fun a -> $"{a}={readAddress(clientRequstInfo, a)}")
                                |> joinWith " "
                            Ok (box result)
                        | StartsWith "write" ->
                            getArgs() |> iter (fun a -> writeAddressWithValue(clientRequstInfo, a))
                            Ok (WriteOK())
                        | StartsWith "cl" ->
                            let name = getArgs() |> Seq.exactlyOne
                            let bm = streamManagers[name]
                            bm.clear()
                            Ok (box (WriteOK()))
                        | _ ->
                            failwithlogf $"ERROR: {command}"
                    | 2 ->
                        match command with
                        | "rx" ->
                            let bm, indices = fetchForReadBit mms
                            bm.VerifyIndices(clientRequstInfo, indices |> map (fun n -> n / 8))
                            let result = bm.readBits indices
                            Ok (box result)
                        | "rb" ->
                            let bm, indices = fetchForRead mms
                            bm.VerifyIndices(clientRequstInfo, indices)
                            let result = bm.readU8s indices
                            Ok result

                        | "rw" ->
                            let bm, indices = fetchForRead mms
                            bm.VerifyIndices(clientRequstInfo, indices |> map (fun n -> n * 2))
                            let result = bm.readU16s indices
                            Ok result

                        | "rd" ->
                            let bm, indices = fetchForRead mms
                            bm.VerifyIndices(clientRequstInfo, indices |> map (fun n -> n * 4))
                            let result = bm.readU32s indices
                            Ok result

                        | "rl" ->
                            let bm, indices = fetchForRead mms
                            bm.VerifyIndices(clientRequstInfo, indices |> map (fun n -> n * 8))
                            let result = bm.readU64s indices
                            Ok result
                        | _ ->
                            failwithlogf $"ERROR: {command}"
                    | 3 ->
                        let writeOK = Ok (box (WriteOK()))
                        match command with
                        | "wx" ->
                            let bm, indices, values = fetchForWrite mms
                            bm.Verify(clientRequstInfo, indices |> map (fun n -> n / 8), values.Length)

                            for i in [0..indices.Length-1] do
                                let value =
                                    match values.[i] with
                                    | 1uy -> true
                                    | 0uy -> false
                                    | _ -> failwithf($"Invalid value: {values.[i]}")
                                bm.writeBit(indices[i], value)

                            bm.Flush()
                            writeOK
                        | "wb" ->
                            let bm, indices, values = fetchForWrite mms
                            bm.Verify(clientRequstInfo, indices, values.Length / 1)
                            Array.zip indices values |> bm.writeU8s
                            writeOK

                        | "ww" ->
                            let bm, indices, values = fetchForWrite mms
                            bm.Verify(clientRequstInfo, indices |> map (fun n -> n * 2), values.Length / 2)

                            Array.zip indices (ByteConverter.BytesToTypeArray<uint16>(values)) |> bm.writeU16s
                            writeOK

                        | "wd" ->
                            let bm, indices, values = fetchForWrite mms
                            bm.Verify(clientRequstInfo, indices |> map (fun n -> n * 4), values.Length / 4)
                            Array.zip indices (ByteConverter.BytesToTypeArray<uint32>(values)) |> bm.writeU32s
                            writeOK

                        | "wl" ->
                            let bm, indices, values = fetchForWrite mms
                            bm.Verify(clientRequstInfo, indices |> map (fun n -> n * 8), values.Length / 8)

                            Array.zip indices (ByteConverter.BytesToTypeArray<uint64>(values)) |> bm.writeU64s
                            writeOK
                        | _ ->
                            failwithlogf $"ERROR: {command}"

                    | _ ->
                        failwithlogf $"ERROR: {command}"
                clientId, reqId, ioResult


        member x.Run() =
            // start a separate thread to run the server
            let f() =
                logInfo $"Starting server on port {port}..."
                use server = new RouterSocket()
                server.Bind($"tcp://*:{port}")
                
                while not cancellationToken.IsCancellationRequested do
                    try
                        let clientId, reqId, response = x.handleRequest server
                        match response with
                        | Ok obj ->

                            if obj = null || obj :? WriteOK then
                                noop()
                            else
                                noop()

                            match obj with
                            | null
                            | :? NoMoreInputOK ->
                                // 현재, request 가 없는 경우
                                // Async.Sleep(???)
                                ()

                            | _ ->
                                let more =
                                    server
                                        .SendMoreFrame(clientId)
                                        .SendMoreFrame(reqId |> ByteConverter.ToBytes)  //.SendMoreFrameWithRequestId(reqId)
                                        .SendMoreFrame("OK")
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
                                    if objType = typeof<bool> then
                                        more.SendFrame(obj :?> bool[] |> map (fun b -> if b then 1uy else 0uy))
                                    elif objType = typeof<byte> then
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
                            server
                                .SendMoreFrame(clientId)
                                .SendMoreFrame(reqId |> ByteConverter.ToBytes)
                                .SendMoreFrame("ERR")
                                .SendFrame(errMsg)
                    with 
                    | :? ExcetionWithClient as ex ->
                        logError $"Error occured while handling request: {ex.Message}"
                        server
                            .SendMoreFrame(ex.ClientId)
                            .SendMoreFrame(ex.RequestId |> ByteConverter.ToBytes)
                            .SendMoreFrame("ERR")
                            .SendFrame(ex.Message)
                    | ex ->
                        logError $"Error occured while handling request: {ex.Message}"

                logInfo("Cancellation request detected!")
                (x :> IDisposable).Dispose()
                terminated <- true

            Task.Factory.StartNew(f, TaskCreationOptions.LongRunning)

        interface IDisposable with
            member x.Dispose() =
                logDebug "Disposing server..."
                streamManagers.Values |> iter (fun stream -> stream.FileStream.Dispose())
                streamManagers.Clear()
