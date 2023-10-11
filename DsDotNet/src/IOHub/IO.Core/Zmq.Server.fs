(* IO.Core using Zero MQ *)

namespace IO.Core
open System
open System.Threading
open NetMQ
open NetMQ.Sockets
open System.IO
open Newtonsoft.Json
open Dual.Common.Core.FS
open System.Collections.Generic

module ZmqServerModule =
    type Server(memoryFilesSpec:IOSpec, cancellationToken:CancellationToken) =
        let port = memoryFilesSpec.ServicePort
        let dir = memoryFilesSpec.Location
        let streams = new Dictionary<string, BufferManager>()
        do
            for mfs in memoryFilesSpec.Files do
                let stream = mfs.InitiaizeFile(dir)
                streams.Add(mfs.Name, new BufferManager(stream))

        let (|AddressPattern|_|) (str: string) =
            let memTypes = streams.Keys.JoinWith "|"
            let dataTypes = "x|b|w|d|l"
            let pattern = sprintf "([%s])([%s])(\d+)" memTypes dataTypes
            match str with
            | RegexPattern pattern [m; d; Int32Pattern offset] -> Some(AddressSpec(m, d, offset))
            | _ -> None

        let (|AddressAssignPattern|_|) (str: string) =
            match str with
            | RegexPattern "(\w+)=(\w+)" [AddressPattern addr; value] ->
                Some(addr, value)
            | _ -> None
            

        member private x.handleRequest (respSocket:ResponseSocket) : IIOResult =
            let request = respSocket.ReceiveFrameString()
            logDebug $"Handling request: {request}"
            let tokens = request.Split(' ', StringSplitOptions.RemoveEmptyEntries) |> Array.ofSeq
            let command = tokens[0]
            let args = tokens[1..] |> map(fun s -> s.ToLower())
            let readAddress(address:string) : obj =
                match address with
                | AddressPattern addr ->
                    let ap = addr
                    let offset = ap.Offset
                    let stream = streams[ap.Name]

                    match ap.Type with
                    | "x" -> stream.readBit(offset) :> obj
                    | "b" -> stream.readU8(offset)
                    | "w" -> stream.readU16(offset)
                    | "d" -> stream.readU32(offset)
                    | "l" -> stream.readU64(offset)
                    | _ -> failwithf($"Unknown data type : {ap.Type}")
                | _ -> failwithf($"Unknown address pattern : {address}")

            let writeAddressWithValue(addressWithAssignValue:string) =
                match addressWithAssignValue with
                | AddressAssignPattern (addressPattern, value) ->
                    let ap = addressPattern
                    let offset = ap.Offset
                    let stream = streams[ap.Name]

                    match ap.Type with
                    | "x" -> stream.writeBit(offset, bool.Parse(value))
                    | "b" -> stream.writeU8(offset,  Byte.Parse(value))
                    | "w" -> stream.writeU16(offset, UInt16.Parse(value))
                    | "d" -> stream.writeU32(offset, UInt32.Parse(value))
                    | "l" -> stream.writeU64(offset, UInt64.Parse(value))
                    | _ -> failwithf($"Unknown data type : {ap.Type}")

                | _ -> failwithf($"Unknown address with assignment pattern : {addressWithAssignValue}")

            match command with
            | "read" ->
                let result =
                    args |> map (fun a -> $"{a}={readAddress(a)}")
                    |> joinWith " "
                ReadResultString(result)

            | "write" ->
                args |> iter (fun a -> writeAddressWithValue(a))
                WriteResultOK()

            | "rb" ->
                let name = respSocket.ReceiveFrameString().ToLower()
                let indices =
                    let address = respSocket.ReceiveFrameBytes()
                    ByteConverter.BytesToTypeArray<int>(address)
                let result = indices |> map (streams[name].readU8)
                ReadResultArray<byte>(result)

            | "rw" ->
                let name = respSocket.ReceiveFrameString().ToLower()
                let indices =
                    let address = respSocket.ReceiveFrameBytes()
                    ByteConverter.BytesToTypeArray<int>(address)
                let result = indices |> map (streams[name].readU16)
                ReadResultArray<uint16>(result)

            | "wb" ->
                let name = respSocket.ReceiveFrameString().ToLower()
                let indices, values =
                    let address = respSocket.ReceiveFrameBytes()
                    let values = respSocket.ReceiveFrameBytes()
                    ByteConverter.BytesToTypeArray<int>(address), values
                if indices.Length <> values.Length then
                    failwithf($"The number of indices and values should be the same.")

                Array.zip indices values |> iter ( fun (index, value) -> streams[name].writeU8(index, value))
                WriteResultOK()
            | _ ->
                ReadResultError $"Unknown request: {request}"

        member x.Run() =
            // start a separate thread to run the server
            Thread(ThreadStart(fun () ->
                use respSocket = new ResponseSocket()
                respSocket.Bind($"tcp://*:{port}")
                
                while not cancellationToken.IsCancellationRequested do
                    let response = x.handleRequest respSocket
                    match response with
                    | null -> ()
                    | :? ReadResultString as ok ->
                        respSocket.SendFrame(ok.Result)
                    | :? WriteResultOK as ok ->
                        respSocket.SendFrame("OK")
                    | :? ReadResultArray<byte> as ok ->
                        respSocket.SendMoreFrame("OK").SendFrame(ok.Results)
                    | :? ReadResultArray<uint16> as ok ->
                        respSocket.SendMoreFrame("OK").SendFrame(ByteConverter.ToBytes<uint16>(ok.Results))
                    | :? IIOResultNG as ng ->
                        respSocket.SendFrame(ng.Error)
                    | _ ->
                        failwithf($"Unknown response type: {response.GetType()}")
                Console.WriteLine("Cancellation request detected!")

            )) |> tee (fun t -> t.Start())

        interface IDisposable with
            member x.Dispose() =
                streams.Values |> iter (fun stream -> stream.FileStream.Dispose())

