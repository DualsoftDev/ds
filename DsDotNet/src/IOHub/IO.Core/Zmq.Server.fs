(* IO.Core using Zero MQ *)

namespace IO.Core
open System
open System.Linq
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
    /// 서버에서 socket message 를 처리후, client 에게 socket 으로 보내기 전에 갖는 result type
    type IResponse = interface end
    type IResponseOK = inherit IResponse
    /// Request 문법 오류
    type IResponseNG = inherit IResponse
    type IResponseNoMoreInput = inherit IResponseOK
    type IResponseWithClientRquestInfo = 
        inherit IResponse
        abstract ClientRequestInfo : ClientRequestInfo
    [<AbstractClass>]
    type Response(cri:ClientRequestInfo) =
        interface IResponseWithClientRquestInfo with
            member x.ClientRequestInfo = cri
        member x.ClientRequestInfo = cri

    [<AbstractClass>]
    type StringResponse(cri:ClientRequestInfo, message:string) =
        inherit Response(cri)
        member x.Message = message

    type ResponseNoMoreInput() =
        interface IResponseNoMoreInput
    type ResponseOK() =
        interface IResponseOK

    type StringResponseOK(cri:ClientRequestInfo, message:string) =
        inherit StringResponse(cri, message)
        interface IResponseOK
    type StringResponseNG(cri:ClientRequestInfo, message:string) =
        inherit StringResponse(cri, message)
        interface IResponseNG

    type WriteResponseOK(cri:ClientRequestInfo, ioFIleSpec:IOFileSpec, contentBitSize:PLCMemoryBitSize, offsets:int[], changedValues:obj) =
        inherit Response(cri)
        interface IResponseOK
        member x.ChangedValues = changedValues
        member x.ContentBitSize = contentBitSize
        member x.Offsets = offsets
        member x.FIleSpec = ioFIleSpec

    type SingleValueChange = IOFileSpec * PLCMemoryBitSize * int array * obj   // dataType, offset, value
    type WriteHeterogeniousResponseOK(cri:ClientRequestInfo, spotChanges:SingleValueChange seq) =
        inherit Response(cri)
        interface IResponseOK
        member val SpotChanges = spotChanges |> toArray


    type ReadResponseOK(cri:ClientRequestInfo, dataType:PLCMemoryBitSize, values:obj) =
        inherit Response(cri)
        interface IResponseOK
        member x.Values = values
        member x.DataType = dataType


    let mutable ioSpec = getNull<IOSpec>()
    /// e.g {"p/o", <Paix Output Buffer manager>}
    let streamManagers = new Dictionary<string, StreamManager>()

    /// tag 별 address 정보를 저장하는 dictionary
    let tagDic = new Dictionary<string, AddressSpec>()
    let clients = ResizeArray<ClientIdentifier>()
    let mutable serverSocket:RouterSocket = null

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
    let writeAddressWithValue(clientRequstInfo:ClientRequestInfo, addressWithAssignValue:string) : SingleValueChange =
        let cri = clientRequstInfo
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
            bufferManager.VerifyIndices(cri, [|byteOffset|])

            let mutable offset = byteOffset
            let mutable objValue:obj = null
            match ap.DataType with
            | PLCMemoryBitSize.Bit   -> objValue <- [|parseBool(value)|];    bufferManager.writeBit (cri, byteOffset, ap.OffsetBit, parseBool(value)); offset <- byteOffset * 8 + ap.OffsetBit
            | PLCMemoryBitSize.Byte  -> objValue <- [|Byte.Parse(value)|];   bufferManager.writeU8s cri ([byteOffset, Byte.Parse(value)])
            | PLCMemoryBitSize.Word  -> objValue <- [|UInt16.Parse(value)|]; bufferManager.writeU16 cri (byteOffset, UInt16.Parse(value))
            | PLCMemoryBitSize.DWord -> objValue <- [|UInt32.Parse(value)|]; bufferManager.writeU32 cri (byteOffset, UInt32.Parse(value))
            | PLCMemoryBitSize.LWord -> objValue <- [|UInt64.Parse(value)|]; bufferManager.writeU64 cri (byteOffset, UInt64.Parse(value))
            | _ -> failwithf($"Unknown data type : {ap.DataType}")

            let fs = bufferManager.FileSpec
            fs, ap.DataType, [|offset|], objValue

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

    let periodicPingClients() =
        Observable.Interval(TimeSpan.FromSeconds(3)).Subscribe(fun n -> 
            for client in clients do
                serverSocket
                    .SendMoreFrame(client)
                    .SendMoreFrame(-1 |> ByteConverter.ToBytes)
                    .SendFrame("PING")
            ()
        )


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

        let ioChangedSubject = new Subject<IOChangeInfo>()
        //let mutable ioChangedObservable:IObservable<IOChangeInfo> = null

        let notifyIoChange(ioChange:IOChangeInfo) =
                Console.WriteLine($"change by client {clientIdentifierToString ioChange.ClientRequestInfo.ClientId}");
                let notiTargetClients =
                    clients
                    |> filter (fun c -> not <| Enumerable.SequenceEqual(c, ioChange.ClientRequestInfo.ClientId))
                    |> toArray
                let notifyToClients() =
                    let bm = ioChange.IOFileSpec
                    let contenetBitLength = int ioChange.DataType
                    let criminalClientId = ioChange.ClientRequestInfo.ClientId
                    let bytes = ioChange.GetValueBytes()
                    let path = ioChange.IOFileSpec.GetPath()
                    let offsets = ioChange.Offsets |> ByteConverter.ToBytes<int>
                    for client in notiTargetClients do
                        Console.WriteLine($"Notifying change to client {clientIdentifierToString client}");
                        serverSocket
                            .SendMoreFrame(client)
                            .SendMoreFrame(-1 |> ByteConverter.ToBytes)
                            .SendMoreFrame("NOTIFY")
                            .SendMoreFrame(bytes) // value
                            .SendMoreFrame(path)  // name
                            .SendMoreFrame(contenetBitLength |> ByteConverter.ToBytes)  // contentBitLength
                            .SendFrame(offsets) // offsets

                notifyToClients()

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

            //ioChangedObservable <-
            //    streamManagers.Values 
            //    |> map (fun sm -> sm.IOChangedSubject :> IObservable<IOChangeInfo>)
            //    |> Observable.Merge

            // TODO
            //ioChangedObservable.Subscribe(fun (ioChange:IOChangeInfo) ->
            //ioChangedSubject.Subscribe(fun (ioChange:IOChangeInfo) ->

            //) |> ignore
            
        let mutable terminated = false

        member x.IsTerminated with get() = terminated

        //member x.IOChangedObservable = ioChangedSubject
        member x.Clients = clients

        member private x.handleRequest (server:RouterSocket) : IResponse =    // ClientIdentifier * int * IOResult =
            let mutable mqMessage:NetMQMessage = null
            if not <| server.TryReceiveMultipartMessage(&mqMessage) then
                ResponseNoMoreInput()
            else
                let clientId = mqMessage[ClientMultiMessage.ClientId].Buffer;  // byte[]로 받음
                let reqId = mqMessage[ClientMultiMessage.RequestId].Buffer |> BitConverter.ToInt32
                /// Client Request Info : clientId, requestId
                let cri = ClientRequestInfo(clientId, reqId)

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
                        logDebug $"Client {clientIdentifierToString clientId} registered"
                        StringResponseOK(cri, "OK")
                    | "UNREGISTER" ->
                        clients.Remove clientId |> ignore
                        logDebug $"Client {clientIdentifierToString clientId} unregistered"
                        StringResponseOK(cri, "OK")
                    | "PONG" ->
                        logDebug $"Got pong from client {clientIdentifierToString clientId}"
                        ResponseOK()
                    | StartsWith "read" ->      // e.g read p/ob1 p/ow1 p/olw3
                        noop()
                        let result =
                            getArgs() |> map (fun a -> $"{a}={readAddress(cri, a)}")
                            |> joinWith " "
                        ReadResponseOK(cri, PLCMemoryBitSize.Undefined, result)
                    | StartsWith "write" ->
                        let changes = getArgs() |> map (fun a -> writeAddressWithValue(cri, a))
                        WriteHeterogeniousResponseOK(cri, changes)
                    | StartsWith "cl" ->
                        let name = getArgs() |> Seq.exactlyOne
                        let bm = streamManagers[name]
                        bm.clear()
                        StringResponseOK(cri, "OK")
                    | _ ->
                        failwithlogf $"ERROR: {command}"
                | 2 ->
                    match command with
                    | "rx" ->
                        let bm, indices = fetchForReadBit mms
                        bm.VerifyIndices(cri, indices |> map (fun n -> n / 8))
                        let result = bm.readBits indices
                        ReadResponseOK(cri, PLCMemoryBitSize.Bit, result)
                    | "rb" ->
                        let bm, indices = fetchForRead mms
                        bm.VerifyIndices(cri, indices)
                        let result = bm.readU8s indices
                        ReadResponseOK(cri, PLCMemoryBitSize.Byte, result)

                    | "rw" ->
                        let bm, indices = fetchForRead mms
                        bm.VerifyIndices(cri, indices |> map (fun n -> n * 2))
                        let result = bm.readU16s indices
                        ReadResponseOK(cri, PLCMemoryBitSize.Word, result)

                    | "rd" ->
                        let bm, indices = fetchForRead mms
                        bm.VerifyIndices(cri, indices |> map (fun n -> n * 4))
                        let result = bm.readU32s indices
                        ReadResponseOK(cri, PLCMemoryBitSize.DWord, result)

                    | "rl" ->
                        let bm, indices = fetchForRead mms
                        bm.VerifyIndices(cri, indices |> map (fun n -> n * 8))
                        let result = bm.readU64s indices
                        ReadResponseOK(cri, PLCMemoryBitSize.LWord, result)
                    | _ ->
                        failwithlogf $"ERROR: {command}"
                | 3 ->
                    match command with
                    | "wx" ->
                        let bm, indices, values = fetchForWrite mms
                        bm.Verify(cri, indices |> map (fun n -> n / 8), values.Length)

                        for i in [0..indices.Length-1] do
                            let value =
                                match values.[i] with
                                | 1uy -> true
                                | 0uy -> false
                                | _ -> failwithf($"Invalid value: {values.[i]}")
                            bm.writeBit (cri, indices[i], value)

                        bm.Flush()
                        WriteResponseOK(cri, bm.FileSpec, PLCMemoryBitSize.Bit, indices, values)
                    | "wb" ->
                        let bm, indices, values = fetchForWrite mms
                        bm.Verify(cri, indices, values.Length / 1)
                        Array.zip indices values |> bm.writeU8s cri
                        WriteResponseOK(cri, bm.FileSpec, PLCMemoryBitSize.Byte, indices, values)

                    | "ww" ->
                        let bm, indices, values = fetchForWrite mms
                        bm.Verify(cri, indices |> map (fun n -> n * 2), values.Length / 2)

                        Array.zip indices (ByteConverter.BytesToTypeArray<uint16>(values)) |> bm.writeU16s cri
                        WriteResponseOK(cri, bm.FileSpec, PLCMemoryBitSize.Byte, indices, values)

                    | "wd" ->
                        let bm, indices, values = fetchForWrite mms
                        bm.Verify(cri, indices |> map (fun n -> n * 4), values.Length / 4)
                        Array.zip indices (ByteConverter.BytesToTypeArray<uint32>(values)) |> bm.writeU32s cri
                        WriteResponseOK(cri, bm.FileSpec, PLCMemoryBitSize.Byte, indices, values)

                    | "wl" ->
                        let bm, indices, values = fetchForWrite mms
                        bm.Verify(cri, indices |> map (fun n -> n * 8), values.Length / 8)

                        Array.zip indices (ByteConverter.BytesToTypeArray<uint64>(values)) |> bm.writeU64s cri
                        WriteResponseOK(cri, bm.FileSpec, PLCMemoryBitSize.Byte, indices, values)
                    | _ ->
                        failwithlogf $"ERROR: {command}"

                | _ ->
                    failwithlogf $"ERROR: {command}"


        member x.Run() =
            // start a separate thread to run the server
            let f() =
                logInfo $"Starting server on port {port}..."
                use server = new RouterSocket()
                serverSocket <- server
                
                //server.Bind($"tcp://*:{port}")
                server.Bind($"tcp://localhost:{port}")
                
                //periodicPingClients()

                while not cancellationToken.IsCancellationRequested do
                    try
                        let response = x.handleRequest server
                        match response with
                        | :? ResponseNoMoreInput ->
                            // 현재, request 가 없는 경우
                            // Async.Sleep(???)
                            ()
                        | :? StringResponseNG as r ->
                            let cri:ClientRequestInfo = r.ClientRequestInfo
                            let clientId = cri.ClientId
                            let reqId = cri.RequestId
                            server
                                .SendMoreFrame(clientId)
                                .SendMoreFrame(reqId |> ByteConverter.ToBytes)
                                .SendMoreFrame("ERR")
                                .SendFrame(r.Message)

                        | :? IResponseWithClientRquestInfo as r ->
                            let clientId = r.ClientRequestInfo.ClientId
                            let reqId = r.ClientRequestInfo.RequestId
                            let more =
                                server
                                    .SendMoreFrame(clientId)
                                    .SendMoreFrame(reqId |> ByteConverter.ToBytes)  //.SendMoreFrameWithRequestId(reqId)
                                    .SendMoreFrame("OK")
                            match r with
                            | :? StringResponse as r ->
                                more.SendFrame(r.Message)
                            | :? ReadResponseOK as r ->
                                let values, dataType = r.Values, r.DataType

                                if dataType = PLCMemoryBitSize.Undefined then
                                    more.SendFrame(r.Values :?> string)
                                else
                                    verify (values.GetType().IsArray)
                                    let moreBytes =
                                        match dataType with
                                        | PLCMemoryBitSize.Bit   -> values :?> bool[] |> map (fun b -> if b then 1uy else 0uy)
                                        | PLCMemoryBitSize.Byte  -> values :?> byte[]
                                        | PLCMemoryBitSize.Word  -> values :?> uint16[] |> ByteConverter.ToBytes<uint16>
                                        | PLCMemoryBitSize.DWord -> values :?> uint32[] |> ByteConverter.ToBytes<uint32>
                                        | PLCMemoryBitSize.LWord -> values :?> uint64[] |> ByteConverter.ToBytes<uint64>
                                        | _ -> failwithlogf "ERROR"

                                    more.SendFrame(moreBytes)

                            | :? WriteResponseOK as r ->
                                more.SendFrame("OK")
                                // TODO : uncomment
                                IOChangeInfo(r.ClientRequestInfo, r.FIleSpec, r.ContentBitSize,  r.Offsets, r.ChangedValues) |> notifyIoChange
                            | :? WriteHeterogeniousResponseOK as r ->
                                more.SendFrame("OK")
                                for ch in r.SpotChanges do
                                    let fs, dataType, offsets, value = ch
                                    let xxx = r.ClientRequestInfo
                                    IOChangeInfo(r.ClientRequestInfo, fs, dataType, offsets, value) |> notifyIoChange
                            | _ ->
                                failwith "Not Yet!!"

                        | _ ->
                            failwith "Not Yet!!"
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
