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
        let reqSocket = new RequestSocket()
        do
            reqSocket.Connect(serverAddress)
                
        let verifyReceiveOK(reqSocket:RequestSocket) : ErrorMessage =
            let result = reqSocket.ReceiveFrameString()
            let detail = reqSocket.ReceiveFrameString()
            match result with
            | "OK" -> null
            | "ERR" -> null
            | _ -> $"Error: {result}"

        let buildCommandAndName(reqSocket:RequestSocket, command:string, name:string) : IOutgoingSocket =
            reqSocket
                .SendMoreFrame(command)
                .SendMoreFrame(name)

        let buildPartial(reqSocket:RequestSocket, command:string, name:string, offsets:int[]) : IOutgoingSocket =
            buildCommandAndName(reqSocket, command, name)
                .SendMoreFrame(ByteConverter.ToBytes<int>(offsets))

        let sendReadRequest(reqSocket:RequestSocket, command:string, name:string, offsets:int[]) : unit =
            buildCommandAndName(reqSocket, command, name)
                .SendFrame(ByteConverter.ToBytes<int>(offsets))


        interface IDisposable with
            member x.Dispose() =
                reqSocket.Close()


        member x.SendRequest(request:string) : CLIRequestResult =
            reqSocket.SendFrame(request)
            let result = reqSocket.ReceiveFrameString()
            let detail = reqSocket.ReceiveFrameString()
            match result with
            | "OK" -> Ok detail
            | "ERR" -> Error detail
            | _ ->
                logError($"Error: {result}")
                Error result

        member x.Read(tag:string) : obj * ErrorMessage =
            reqSocket.SendFrame($"r {tag}")
            let result = reqSocket.ReceiveFrameString()
            match result with
            | "OK" ->
                let buffer = reqSocket.ReceiveFrameBytes()
                buffer, null
            | "ERR" ->
                let errMsg = reqSocket.ReceiveFrameString()
                null, errMsg
            | _ ->
                logError($"Error: {result}")
                null, result
            
        member x.ReadBytes(name:string, offsets:int[]) : TypedIOResult<byte[]> =
            sendReadRequest(reqSocket, "rb", name, offsets)
            let result = reqSocket.ReceiveFrameString()
            match result with
            | "OK" ->
                let buffer = reqSocket.ReceiveFrameBytes()
                Ok buffer
            | "ERR" ->
                let errMsg = reqSocket.ReceiveFrameString()
                Error errMsg
            | _ ->
                logError($"UNKNOWN Error: {result}")
                Error result


        // command: "rw", "rd", "rl"
        member private x.ReadTypes<'T>(command:string, name:string, offsets:int[]) : 'T[] * ErrorMessage =
            sendReadRequest(reqSocket, command, name, offsets)

            // 서버로부터 응답 수신
            let result = reqSocket.ReceiveFrameString()
            match result with
            | "OK" ->
                let buffer = reqSocket.ReceiveFrameBytes()
                let arr = ByteConverter.BytesToTypeArray<'T>(buffer) // 바이트 배열을 uint16 배열로 변환
                arr, null
            | "ERR" ->
                let errMsg = reqSocket.ReceiveFrameString()
                logError($"Error: {errMsg}")
                null, errMsg
            | _ ->
                logError($"UNKNOWN Error: {result}")
                null, result

        member x.ReadUInt16s(name:string, offsets:int[]) : uint16[] * ErrorMessage =
            x.ReadTypes<uint16>("rw", name, offsets)
        member x.ReadUInt32s(name:string, offsets:int[]) : uint32[] * ErrorMessage=
            x.ReadTypes<uint32>("rd", name, offsets)
        member x.ReadUInt64s(name:string, offsets:int[]) : uint64[] * ErrorMessage=
            x.ReadTypes<uint64>("rl", name, offsets)

        //member x.ReadUInt16s(name:string, offsets:int[]) : uint16[] =
        //    sendReadRequest(reqSocket, "rw", name, offsets)

        //    // 서버로부터 응답 수신
        //    let result = reqSocket.ReceiveFrameString()
        //    match result with
        //    | "OK" ->
        //        let buffer = reqSocket.ReceiveFrameBytes()
        //        ByteConverter.BytesToTypeArray<uint16>(buffer) // 바이트 배열을 uint16 배열로 변환
        //    | _ ->
        //        failwithf($"Error: {result}")


        member x.WriteBits(name:string, offsets:int[], values:bool[]) =
            let byteValues = values |> map (fun v -> if v then 1uy else 0uy)
            buildPartial(reqSocket, "wx", name, offsets)
                .SendFrame(byteValues)

            verifyReceiveOK reqSocket

        member x.WriteBytes(name:string, offsets:int[], values:byte[]) =
            buildPartial(reqSocket, "wb", name, offsets)
                .SendFrame(values)

            verifyReceiveOK reqSocket


        member x.WriteUInt16s(name:string, offsets:int[], values:uint16[]) =
            buildPartial(reqSocket, "ww", name, offsets)
                .SendFrame(ByteConverter.ToBytes<uint16>(values))

            verifyReceiveOK reqSocket

        member x.WriteUInt32s(name:string, offsets:int[], values:uint32[]) =
            buildPartial(reqSocket, "wd", name, offsets)
                .SendFrame(ByteConverter.ToBytes<uint32>(values))

            verifyReceiveOK reqSocket

        member x.WriteUInt64s(name:string, offsets:int[], values:uint64[]) =
            buildPartial(reqSocket, "wl", name, offsets)
                .SendFrame(ByteConverter.ToBytes<uint64>(values))

            verifyReceiveOK reqSocket

        member x.ClearAll(name:string) =
            reqSocket
                .SendMoreFrame("cl")
                .SendFrame(name)

            verifyReceiveOK reqSocket
