(* IO.Core using Zero MQ *)

namespace IO.Core

open System
open NetMQ
open NetMQ.Sockets
open Dual.Common.Core.FS
open IO.Core
open System.Collections.Concurrent
open System.Threading
open System.Reactive.Subjects
open System.Threading.Tasks
open Newtonsoft.Json

[<AutoOpen>]
module internal ZmqClient =
    /// command line request: "read ..", "write ..."
    type CLIRequestResult = Result<string, ErrorMessage>

    /// Server 로부터 받은 multi-message format
    [<AutoOpen>]
    module MultiMessageFromServer =
        let RequestId = 0 // int.  음수이면 notify, else 자신이 보낸 request id 의 echo
        let OkNg = 1

        let Detail = 2

        // - read request 에 대한 reply 및,
        // - server tag changed notify
        // 에 대한 format
        let Values = 2 // encoded byte array
        let Keys = 3 // string keys
        let Name = 3 // e.g "p/o"
        let ContentBitLength = 4 // e.g 1, 8, 16, 32, 64
        let Offsets = 5 // bitoffset or byteoffset

    type NetMQMessage with

        member x.CheckRequestId(id: int) =
            verify (id = x[MultiMessageFromServer.RequestId].ConvertToInt32())

/// server 로부터 공지 받은 Tag 변경 정보
[<AbstractClass>]
type TagChangedInfo(path: string, contentBitLength: int, values: obj) =
    member x.Path = path
    member x.ContentBitLength = contentBitLength

type IOTagChangedInfo(path: string, contentBitLength: int, offsets: int[], values: obj) =
    inherit TagChangedInfo(path, contentBitLength, values)
    member x.Offsets = offsets
    member x.Values = values

type StringTagChangedInfo(path: string, contentBitLength: int, keys: string[], values: string[]) =
    inherit TagChangedInfo(path, contentBitLength, values)
    member x.Keys = keys
    member x.Values = values

/// FSharp 버젼 Client
/// - serverAddress: "tcp://192.168.0.2:5555" or "tcp://*:5555"
[<AllowNullLiteral>]
type Client(serverAddress: string) =
    let cancellationTokenSource = new CancellationTokenSource()
    let client = new DealerSocket()
    /// 서버와의 endian 이 같은지 여부.  서버와 다른 endian 이면, 서버로부터 받은 데이터를 변환해야 한다.
    let mutable needEndianFix = false

    let reqIdGenerator = counterGenerator 1
    let queue = ConcurrentQueue<NetMQMessage>()

    let deque (reqId: int) =
        let rec helper () =
            match queue.TryDequeue() with
            | true, item -> Some item
            | false, _ ->
                // 큐가 비었음
                Thread.Sleep(100)
                helper ()

        let mq = helper().Value
        let reqIdBack = mq[MultiMessageFromServer.RequestId].GetInt32(needEndianFix)

        if reqId <> reqIdBack then
            failwithf $"Request/Reply id mismatch: {reqId} <> {reqIdBack}"

        mq

    /// server 로부터 공지 받은 변경 사항
    let tagChangedSubject = new Subject<TagChangedInfo>()
    //let clientGuid = Guid.NewGuid().ToByteArray()
    let clientGuid =
        System.Random().Next(0, 65535) |> uint16 |> ByteConverter.ToBytes<uint16>

    let clientId = clientIdentifierToString clientGuid



    let loop () =
        while not cancellationTokenSource.IsCancellationRequested do
            let mutable mq: NetMQMessage = null

            while mq = null && not cancellationTokenSource.IsCancellationRequested do
                if not <| client.TryReceiveMultipartMessage(&mq) then
                    Thread.Sleep(100) // got it

            let reqIdBack = mq[RequestId].GetInt32(needEndianFix)

            if reqIdBack >= 0 then
                verify (reqIdBack < 1000)
                // normal request 에 대한 reply: queue 에 삽입
                queue.Enqueue(mq)
            else
                // 서버로부터 받은 변경 내용을 client app 에 공지
                let command = mq[OkNg].ConvertToString() // e.g "NOTIFY", "PING"
                logDebug $"Got notification {command} from server on client {clientId}..."

                match command with
                | "PING" -> client.SendMoreFrame(ByteConverter.ToBytes(-1, needEndianFix)).SendFrame("PONG")
                | "NOTIFY" ->
                    let contentBitLength = mq[ContentBitLength].GetInt32(needEndianFix) // e.g 1, 8, 16, 32, 64
                    let path = mq[Name].ConvertToString() // e.g "p/o"

                    if contentBitLength = int MemoryType.String then
                        let key = mq[Offsets].ConvertToString()
                        let value = mq[Values].ConvertToString()
                        logDebug $"Got tag changed notification: {path} = {value}"

                        StringTagChangedInfo(path, contentBitLength, [| key |], [| value |])
                        |> tagChangedSubject.OnNext
                    else
                        let offsets = mq[Offsets].GetArray<int>(needEndianFix) // bitoffset or byteoffset

                        let values: obj =
                            match contentBitLength with
                            | 1 -> mq[Values].Buffer |> map (fun b -> b = 0uy) |> box
                            | 8 -> mq[Values].Buffer |> box
                            | 16 -> mq[Values].GetArray<uint16>(needEndianFix) |> box
                            | 32 -> mq[Values].GetArray<uint32>(needEndianFix) |> box
                            | 64 -> mq[Values].GetArray<uint64>(needEndianFix) |> box
                            | _ -> failwithlog "ERROR"

                        IOTagChangedInfo(path, contentBitLength, offsets, values)
                        |> tagChangedSubject.OnNext
                | _ -> failwithlogf $"Unknown command: {command}"


    do
        client.Options.Identity <- clientGuid // 각 클라이언트에 대한 고유 식별자 설정
        logDebug $"Client identity: {clientId}"
        client.Connect(serverAddress)

        Task.Factory.StartNew(loop, TaskCreationOptions.LongRunning) |> ignore

        let reqId = 0 // "REGISTER" 인 경우 항상 reqId 를 0 으로 설정한다.

        client
            .SendMoreFrame("REGISTER")
            .SendFrameWithRequestIdAndEndian(reqId, needEndianFix)

        let mqMessage = deque reqId
        let result = mqMessage[OkNg].ConvertToString()
        let detail = mqMessage[Detail].ConvertToString().ToLower()
        needEndianFix <- detail.Contains("little") <> BitConverter.IsLittleEndian
        logDebug "Got client registered response."

    let verifyReceiveOK (client: DealerSocket) (reqId: int) : CLIRequestResult =
        let mqMessage = deque reqId

        let result = mqMessage[OkNg].ConvertToString()
        let detail = mqMessage[Detail].ConvertToString()

        match result with
        | "OK" -> Ok detail
        | "ERR" -> Error detail
        | _ -> Error $"Error: {result}"

    let buildCommandAndName (client: DealerSocket, reqId: int, command: string, name: MemoryName) : IOutgoingSocket =
        client
            .SendMoreFrame(command)
            .SendMoreFrameWithRequestIdAndEndian(reqId, needEndianFix)
            .SendMoreFrame(name)

    let buildPartial
        (
            client: DealerSocket,
            reqId: int,
            command: string,
            name: MemoryName,
            offsets: int[]
        ) : IOutgoingSocket =
        buildCommandAndName(client, reqId, command, name)
            .SendMoreFrame(ByteConverter.ToBytes<int>(offsets, needEndianFix))

    let sendReadRequest (client: DealerSocket, reqId: int, command: string, name: MemoryName, offsets: int[]) : unit =
        buildCommandAndName(client, reqId, command, name)
            .SendFrame(ByteConverter.ToBytes<int>(offsets, needEndianFix))

    let sendStringReadRequest (client: DealerSocket, reqId: int, name: MemoryName, keys: string[]) : unit =
        let jsonKeys = JsonConvert.SerializeObject(keys)
        buildCommandAndName(client, reqId, "rs", name).SendFrame(jsonKeys)


    interface IDisposable with
        member x.Dispose() =
            client
                .SendMoreFrame("UNREGISTER")
                .SendFrameWithRequestIdAndEndian(reqIdGenerator (), needEndianFix)

            client.Close()


    /// 직접 socket 접근해서 사용하기 위한 용도로 Zmq DealerSocket 반환.  비추
    [<Obsolete("가급적 API 이용")>]
    member x.Socket = client

    member x.TagChangedSubject = tagChangedSubject

    member x.SendRequest(request: string) : CLIRequestResult =
        let reqId = reqIdGenerator ()

        client
            .SendMoreFrame(request)
            .SendFrameWithRequestIdAndEndian(reqId, needEndianFix)

        let mqMessage = deque reqId
        let result = mqMessage[OkNg].ConvertToString()
        let detail = mqMessage[Detail].ConvertToString()

        match result with
        | "OK" -> Ok detail
        | "ERR" -> Error detail
        | _ ->
            logError ($"Error: {result}")
            Error result

    /// 서버에 설정된 모든 IO 정보(IOSpec)를 가져온다.
    member x.GetMeta() : IOSpec =
        match x.SendRequest("META") with
        | Ok jsonMeta -> JsonConvert.DeserializeObject<IOSpec>(jsonMeta)
        | Error errMsg -> failwithf ($"Error: {errMsg}")

    member x.ReadBits(name: MemoryName, offsets: int[]) : TypedIOResult<bool[]> =
        let reqId = reqIdGenerator ()
        sendReadRequest (client, reqId, "rx", name, offsets)

        // 서버로부터 응답 수신
        let mqMessage = deque reqId
        let result = mqMessage[OkNg].ConvertToString()

        match result with
        | "OK" ->
            let buffer = mqMessage[Values].Buffer
            let arr = buffer |> map ((=) 1uy)
            Ok arr
        | "ERR" ->
            let errMsg = mqMessage[Detail].ConvertToString()
            logError ($"Error: {errMsg}")
            Error errMsg
        | _ ->
            logError ($"UNKNOWN Error: {result}")
            Error result

    member x.ReadStrings(name: MemoryName, keys: string[]) : TypedIOResult<StringKeyValue[]> =
        let reqId = reqIdGenerator ()
        sendStringReadRequest (client, reqId, name, keys)

        // 서버로부터 응답 수신
        let mqMessage = deque reqId
        let result = mqMessage[OkNg].ConvertToString()

        match result with
        | "OK" ->
            let jsonValues = mqMessage[Values].ConvertToString()
            let values = JsonConvert.DeserializeObject<string[]>(jsonValues)

            let keys =
                if keys.IsEmpty () then
                    let jsonKeys = mqMessage[Keys].ConvertToString()
                    JsonConvert.DeserializeObject<string[]>(jsonKeys)
                else
                    keys

            Ok(Array.zip keys values)
        | "ERR" ->
            let errMsg = mqMessage[Detail].ConvertToString()
            logError ($"Error: {errMsg}")
            Error errMsg
        | _ ->
            logError ($"UNKNOWN Error: {result}")
            Error result

    member x.ReadAllStrings(name: MemoryName) = x.ReadStrings(name, [||])



    // command: "rw", "rd", "rl"
    member private x.ReadTypes<'T>(command: string, name: MemoryName, offsets: int[]) : TypedIOResult<'T[]> =
        let reqId = reqIdGenerator ()
        sendReadRequest (client, reqId, command, name, offsets)

        // 서버로부터 응답 수신
        let mqMessage = deque reqId
        let result = mqMessage[OkNg].ConvertToString()

        match result with
        | "OK" ->
            let arr = mqMessage[Values].GetArray<'T>(needEndianFix) // 바이트 배열을 'T 배열로 변환
            Ok arr
        | "ERR" ->
            let errMsg = mqMessage[Detail].ConvertToString()
            logError ($"Error: {errMsg}")
            Error errMsg
        | _ ->
            logError ($"UNKNOWN Error: {result}")
            Error result

    member x.ReadBytes(name: MemoryName, offsets: int[]) : TypedIOResult<byte[]> =
        x.ReadTypes<byte>("rb", name, offsets)

    member x.ReadUInt16s(name: MemoryName, offsets: int[]) : TypedIOResult<uint16[]> =
        x.ReadTypes<uint16>("rw", name, offsets)

    member x.ReadUInt32s(name: MemoryName, offsets: int[]) : TypedIOResult<uint32[]> =
        x.ReadTypes<uint32>("rd", name, offsets)

    member x.ReadUInt64s(name: MemoryName, offsets: int[]) : TypedIOResult<uint64[]> =
        x.ReadTypes<uint64>("rl", name, offsets)


    member x.WriteBits(name: MemoryName, offsets: int[], values: bool[]) =
        let reqId = reqIdGenerator ()
        let byteValues = values |> map (fun v -> if v then 1uy else 0uy)
        buildPartial(client, reqId, "wx", name, offsets).SendFrame(byteValues)

        verifyReceiveOK client reqId

    member x.WriteBytes(name: MemoryName, offsets: int[], values: byte[]) =
        let reqId = reqIdGenerator ()
        buildPartial(client, reqId, "wb", name, offsets).SendFrame(values)

        verifyReceiveOK client reqId


    member x.WriteUInt16s(name: MemoryName, offsets: int[], values: uint16[]) =
        let reqId = reqIdGenerator ()

        buildPartial(client, reqId, "ww", name, offsets)
            .SendFrame(ByteConverter.ToBytes<uint16>(values, needEndianFix))

        verifyReceiveOK client reqId

    member x.WriteUInt32s(name: MemoryName, offsets: int[], values: uint32[]) =
        let reqId = reqIdGenerator ()

        buildPartial(client, reqId, "wd", name, offsets)
            .SendFrame(ByteConverter.ToBytes<uint32>(values, needEndianFix))

        verifyReceiveOK client reqId

    member x.WriteUInt64s(name: MemoryName, offsets: int[], values: uint64[]) =
        let reqId = reqIdGenerator ()

        buildPartial(client, reqId, "wl", name, offsets)
            .SendFrame(ByteConverter.ToBytes<uint64>(values, needEndianFix))

        verifyReceiveOK client reqId

    member x.ClearAll(name: MemoryName) =
        let reqId = reqIdGenerator ()

        client
            .SendMoreFrame($"cl {name}")
            .SendFrameWithRequestIdAndEndian(reqId, needEndianFix)

        verifyReceiveOK client reqId
