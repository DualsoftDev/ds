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
open Newtonsoft.Json

[<AllowNullLiteral>]
type Server(ioSpec_: IOSpec, cancellationToken: CancellationToken) =

    let port = ioSpec_.ServicePort

    let memoryChangedSubject = new Subject<IMemoryChangeInfo>()

    let notifyIoChange (memChange: IMemoryChangeInfo) =
        let cri = memChange.ClientRequestInfo :?> ClientRequestInfo
        Console.WriteLine($"change by client (Count:{clients.Count()}) {clientIdentifierToString cri.ClientId}")

        let notiTargetClients =
            clients
            |> filter (fun c -> not <| Enumerable.SequenceEqual(c, cri.ClientId))
            |> toArray

        let notifyToClients () =
            let path = memChange.IOFileSpec.GetPath()

            match memChange with
            | :? SingleStringChangeInfo as strChange ->
                for client in notiTargetClients do
                    Console.WriteLine($"Notifying change to client {clientIdentifierToString client}")
                    let key = strChange.Keys[0]
                    let value = strChange.Value[0]

                    serverSocket
                        .SendMoreFrame(client)
                        .SendMoreFrame(-1 |> ByteConverter.ToBytes)
                        .SendMoreFrame("NOTIFY")
                        .SendMoreFrame(value) // value
                        .SendMoreFrame(path) // name
                        .SendMoreFrame(int MemoryType.String |> ByteConverter.ToBytes) // contentBitLength
                        .SendFrame(key) // offsets
            | :? IOChangeInfo as ioChange ->
                let contenetBitLength = int ioChange.MemoryType
                let bytes = ioChange.GetValueBytes()
                let offsets = ioChange.Offsets |> ByteConverter.ToBytes<int>

                for client in notiTargetClients do
                    Console.WriteLine($"Notifying change to client {clientIdentifierToString client}")

                    serverSocket
                        .SendMoreFrame(client)
                        .SendMoreFrame(-1 |> ByteConverter.ToBytes)
                        .SendMoreFrame("NOTIFY")
                        .SendMoreFrame(bytes) // value
                        .SendMoreFrame(path) // name
                        .SendMoreFrame(contenetBitLength |> ByteConverter.ToBytes) // contentBitLength
                        .SendFrame(offsets) // offsets
            | _ -> failwithlog "ERROR"

        notifyToClients ()
        memoryChangedSubject.OnNext memChange

    do
        ioSpec <- ioSpec_

        for v in ioSpec.Vendors do
            v.AddressResolver <-
                let dllPath = if File.Exists(v.Dll) then v.Dll else Path.Combine(AppContext.BaseDirectory, v.Dll)
                let oh: ObjectHandle = Activator.CreateInstanceFrom(dllPath, v.ClassName)
                let obj: obj = oh.Unwrap()
                obj :?> IAddressInfoProvider

            //showSamples v v.AddressResolver

            for f in v.Files do
                f.Vendor <- v
                let dir, key =
                    match v.Location with
                    | ""
                    | null -> ioSpec.TopLevelLocation, f.Name
                    | _ -> Path.Combine(ioSpec.TopLevelLocation, v.Location), $"{v.Location}/{f.Name}"

                f.InitiaizeFile(dir)
                let streamManager = new StreamManager(f)

                let key =
                    if v.Location.NonNullAny() then
                        $"{v.Location}/{f.Name}"
                    else
                        f.Name
                //경로가 있어서 항상 소문자로 관리
                streamManagers.Add(key.ToLower(), streamManager)

    let mutable terminated = false

    member x.IsTerminated = terminated

    member x.MemoryChangedObservable = memoryChangedSubject
    member x.Clients = clients

    member private x.handleRequest(server: RouterSocket) : IResponse = // ClientIdentifier * int * IOResult =
        let mutable mqMessage: NetMQMessage = null

        if not <| server.TryReceiveMultipartMessage(&mqMessage) then
            ResponseNoMoreInput()
        else
            let mms = mqMessage |> toArray
            let clientId = mms[MultiMessageFromClient.ClientId].Buffer // byte[]로 받음

            let command = mms[MultiMessageFromClient.Command].ConvertToString() |> removeTrailingNullChar

            let reqId = (mms[MultiMessageFromClient.RequestId].Buffer, 0) |> BitConverter.ToInt32
            /// Client Request Info : clientId, requestId
            let cri = ClientRequestInfo(clientId, reqId)

            logDebug $"Handling request: {command}"

            let getArgs () =
                let tokens =
                    command.Split([|' '|], StringSplitOptions.RemoveEmptyEntries) |> Array.ofSeq

                tokens[1..] |> map (fun s -> s.ToLower())

            try
                // client 에서 오는 모든 메시지는 client ID, requestId 와 command frame 을 기본 포함하므로,
                // 이 3개의 frame 을 제외한 frame 의 갯수에 따라 message 를 처리한다.
                match mms.length () - 3 with
                | 0 ->
                    match command with
                    | "REGISTER" ->
                        clients <- clientId :: clients
                        logDebug $"Client {clientIdentifierToString clientId} registered"

                        let endian =
                            if BitConverter.IsLittleEndian then
                                "LittleEndian"
                            else
                                "BigEndian"

                        StringResponseOK(cri, endian)
                    | "UNREGISTER" ->
                        clients <- clients |> List.except [ clientId ]
                        logDebug $"Client {clientIdentifierToString clientId} unregistered"
                        StringResponseOK(cri, "OK")
                    | "META" ->
                        let meta = JsonConvert.SerializeObject(ioSpec)
                        StringResponseOK(cri, meta)
                    | "PONG" ->
                        logDebug $"Got pong from client {clientIdentifierToString clientId}"
                        ResponseOK()
                    | StartsWith "read" -> // e.g read p/ob1 p/ow1 p/olw3
                        try
                            let result =
                                getArgs () |> map (fun a -> $"{a}={readAddress (cri, a)}") |> joinWith " "

                            ReadResponseOK(cri, MemoryType.Undefined, result)
                        with ex ->
                            StringResponseNG(cri, ex.Message)
                    | StartsWith "write" ->
                        let changes = getArgs () |> map (fun a -> writeAddressWithValue (cri, a))
                        WriteHeterogeniousResponseOK(cri, changes)
                    | StartsWith "cl" ->
                        let name = getArgs () |> Seq.exactlyOne
                        let bm = streamManagers[name]
                        bm.clear ()
                        StringResponseOK(cri, "OK")
                    | _ -> StringResponseNG(cri, $"ERROR: {command}")
                | 2 ->
                    match command with
                    | "rx" ->
                        let bm, offsets = fetchForReadBit mms
                        bm.VerifyOffsets(cri, MemoryType.Bit, offsets)
                        let result = bm.readBits offsets
                        ReadResponseOK(cri, MemoryType.Bit, result)
                    | "rb" ->
                        let bm, offsets = fetchForRead mms
                        bm.VerifyOffsets(cri, MemoryType.Byte, offsets)
                        let result = bm.readU8s offsets
                        ReadResponseOK(cri, MemoryType.Byte, result)

                    | "rw" ->
                        let bm, offsets = fetchForRead mms
                        bm.VerifyOffsets(cri, MemoryType.Word, offsets)
                        let result = bm.readU16s offsets
                        ReadResponseOK(cri, MemoryType.Word, result)

                    | "rd" ->
                        let bm, offsets = fetchForRead mms
                        bm.VerifyOffsets(cri, MemoryType.DWord, offsets)
                        let result = bm.readU32s offsets
                        ReadResponseOK(cri, MemoryType.DWord, result)

                    | "rl" ->
                        let bm, offsets = fetchForRead mms
                        bm.VerifyOffsets(cri, MemoryType.LWord, offsets)
                        let result = bm.readU64s offsets
                        ReadResponseOK(cri, MemoryType.LWord, result)
                    | "rs" ->
                        let name = mms[TagKindName].ConvertToString().ToLower()

                        let fs =
                            seq {
                                for v in ioSpec.Vendors do
                                    for fs in v.Files do
                                        if (fs.GetPath().ToLower() = name) then
                                            yield fs
                            } |> Seq.exactlyOne

                        let jsonKeys = mms[MultiMessageFromClient.Offsets].ConvertToString()
                        let keys = JsonConvert.DeserializeObject<string[]>(jsonKeys)
                        let keys, values = fs.ReadStrings keys
                        ReadStringsResponseOK(cri, keys, values)
                    | _ -> StringResponseNG(cri, $"ERROR: {command}")
                | 3 ->
                    match command with
                    | "wx" ->
                        let bm, offsets, byteValues = fetchForWrite mms
                        let values = byteValues |> map (fun b -> b <> 0uy)
                        bm.Verify(cri, MemoryType.Bit, offsets, values.Length)

                        for i in [ 0 .. offsets.Length - 1 ] do
                            bm.writeBit (cri, offsets[i], values[i])

                        bm.Flush()
                        WriteResponseOK(cri, IOChangeInfo(cri, bm.FileSpec, MemoryType.Bit, offsets, values))
                    | "wb" ->
                        let bm, offsets, byteValues = fetchForWrite mms
                        bm.Verify(cri, MemoryType.Byte, offsets, byteValues.Length)
                        Array.zip offsets byteValues |> bm.writeU8s cri
                        WriteResponseOK(cri, IOChangeInfo(cri, bm.FileSpec, MemoryType.Byte, offsets, byteValues))

                    | "ww" ->
                        let bm, offsets, byteValues = fetchForWrite mms
                        let values = ByteConverter.BytesToTypeArray<uint16>(byteValues)
                        bm.Verify(cri, MemoryType.Word, offsets, values.Length)

                        Array.zip offsets values |> bm.writeU16s cri
                        WriteResponseOK(cri, IOChangeInfo(cri, bm.FileSpec, MemoryType.Word, offsets, values))

                    | "wd" ->
                        let bm, offsets, byteValues = fetchForWrite mms
                        let values = ByteConverter.BytesToTypeArray<uint32>(byteValues)
                        bm.Verify(cri, MemoryType.DWord, offsets, values.Length)
                        Array.zip offsets values |> bm.writeU32s cri
                        WriteResponseOK(cri, IOChangeInfo(cri, bm.FileSpec, MemoryType.DWord, offsets, values))

                    | "wl" ->
                        let bm, offsets, byteValues = fetchForWrite mms
                        let values = ByteConverter.BytesToTypeArray<uint64>(byteValues)
                        bm.Verify(cri, MemoryType.LWord, offsets, values.Length)

                        Array.zip offsets values |> bm.writeU64s cri
                        WriteResponseOK(cri, IOChangeInfo(cri, bm.FileSpec, MemoryType.LWord, offsets, values))
                    | _ -> StringResponseNG(cri, $"ERROR: {command}")

                | _ -> StringResponseNG(cri, $"ERROR: {command}")
            with ex ->
                StringResponseNG(cri, ex.Message)

    member x.Run() =
        // start a separate thread to run the server
        let f () =
            logInfo $"Starting IO server on port {port}..."
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
                        let cri: ClientRequestInfo = r.ClientRequestInfo
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
                        | :? StringResponse as r -> more.SendFrame(r.Message)
                        | :? ReadResponseOK as r ->
                            let values, dataType = r.Values, r.MemoryType

                            if dataType = MemoryType.Undefined then
                                more.SendFrame(r.Values :?> string)
                            else
                                verify (values.GetType().IsArray)

                                let moreBytes =
                                    match dataType with
                                    | MemoryType.Bit   -> values :?> bool[] |> map (fun b -> if b then 1uy else 0uy)
                                    | MemoryType.Byte  -> values :?> byte[]
                                    | MemoryType.Word  -> values :?> uint16[] |> ByteConverter.ToBytes<uint16>
                                    | MemoryType.DWord -> values :?> uint32[] |> ByteConverter.ToBytes<uint32>
                                    | MemoryType.LWord -> values :?> uint64[] |> ByteConverter.ToBytes<uint64>
                                    | _ -> failwithlogf "ERROR"

                                more.SendFrame(moreBytes)

                        | :? WriteResponseOK as r ->
                            more.SendFrame("OK")
                            r.MemoryChangeInfo |> notifyIoChange
                        | :? WriteHeterogeniousResponseOK as r ->
                            more.SendFrame("OK")
                            r.SpotChanges |> iter notifyIoChange
                        | :? ReadStringsResponseOK as r ->
                            let keys, values = r.Keys, r.Values
                            let jsonKeys = JsonConvert.SerializeObject(keys)
                            let jsonValues = JsonConvert.SerializeObject(values)
                            more
                                .SendMoreFrame(jsonValues)
                                .SendFrame(jsonKeys)
                        | _ -> failwithlog "Not Yet!!"

                    | _ -> failwithlog "Not Yet!!"
                with
                | :? ExcetionWithClient as ex ->
                    logError $"Error occured while handling request: {ex.Message}"

                    server
                        .SendMoreFrame(ex.ClientId)
                        .SendMoreFrame(ex.RequestId |> ByteConverter.ToBytes)
                        .SendMoreFrame("ERR")
                        .SendFrame(ex.Message)
                | ex -> logError $"Error occured while handling request: {ex.Message}"

            logInfo ("Cancellation request detected!")
            (x :> IDisposable).Dispose()
            terminated <- true

        Task.Factory.StartNew(f, TaskCreationOptions.LongRunning)

    interface IDisposable with
        member x.Dispose() =
            logDebug "Disposing hub IO server..."
            lock streamManagers (
                fun () ->
                    streamManagers.Values |> iter (fun stream -> dispose stream.FileStream)
                    streamManagers.Clear()
            )
