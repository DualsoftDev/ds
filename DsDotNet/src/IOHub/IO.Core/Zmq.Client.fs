(* IO.Core using Zero MQ *)

namespace IO.Core
open System
open System.Runtime.CompilerServices
open NetMQ
open NetMQ.Sockets
open Dual.Common.Core.FS
open IO.Core
open System.Collections.Concurrent
open System.Threading
open System.Reactive.Subjects
open System.Threading.Tasks

[<AutoOpen>]
module ZmqClient =
    /// command line request: "read ..", "write ..."
    type CLIRequestResult = Result<string, ErrorMessage>

    /// Server 로부터 받은 multi-message format
    [<AutoOpen>]
    module private ServerMultiMessage =
        let RequestId = 0       // int.  음수이면 notify, else 자신이 보낸 request id 의 echo
        let OkNg      = 1

        let Detail    = 2

        // - read request 에 대한 reply 및, 
        // - server tag changed notify 
        // 에 대한 format
        let Values    = 2       // encoded byte array
        let Name      = 3       // e.g "p/o"
        let ContentBitLength = 4// e.g 1, 8, 16, 32, 64
        let Offsets   = 5       // bitoffset or byteoffset

    type NetMQMessage with
        member x.CheckRequestId(id:int) = verify(id = x[ServerMultiMessage.RequestId].ConvertToInt32())

    /// server 로부터 공지 받은 Tag 변경 정보
    type TagChangedInfo(path:string, contentBitLength:int, offsets:int[], values:obj) =
        member x.Path = path
        member x.ContentBitLength = contentBitLength
        member x.Offsets = offsets
        member x.Values = values


    /// serverAddress: "tcp://192.168.0.2:5555" or "tcp://*:5555"
    [<AllowNullLiteral>]
    type Client(serverAddress:string) =
        let cancellationTokenSource = new CancellationTokenSource() 
        let client = new DealerSocket()
        let reqIdGenerator = counterGenerator 1
        let queue = ConcurrentQueue<NetMQMessage>()
        let deque (reqId:int) =
            let rec helper() =
                match queue.TryDequeue() with
                | true, item -> Some item
                | false, _ ->
                    // 큐가 비었음
                    Thread.Sleep(100)
                    helper()
            let mq = helper().Value
            let reqIdBack = mq[ServerMultiMessage.RequestId].Buffer |> BitConverter.ToInt32
            if reqId <> reqIdBack then
                failwithf $"Request/Reply id mismatch: {reqId} <> {reqIdBack}"
            mq

        /// server 로부터 공지 받은 변경 사항
        let tagChangedSubject = new Subject<TagChangedInfo>()
        //let clientGuid = Guid.NewGuid().ToByteArray()
        let clientGuid = System.Random().Next(0, 65535) |> uint16 |> ByteConverter.ToBytes<uint16>
        let clientId = clientIdentifierToString clientGuid


        let loop() =
            while not cancellationTokenSource.IsCancellationRequested do
                let mutable mq:NetMQMessage = null
                while mq = null && not cancellationTokenSource.IsCancellationRequested do
                    if not <| client.TryReceiveMultipartMessage(&mq) then
                        Thread.Sleep(100)  // got it

                let reqIdBack = mq[RequestId].ConvertToInt32()
                if reqIdBack >= 0 then
                    // normal request 에 대한 reply: queue 에 삽입
                    queue.Enqueue(mq)
                else
                    // 서버로부터 받은 변경 내용을 client app 에 공지
                    let command = mq[OkNg].ConvertToString()  // e.g "NOTIFY", "PING"
                    logDebug $"Got notification {command} from server on client {clientId}..."
                    match command with
                    | "PING" ->
                        client
                            .SendMoreFrame(-1 |> ByteConverter.ToBytes)
                            .SendFrame("PONG")
                    | "NOTIFY" ->
                        let contentBitLength = mq[ContentBitLength].Buffer |> BitConverter.ToInt32  // e.g 1, 8, 16, 32, 64
                        let offsets   = mq[Offsets].Buffer |> ByteConverter.BytesToTypeArray<int>       // bitoffset or byteoffset
                        let path      = mq[Name].ConvertToString()  // e.g "p/o"
                        let values:obj =
                            match contentBitLength with
                            |  1 -> mq[Values].Buffer |> box
                            |  8 -> mq[Values].Buffer |> ByteConverter.BytesToTypeArray<byte>  |> box
                            | 16 -> mq[Values].Buffer |> ByteConverter.BytesToTypeArray<uint16>|> box
                            | 32 -> mq[Values].Buffer |> ByteConverter.BytesToTypeArray<uint32>|> box
                            | 64 -> mq[Values].Buffer |> ByteConverter.BytesToTypeArray<uint64>|> box
                            |  _ -> failwith "ERROR"

                        TagChangedInfo(path, contentBitLength, offsets, values)
                        |> tagChangedSubject.OnNext
                    | _ ->
                        failwithlogf $"Unknown command: {command}"


        do
            client.Options.Identity <- clientGuid // 각 클라이언트에 대한 고유 식별자 설정
            logDebug $"Client identity: {clientId}"
            client.Connect(serverAddress)
            client
                .SendMoreFrameWithRequestId(reqIdGenerator())
                .SendFrame("REGISTER")
            Task.Factory.StartNew(loop, TaskCreationOptions.LongRunning) |> ignore
                
        let verifyReceiveOK(client:DealerSocket) (reqId:int) : CLIRequestResult =
            let mqMessage = deque reqId

            let result = mqMessage[OkNg].ConvertToString()
            let detail = mqMessage[Detail].ConvertToString()
            match result with
            | "OK" -> Ok detail
            | "ERR" -> Error detail
            | _ -> Error $"Error: {result}"

        let buildCommandAndName(client:DealerSocket, reqId:int, command:string, name:string) : IOutgoingSocket =
            client
                .SendMoreFrameWithRequestId(reqId)
                .SendMoreFrame(command)
                .SendMoreFrame(name)

        let buildPartial(client:DealerSocket, reqId:int, command:string, name:string, offsets:int[]) : IOutgoingSocket =
            buildCommandAndName(client, reqId, command, name)
                .SendMoreFrame(ByteConverter.ToBytes<int>(offsets))

        let sendReadRequest(client:DealerSocket, reqId:int, command:string, name:string, offsets:int[]) : unit =
            buildCommandAndName(client, reqId, command, name)
                .SendFrame(ByteConverter.ToBytes<int>(offsets))


        interface IDisposable with
            member x.Dispose() =
                client
                    .SendMoreFrameWithRequestId(reqIdGenerator())
                    .SendFrame("UNREGISTER")
                client.Close()


        /// 직접 socket 접근해서 사용하기 위한 용도로 Zmq DealerSocket 반환.  비추
        [<Obsolete("가급적 API 이용")>]
        member x.Socket = client

        member x.TagChangedSubject = tagChangedSubject

        member x.SendRequest(request:string) : CLIRequestResult =
            let reqId = reqIdGenerator()
            client
                .SendMoreFrameWithRequestId(reqId)
                .SendFrame(request)

            let mqMessage = deque reqId
            let result = mqMessage[OkNg].ConvertToString()
            let detail = mqMessage[Detail].ConvertToString()

            match result with
            | "OK" -> Ok detail
            | "ERR" -> Error detail
            | _ ->
                logError($"Error: {result}")
                Error result

        member x.ReadBits(name:string, offsets:int[]) : TypedIOResult<bool[]> =
            let reqId = reqIdGenerator()
            sendReadRequest(client, reqId, "rx", name, offsets)

            // 서버로부터 응답 수신
            let mqMessage = deque reqId
            let result = mqMessage[OkNg].ConvertToString()
            match result with
            | "OK" ->
                let buffer = mqMessage[Values].Buffer
                let arr = buffer |> map ( (=) 1uy)
                Ok arr
            | "ERR" ->
                let errMsg = mqMessage[Detail].ConvertToString()
                logError($"Error: {errMsg}")
                Error errMsg
            | _ ->
                logError($"UNKNOWN Error: {result}")
                Error result


        // command: "rw", "rd", "rl"
        member private x.ReadTypes<'T>(command:string, name:string, offsets:int[]) : TypedIOResult<'T[]> =
            let reqId = reqIdGenerator()
            sendReadRequest(client, reqId, command, name, offsets)

            // 서버로부터 응답 수신
            let mqMessage = deque reqId
            let result = mqMessage[OkNg].ConvertToString()
            match result with
            | "OK" ->
                let buffer = mqMessage[Values].Buffer
                let arr = ByteConverter.BytesToTypeArray<'T>(buffer) // 바이트 배열을 'T 배열로 변환
                Ok arr
            | "ERR" ->
                let errMsg = mqMessage[Detail].ConvertToString()
                logError($"Error: {errMsg}")
                Error errMsg
            | _ ->
                logError($"UNKNOWN Error: {result}")
                Error result

        member x.ReadBytes(name:string, offsets:int[]) : TypedIOResult<byte[]> =
            x.ReadTypes<byte>("rb", name, offsets)
        member x.ReadUInt16s(name:string, offsets:int[]) : TypedIOResult<uint16[]> =
            x.ReadTypes<uint16>("rw", name, offsets)
        member x.ReadUInt32s(name:string, offsets:int[]) : TypedIOResult<uint32[]> =
            x.ReadTypes<uint32>("rd", name, offsets)
        member x.ReadUInt64s(name:string, offsets:int[]) : TypedIOResult<uint64[]> =
            x.ReadTypes<uint64>("rl", name, offsets)


        member x.WriteBits(name:string, offsets:int[], values:bool[]) =
            let reqId = reqIdGenerator()
            let byteValues = values |> map (fun v -> if v then 1uy else 0uy)
            buildPartial(client, reqId, "wx", name, offsets)
                .SendFrame(byteValues)

            verifyReceiveOK client reqId

        member x.WriteBytes(name:string, offsets:int[], values:byte[]) =
            let reqId = reqIdGenerator()
            buildPartial(client, reqId, "wb", name, offsets)
                .SendFrame(values)

            verifyReceiveOK client reqId


        member x.WriteUInt16s(name:string, offsets:int[], values:uint16[]) =
            let reqId = reqIdGenerator()
            buildPartial(client, reqId, "ww", name, offsets)
                .SendFrame(ByteConverter.ToBytes<uint16>(values))

            verifyReceiveOK client reqId

        member x.WriteUInt32s(name:string, offsets:int[], values:uint32[]) =
            let reqId = reqIdGenerator()
            buildPartial(client, reqId, "wd", name, offsets)
                .SendFrame(ByteConverter.ToBytes<uint32>(values))

            verifyReceiveOK client reqId

        member x.WriteUInt64s(name:string, offsets:int[], values:uint64[]) =
            let reqId = reqIdGenerator()
            buildPartial(client, reqId, "wl", name, offsets)
                .SendFrame(ByteConverter.ToBytes<uint64>(values))

            verifyReceiveOK client reqId

        member x.ClearAll(name:string) =
            let reqId = reqIdGenerator()
            client
                .SendMoreFrameWithRequestId(reqId)
                .SendFrame($"cl {name}")
            verifyReceiveOK client reqId


    type CsResult<'T> = Dual.Common.Core.Result<'T, ErrorMessage>
    let toResultCs (fsResult:TypedIOResult<'T>) =
        match fsResult with
        | Ok r -> Dual.Common.Core.Result.Ok r
        | Error e -> Dual.Common.Core.Result.Err e

// { C# 에서 소화하기 쉬운 형태로 변환.  C# Result<'T, string> 형태로..
[<Extension>]
type ZmqClientExt =
    [<Extension>] static member CsSendRequest(client:Client, request:string) : CsResult<string>               = toResultCs <| client.SendRequest(request)
    [<Extension>] static member CsReadBits   (client:Client, name:string, offsets:int[]) : CsResult<bool[]>   = toResultCs <| client.ReadBits(name, offsets)
    [<Extension>] static member CsReadBytes  (client:Client, name:string, offsets:int[]) : CsResult<byte[]>   = toResultCs <| client.ReadBytes(name, offsets)
    [<Extension>] static member CsReadUInt16s(client:Client, name:string, offsets:int[]) : CsResult<uint16[]> = toResultCs <| client.ReadUInt16s(name, offsets)
    [<Extension>] static member CsReadUInt32s(client:Client, name:string, offsets:int[]) : CsResult<uint32[]> = toResultCs <| client.ReadUInt32s(name, offsets)
    [<Extension>] static member CsReadUInt64s(client:Client, name:string, offsets:int[]) : CsResult<uint64[]> = toResultCs <| client.ReadUInt64s(name, offsets)
// }

