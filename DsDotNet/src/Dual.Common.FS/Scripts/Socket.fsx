// http://www.fssnip.net/1E/title/Async-TCP-Server

#I @"..\..\bin"
#I @"..\..\bin\netstandard2.0"
#r "Dual.Common.FS.dll"


open System
open System.IO
open System.Net
open System.Net.Sockets
open System.Threading
open System.Runtime.CompilerServices
open Dual.Common


// see TcpIp.fs
[<Extension>]
type SocketExt =
    [<Extension>]
    static member AsyncAccept(socket:Socket) =
        Async.FromBeginEnd(socket.BeginAccept, socket.EndAccept)


    [<Extension>]
    static member AsyncReceive(socket:Socket, buffer: byte [], ?offset, ?count) =
        let offset = defaultArg offset 0
        let count = defaultArg count buffer.Length

        let beginReceive (b, o, c, cb, s) =
            socket.BeginReceive(b, o, c, SocketFlags.None, cb, s)

        Async.FromBeginEnd(buffer, offset, count, beginReceive, socket.EndReceive)

    /// 비동기로 주어진 count 만큼 읽어들임
    [<Extension>]
    static member AsyncRead(socket:Socket, count) =
        let buffer = Array.zeroCreate<Byte> (count)

        let rec loop offset remaining =
            async {
                let! read = SocketExt.AsyncReceive(socket, buffer, offset, remaining)
                if remaining - read = 0 then
                    return buffer
                else
                    return! loop (offset + read) (remaining - read)
            }

        loop 0 count

    [<Extension>]
    static member AsyncSend(socket:Socket, buffer: byte [], ?offset, ?count) =
        let offset = defaultArg offset 0
        let count = defaultArg count buffer.Length

        let beginSend (b, o, c, cb, s) =
            socket.BeginSend(b, o, c, SocketFlags.None, cb, s)

        Async.FromBeginEnd(buffer, offset, count, beginSend, socket.EndSend)



type Server() =
    static member Start(hostname: string, ?port) =
        let ipAddress =
            Dns.GetHostEntry(hostname).AddressList.[0]

        Server.Start(ipAddress, ?port = port)

    static member Start(?ipAddress, ?port) =
        let ipAddress = defaultArg ipAddress IPAddress.Any
        let port = defaultArg port 80
        let endpoint = IPEndPoint(ipAddress, port)
        let cts = new CancellationTokenSource()

        let listener =
            new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)

        listener.Bind(endpoint)
        listener.Listen(int SocketOptionName.MaxConnections)
        printfn "Started listening on port %d" port

        let rec loop () =
            async {
                printfn "Waiting for request ..."

                let! socket = listener.AsyncAccept()
                printfn "Received request"

                // 주어진 갯수만큼 client 에서 읽어 들이는 예제
                //let! buffer = socket.AsyncRead(10)
                //printfn "Finished Async Read Operation...."

                let response =
                    [| "HTTP/1.1 200 OK\r\n"B
                       "Content-Type: text/plain\r\n"B
                       "\r\n"B
                       //buffer
                       "Hello World!"B |]
                    |> Array.concat

                try
                    try
                        let! bytesSent = socket.AsyncSend(response)
                        printfn "Sent response"
                    with e -> printfn "An error occurred: %s" e.Message
                finally
                    socket.Shutdown(SocketShutdown.Both)
                    socket.Close()

                return! loop ()
            }

        Async.Start(loop (), cancellationToken = cts.Token)

        { new IDisposable with
            member x.Dispose() =
                cts.Cancel()
                listener.Close() }

// Demo
let disposable = Server.Start(port = 8099)
Thread.Sleep(60 * 1000)
printfn "bye!"
disposable.Dispose()



