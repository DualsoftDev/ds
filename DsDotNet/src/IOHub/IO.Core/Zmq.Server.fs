(* IO.Core using Zero MQ *)

namespace IO.Core
open System
open System.Linq
open System.Threading
open System.Threading.Tasks
open NetMQ
open NetMQ.Sockets
open Dual.Common.Core.FS
open System.IO
open System.Runtime.Remoting
open System.Reactive.Subjects
open IO.Spec

module ZmqServerModule =
    [<AllowNullLiteral>]
    type Server(ioSpec_:IOSpec, cancellationToken:CancellationToken) =
        
        let port = ioSpec_.ServicePort

        let ioChangedSubject = new Subject<IOChangeInfo>()

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
                ioChangedSubject.OnNext ioChange
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
            
        let mutable terminated = false

        member x.IsTerminated with get() = terminated

        member x.IOChangedObservable = ioChangedSubject
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

                try
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
                            StringResponseNG(cri, $"ERROR: {command}")
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
                            StringResponseNG(cri, $"ERROR: {command}")
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
                            WriteResponseOK(cri, ValuesChangeInfo(bm.FileSpec, PLCMemoryBitSize.Bit, indices, values))
                        | "wb" ->
                            let bm, indices, values = fetchForWrite mms
                            bm.Verify(cri, indices, values.Length / 1)
                            Array.zip indices values |> bm.writeU8s cri
                            WriteResponseOK(cri, ValuesChangeInfo(bm.FileSpec, PLCMemoryBitSize.Byte, indices, values))

                        | "ww" ->
                            let bm, indices, values = fetchForWrite mms
                            bm.Verify(cri, indices |> map (fun n -> n * 2), values.Length / 2)

                            Array.zip indices (ByteConverter.BytesToTypeArray<uint16>(values)) |> bm.writeU16s cri
                            WriteResponseOK(cri, ValuesChangeInfo(bm.FileSpec, PLCMemoryBitSize.Byte, indices, values))

                        | "wd" ->
                            let bm, indices, values = fetchForWrite mms
                            bm.Verify(cri, indices |> map (fun n -> n * 4), values.Length / 4)
                            Array.zip indices (ByteConverter.BytesToTypeArray<uint32>(values)) |> bm.writeU32s cri
                            WriteResponseOK(cri, ValuesChangeInfo(bm.FileSpec, PLCMemoryBitSize.Byte, indices, values))

                        | "wl" ->
                            let bm, indices, values = fetchForWrite mms
                            bm.Verify(cri, indices |> map (fun n -> n * 8), values.Length / 8)

                            Array.zip indices (ByteConverter.BytesToTypeArray<uint64>(values)) |> bm.writeU64s cri
                            WriteResponseOK(cri, ValuesChangeInfo(bm.FileSpec, PLCMemoryBitSize.Byte, indices, values))
                        | _ ->
                            StringResponseNG(cri, $"ERROR: {command}")

                    | _ ->
                        StringResponseNG(cri, $"ERROR: {command}")
                with ex ->
                    StringResponseNG(cri, ex.Message)

        member x.Run() =
            // start a separate thread to run the server
            let f() =
                logInfo $"Starting server on port {port}..."
                use server = new RouterSocket()
                serverSocket <- server
                
                server.Bind($"tcp://*:{port}")
                
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
                                .SendMoreFrameWithRequestId(reqId)
                                .SendMoreFrame("ERR")
                                .SendFrame(r.Message)

                        | :? IResponseWithClientRquestInfo as r ->
                            let clientId = r.ClientRequestInfo.ClientId
                            let reqId = r.ClientRequestInfo.RequestId
                            let more =
                                server
                                    .SendMoreFrame(clientId)
                                    .SendMoreFrameWithRequestId(reqId)
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
