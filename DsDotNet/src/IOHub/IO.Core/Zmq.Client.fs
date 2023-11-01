(* IO.Core using Zero MQ *)

namespace IO.Core
open System
open NetMQ
open NetMQ.Sockets
open Dual.Common.Core.FS

module ZmqClientModule =
    /// command line request: "read ..", "write ..."
    type CLIRequestResult = Result<string, ErrorMessage>
    /// serverAddress: "tcp://localhost:5555" or "tcp://*:5555"
    type Client(serverAddress:string) =
        let client = new DealerSocket()
        do
            client.Options.Identity <- Guid.NewGuid().ToByteArray() // 각 클라이언트에 대한 고유 식별자 설정
            client.Connect(serverAddress)
            client.SendFrame("REGISTER")
                
        let verifyReceiveOK(client:DealerSocket) : CLIRequestResult =
            let result = client.ReceiveFrameString()
            let detail = client.ReceiveFrameString()
            match result with
            | "OK" -> Ok detail
            | "ERR" -> Error detail
            | _ -> Error $"Error: {result}"

        let buildCommandAndName(client:DealerSocket, command:string, name:string) : IOutgoingSocket =
            client
                .SendMoreFrame(command)
                .SendMoreFrame(name)

        let buildPartial(client:DealerSocket, command:string, name:string, offsets:int[]) : IOutgoingSocket =
            buildCommandAndName(client, command, name)
                .SendMoreFrame(ByteConverter.ToBytes<int>(offsets))

        let sendReadRequest(client:DealerSocket, command:string, name:string, offsets:int[]) : unit =
            buildCommandAndName(client, command, name)
                .SendFrame(ByteConverter.ToBytes<int>(offsets))


        interface IDisposable with
            member x.Dispose() =
                client.Close()


        member x.SendRequest(request:string) : CLIRequestResult =
            client.SendFrame(request)
            let result = client.ReceiveFrameString()
            let detail = client.ReceiveFrameString()
            match result with
            | "OK" -> Ok detail
            | "ERR" -> Error detail
            | _ ->
                logError($"Error: {result}")
                Error result

        member x.Read(tag:string) : obj * ErrorMessage =
            client.SendFrame($"r {tag}")
            let result = client.ReceiveFrameString()
            match result with
            | "OK" ->
                let buffer = client.ReceiveFrameBytes()
                buffer, null
            | "ERR" ->
                let errMsg = client.ReceiveFrameString()
                null, errMsg
            | _ ->
                logError($"Error: {result}")
                null, result

        member x.ReadBits(name:string, offsets:int[]) : TypedIOResult<bool[]> =
            sendReadRequest(client, "rx", name, offsets)

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
            sendReadRequest(client, command, name, offsets)

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
            let byteValues = values |> map (fun v -> if v then 1uy else 0uy)
            buildPartial(client, "wx", name, offsets)
                .SendFrame(byteValues)

            verifyReceiveOK client

        member x.WriteBytes(name:string, offsets:int[], values:byte[]) =
            buildPartial(client, "wb", name, offsets)
                .SendFrame(values)

            verifyReceiveOK client


        member x.WriteUInt16s(name:string, offsets:int[], values:uint16[]) =
            buildPartial(client, "ww", name, offsets)
                .SendFrame(ByteConverter.ToBytes<uint16>(values))

            verifyReceiveOK client

        member x.WriteUInt32s(name:string, offsets:int[], values:uint32[]) =
            buildPartial(client, "wd", name, offsets)
                .SendFrame(ByteConverter.ToBytes<uint32>(values))

            verifyReceiveOK client

        member x.WriteUInt64s(name:string, offsets:int[], values:uint64[]) =
            buildPartial(client, "wl", name, offsets)
                .SendFrame(ByteConverter.ToBytes<uint64>(values))

            verifyReceiveOK client

        member x.ClearAll(name:string) =
            client
                .SendMoreFrame("cl")
                .SendFrame(name)

            verifyReceiveOK client
