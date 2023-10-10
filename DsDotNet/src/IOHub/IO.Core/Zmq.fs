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


module Zmq =
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
            

        member private x.handleRequest (request: string) =
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
                args |> map (fun a -> $"{a}={readAddress(a)}")
                |> joinWith " "

            | "write" ->
                args |> iter (fun a -> writeAddressWithValue(a))
                "OK"
            | _ ->
                "Unknown request"

        member x.Run() =
            // start a separate thread to run the server
            let th =
                Thread(ThreadStart(fun () ->
                    use respSocket = new ResponseSocket()
                    respSocket.Bind($"tcp://*:{port}")
                
                    while not cancellationToken.IsCancellationRequested do
                        let message = respSocket.ReceiveFrameString()
                        let response = x.handleRequest message
                        respSocket.SendFrame(response)
                    ))
            th.Start()
            th

        interface IDisposable with
            member x.Dispose() =
                streams.Values |> iter (fun stream -> stream.FileStream.Dispose())

    /// serverAddress: "tcp://localhost:5555" or "tcp://*:5555"
    type Client(serverAddress:string) =
        let reqSocket = new RequestSocket()
        do
            reqSocket.Connect(serverAddress)
                
        interface IDisposable with
            member x.Dispose() =
                reqSocket.Close()

        member x.SendRequest(request:string) =
            reqSocket.SendFrame(request)
            reqSocket.ReceiveFrameString()



module Main =
    [<EntryPoint>]
    let main _ = 
        let ioSpec:IOSpec =
            "appsettings.json"
            |> File.ReadAllText
            |> JsonConvert.DeserializeObject<IOSpec>

        let port = ioSpec.ServicePort
        let cts = new CancellationTokenSource()
        let server = new Zmq.Server(ioSpec, cts.Token)
        let serverThread = server.Run()

        let client = new Zmq.Client($"tcp://localhost:{port}")
        let rr0 = client.SendRequest("read Mw100 Mx30 Md1234")
        let result = client.SendRequest("read Mw100 Mx30")
        let result2 = client.SendRequest("read Mw100 Mb70 Mx30 Md50 Ml50")
        //let result3 = client.SendRequest("read [Mw100..Mw30]")
        let wr = client.SendRequest("write Mw100=1 Mx30=false Md1234=1234")
        let rr = client.SendRequest("read Mw100 Mx30 Md1234")
        let xxx = result
        serverThread.Join()
     
        
        0