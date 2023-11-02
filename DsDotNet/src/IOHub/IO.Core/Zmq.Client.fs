(* IO.Core using Zero MQ *)

namespace IO.Core
open System
open System.Runtime.CompilerServices
open NetMQ
open NetMQ.Sockets
open Dual.Common.Core.FS
open IO.Core

[<AutoOpen>]
module ZmqClient =
    type DealerSocket with
        member x.SendMoreFrameWithRequestId(id:int) = id |> ByteConverter.ToBytes |> x.SendMoreFrame

    /// command line request: "read ..", "write ..."
    type CLIRequestResult = Result<string, ErrorMessage>

    /// serverAddress: "tcp://localhost:5555" or "tcp://*:5555"
    [<AllowNullLiteral>]
    type Client(serverAddress:string) =
        let client = new DealerSocket()
        let reqIdGenerator = counterGenerator 1
        do
            client.Options.Identity <- Guid.NewGuid().ToByteArray() // 각 클라이언트에 대한 고유 식별자 설정
            client.Connect(serverAddress)
            client
                .SendMoreFrameWithRequestId(reqIdGenerator())
                .SendFrame("REGISTER")
                
        let verifyReceiveOK(client:DealerSocket) : CLIRequestResult =
            let result = client.ReceiveFrameString()
            let detail = client.ReceiveFrameString()
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

        member x.SendRequest(request:string) : CLIRequestResult =
            let reqId = reqIdGenerator()
            client
                .SendMoreFrameWithRequestId(reqId)
                .SendFrame(request)
            let result = client.ReceiveFrameString()
            let detail = client.ReceiveFrameString()
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
            let result = client.ReceiveFrameString()
            match result with
            | "OK" ->
                let buffer = client.ReceiveFrameBytes()
                let arr = buffer |> map ( (=) 1uy)
                Ok arr
            | "ERR" ->
                let errMsg = client.ReceiveFrameString()
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
            let result = client.ReceiveFrameString()
            match result with
            | "OK" ->
                let buffer = client.ReceiveFrameBytes()
                let arr = ByteConverter.BytesToTypeArray<'T>(buffer) // 바이트 배열을 'T 배열로 변환
                Ok arr
            | "ERR" ->
                let errMsg = client.ReceiveFrameString()
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

            verifyReceiveOK client

        member x.WriteBytes(name:string, offsets:int[], values:byte[]) =
            let reqId = reqIdGenerator()
            buildPartial(client, reqId, "wb", name, offsets)
                .SendFrame(values)

            verifyReceiveOK client


        member x.WriteUInt16s(name:string, offsets:int[], values:uint16[]) =
            let reqId = reqIdGenerator()
            buildPartial(client, reqId, "ww", name, offsets)
                .SendFrame(ByteConverter.ToBytes<uint16>(values))

            verifyReceiveOK client

        member x.WriteUInt32s(name:string, offsets:int[], values:uint32[]) =
            let reqId = reqIdGenerator()
            buildPartial(client, reqId, "wd", name, offsets)
                .SendFrame(ByteConverter.ToBytes<uint32>(values))

            verifyReceiveOK client

        member x.WriteUInt64s(name:string, offsets:int[], values:uint64[]) =
            let reqId = reqIdGenerator()
            buildPartial(client, reqId, "wl", name, offsets)
                .SendFrame(ByteConverter.ToBytes<uint64>(values))

            verifyReceiveOK client

        member x.ClearAll(name:string) =
            let reqId = reqIdGenerator()
            client
                .SendMoreFrameWithRequestId(reqId)
                .SendFrame($"cl {name}")
            verifyReceiveOK client


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

