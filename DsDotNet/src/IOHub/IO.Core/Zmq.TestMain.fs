namespace IO.Core

open System
open System.Threading
open Dual.Common.Core.FS
open System.Reactive.Disposables

module ZmqTestModule =
    let clientKeyboardLoop (client:Client) (ct:CancellationToken) =
        let mutable key = ""
        while ( key <> null && not ct.IsCancellationRequested ) do
            key <- Console.ReadLine()
            if key <> null then
                match key with
                | "q" | "Q" -> key <- null
                | "tr" ->   // test read
                    match client.ReadBytes("p/o", [|0..3|]) with
                    | Ok bytes ->
                        let str = bytes |> map toString |> joinWith ", "
                        Console.WriteLine($"OK: {str}")
                    | _ ->
                        failwith "ERROR"
                | "tw" ->   // test write
                    match client.WriteBytes("p/o", [|0..3|], [|0uy..3uy|]) with
                    | Ok msg ->
                        Console.WriteLine($"WriteResult: {msg}")
                    | _ ->
                        failwith "ERROR"

                | "" -> ()
                | _ ->
                    match client.SendRequest(key) with
                    | Ok value ->
                        Console.WriteLine($"OK: {value}")
                    | Error err ->
                        Console.WriteLine($"ERR: {err}")

                ()
    let registerCancelKey (cts:CancellationTokenSource) (disposable:IDisposable) =
        let handleCancelKey (args: ConsoleCancelEventArgs) =
            logDebug("Ctrl+C pressed!")
            cts.Cancel()
            dispose disposable
            args.Cancel <- true // 프로그램을 종료하지 않도록 설정 (선택 사항)
        Console.CancelKeyPress.Add(handleCancelKey)

    let runServer() =
        let zmqInfo = Zmq.InitializeServer "zmqsettings.json"
        let server, cts = zmqInfo.Server, zmqInfo.CancellationTokenSource

        use subs =
            server.IOChangedObservable.Subscribe(fun change ->
                for (tag, value) in change.GetTagNameAndValues() do
                    logDebug $"Tag change detected on server side for {tag}: {value}"
                ())

        registerCancelKey cts server
        while(not server.IsTerminated) do
            Thread.Sleep(1000)
        
        logDebug("Exiting...")
        Thread.Sleep(1000)

    let runServerAndClient() =
        let zmqInfo = Zmq.Initialize "zmqsettings.json"
        let server, client, cts = zmqInfo.Server, zmqInfo.Client, zmqInfo.CancellationTokenSource

        use subs =
            server.IOChangedObservable.Subscribe(fun change ->
                for (tag, value) in change.GetTagNameAndValues() do
                    logDebug $"Tag change detected on server side for {tag}: {value}"
                ())

        let disposables = new CompositeDisposable()
        disposables.Add(client)
        disposables.Add(server)
        registerCancelKey cts disposables

        //let rr0 = client.SendRequest("read Mw100 Mx30 Md12")
        //let result = client.SendRequest("read Mw100 Mx30")
        //let result2 = client.SendRequest("read Mw100 Mb70 Mx30 Md50 Ml50")
        ////let result3 = client.SendRequest("read [Mw100..Mw30]")
        //let wr = client.SendRequest("write Mw100=1 Mx30=false Md12=12")
        //let rr = client.SendRequest("read Mw100 Mx30 Md12")
        //let xxx = result

        //// wb
        //let wr2 = client.WriteBytes("M", [|0; 1; 2; 3|], [|0uy; 1uy; 2uy; 3uy|])
        //let bytes:byte[] = client.ReadBytes("M", [|0; 1; 2; 3|])
        //let words:uint16[] = client.ReadUInt16s("M", [|0; 1; 2; 3|])

        //// wb
        //let wr2 = client.WriteBytes("M", [|0; 1; 2; 3|], [|1uy; 0uy; 55uy; 0uy|])
        //let bytes:byte[] = client.ReadBytes("M", [|0; 1; 2; 3|])
        //let words:uint16[] = client.ReadUInt16s("M", [|0; 1; 2; 3|])

        //// wx
        //let wr3 = client.WriteBits("M", [|0; 7|], [|true; true|])
        //let rr3 = client.ReadBytes("M", [|0|])

        // ---- third party ----
        // wb
        let wr2 = client.WriteBytes("p/o", [|0; 1; 2; 3|], [|99uy; 98uy; 97uy; 96uy|])
        match client.ReadBytes("p/o", [|0; 1; 2; 3|]) with
        | Ok bytes ->
            ()
        | _ ->
            failwith "ERROR"

 
        clientKeyboardLoop client cts.Token

        while(not server.IsTerminated) do
            logDebug("Waiting server terminated...")
            Thread.Sleep(1000)
        
        logDebug("Exiting...")
        Thread.Sleep(1000)
        0

    [<EntryPoint>]
    let main _ =
        runServerAndClient()
