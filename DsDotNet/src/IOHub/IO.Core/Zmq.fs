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

[<AutoOpen>]
module FileLayer =
    type ReadResult(error:string, result:obj) =
        member val Error = error with get, set
        member val Result = result  with get, set

    type AddressSpec(name:string, typ:string, offset:int) =
        member val Name = name with get, set
        member val Offset = offset with get, set
        member val Type = typ with get, set

    type ByteRange(s, e) = 
        member val Start = s with get, set
        member val End = e with get, set
    type IOFileSpec(name:string, length:int, validRanges:ByteRange[]) =
        member val Name = name  with get, set
        member val Length = length with get, set
        member val ValidRanges:ByteRange[] = validRanges with get, set
    type IOSpec(servicePort:int, files:IOFileSpec[]) =
        member val Location = "." with get, set
        member val ServicePort = servicePort with get, set
        member val Files = files with get, set

    type MemoryBuffer(stream:FileStream) =
        let locker = obj()  // 객체를 lock용으로 사용
        //member x.Type = typ
        member x.FileStream = stream
        member x.readBits (offset: int, count: int) : bool[] =
            let startByte = offset / 8
            let endByte = startByte + count / 8
            let byteCount = endByte - startByte + 1
            let buffer:byte[] = x.readU8s(startByte, byteCount)
            // buffer 내용을 참조해서 bool 배열로 변환

            let bits =
                buffer
                   |> Array.map (fun b -> Convert.ToString(b, 2).PadLeft(8, '0'))
                   |> Array.collect (fun s -> s.ToCharArray())
                   |> Array.skip  (offset % 8)
                   |> Array.map (fun c -> c = '1')
                   |> Array.take count
                   |> Array.ofSeq
            bits
        member x.readBit(offset:int) = x.readBits(offset, 1)[0]
        member x.readU8 (offset:int) = x.readU8s(offset, 1)[0]
        member x.readU16(offset:int) = x.readU16s(offset, 1)[0]
        member x.readU32(offset:int) = x.readU32s(offset, 1)[0]
        member x.readU64(offset:int) = x.readU64s(offset, 1)[0]

        member x.writeBit(offset:int, value:bool) =
            // 바이트 위치 및 해당 바이트 내의 비트 위치 계산
            let byteIndex = offset / 8
            let bitIndex = offset % 8

            // 해당 위치의 바이트를 읽기
            let currentByte = x.readU8(byteIndex)

            // 비트를 설정하거나 클리어
            let updatedByte =
                if value then
                    currentByte ||| (1uy <<< bitIndex)   // OR 연산을 사용하여 비트 설정
                else
                    currentByte &&& (~~~(1uy <<< bitIndex))  // AND 연산과 NOT 연산을 사용하여 비트 클리어

            // 수정된 바이트를 해당 위치에 쓰기
            x.writeU8(byteIndex, updatedByte)

        member x.writeU8 (offset:int, value:byte) =
            lock locker (fun () ->
                stream.Seek(int64 offset, SeekOrigin.Begin) |> ignore
                stream.WriteByte(value)
            )

        member x.writeU16(offset:int, value:uint16) =
            lock locker (fun () ->
                let buffer = System.BitConverter.GetBytes(value)
                stream.Seek(int64 offset, SeekOrigin.Begin) |> ignore
                stream.Write(buffer, 0, buffer.Length)
            )

        member x.writeU32(offset:int, value:uint32) =
            lock locker (fun () ->
                let buffer = System.BitConverter.GetBytes(value)
                stream.Seek(int64 offset, SeekOrigin.Begin) |> ignore
                stream.Write(buffer, 0, buffer.Length)
            )

        member x.writeU64(offset:int, value:uint64) =
            lock locker (fun () ->
                let buffer = System.BitConverter.GetBytes(value)
                stream.Seek(int64 offset, SeekOrigin.Begin) |> ignore
                stream.Write(buffer, 0, buffer.Length)
            )


        member x.readU8s (offset: int, count: int) : byte[] =
            lock locker (fun () ->
                let buffer = Array.zeroCreate<byte> count
                stream.Seek(int64 offset, SeekOrigin.Begin) |> ignore
                stream.Read(buffer, 0, count) |> ignore
                buffer
            );
        member x.readU16s (offset: int, count: int) : uint16[] =
            lock locker (fun () ->
                let buffer = Array.zeroCreate<byte> (count * 2)
                stream.Seek(int64 offset, SeekOrigin.Begin) |> ignore
                stream.Read(buffer, 0, count * 2) |> ignore
                Array.init count (fun i -> System.BitConverter.ToUInt16(buffer, i * 2))
            )
        member x.readU32s (offset: int, count: int) : uint32[] =
            lock locker (fun () ->
                let buffer = Array.zeroCreate<byte> (count * 4)
                stream.Seek(int64 offset, SeekOrigin.Begin) |> ignore
                stream.Read(buffer, 0, count * 4) |> ignore
                Array.init count (fun i -> System.BitConverter.ToUInt32(buffer, i * 4))
            )
        member x.readU64s (offset: int, count: int) : uint64[] =
            lock locker (fun () ->
                let buffer = Array.zeroCreate<byte> (count * 8)
                stream.Seek(int64 offset, SeekOrigin.Begin) |> ignore
                stream.Read(buffer, 0, count * 8) |> ignore
                Array.init count (fun i -> System.BitConverter.ToUInt64(buffer, i * 8))
            )


        member x.writeByte (offset: int, value: byte) =
            lock locker (fun () ->
                stream.Seek(int64 offset, SeekOrigin.Begin) |> ignore
                stream.Write([| value |], 0, 1)
                stream.Flush()  // Ensure that the changes are written immediately to the file
            )


[<AutoOpen>]
module Extension =
    type IOFileSpec with
        member x.InitiaizeFile(dir:string) : FileStream =
            let path = Path.Combine(dir, x.Name)
            let mutable fs:FileStream = null
            if (File.Exists path) then
                // ensure that the file has the correct length
                fs <- new FileStream(path, FileMode.Open, FileAccess.ReadWrite)
                if (fs.Length <> x.Length) then
                    failwithf($"File [{path}] length mismatch : {fs.Length} <> {x.Length}")
            else
                logInfo($"Creating new file : {path}")
                // create zero-filled file with length = x.Length
                fs <- new FileStream(path, FileMode.Create, FileAccess.ReadWrite)
                let buffer = Array.zeroCreate<byte> x.Length
                fs.Write(buffer, 0, x.Length)
            fs

module Zmq =
    type Server(memoryFilesSpec:IOSpec, cancellationToken:CancellationToken) =
        let port = memoryFilesSpec.ServicePort
        let dir = memoryFilesSpec.Location
        let streams = new Dictionary<string, MemoryBuffer>()
        do
            for mfs in memoryFilesSpec.Files do
                let stream = mfs.InitiaizeFile(dir)
                streams.Add(mfs.Name, new MemoryBuffer(stream))

        let (|AddressPattern|_|) (str: string) =
            let memTypes = streams.Keys.JoinWith "|"
            let dataTypes = "x|b|w|d|l"
            let pat = sprintf "([%s])([%s])(\d+)" memTypes dataTypes
            match str with
            | RegexPattern pat [m; d; Int32Pattern offset] -> Some(AddressSpec(m, d, offset))
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
            let args = tokens[1..]
            match command with
            | ("r" | "rx" | "rb" | "rw" | "rd" | "rl") ->
                let objs =
                    [| for a in args do
                        match a with
                        | AddressPattern addressPattern ->
                            let ap = addressPattern
                            let offset = ap.Offset
                            let stream = streams[ap.Name]

                            let result =
                                match ap.Type with
                                | "x" -> stream.readBit(offset) :> obj
                                | "b" -> stream.readU8(offset)
                                | "w" -> stream.readU16(offset)
                                | "d" -> stream.readU32(offset)
                                | "l" -> stream.readU64(offset)
                                | _ -> failwithf($"Unknown data type : {ap.Type}")
                            yield $"{a}={result}"
                        | _ -> failwithf($"Unknown address pattern : {a}")
                    |]
                objs.JoinWith " "

            | "w" ->
                for a in args do
                    match a with
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

                    | _ -> failwithf($"Unknown address pattern : {a}")
                "OK"
            // ... other patterns
            | _ -> "Unknown request"

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
        let server = new Zmq.Server(ioSpec, CancellationToken.None)
        let serverThread = server.Run()

        let client = new Zmq.Client($"tcp://localhost:{port}")
        let rr0 = client.SendRequest("r Mw100 Mx30 Md1234")
        let result = client.SendRequest("r Mw100 Mx30")
        let result2 = client.SendRequest("r Mw100 Mb70 Mx30 Md50 Ml50")
        //let result3 = client.SendRequest("r [Mw100..Mw30]")
        let wr = client.SendRequest("w Mw100=1 Mx30=false Md1234=1234")
        let rr = client.SendRequest("r Mw100 Mx30 Md1234")
        let xxx = result
        serverThread.Join()
     
        
        0