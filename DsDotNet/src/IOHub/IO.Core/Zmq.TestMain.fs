namespace IO.Core
open System
open System.Threading
open Dual.Common.Core.FS

module ZmqTestMain =
    [<EntryPoint>]
    let main _ = 
        let zmqInfo = Zmq.Initialize "zmqsettings.json"
        let server, client, cts = zmqInfo.Server, zmqInfo.Client, zmqInfo.CancellationTokenSource

        let handleCancelKey (args: ConsoleCancelEventArgs) =
            logDebug("Ctrl+C pressed!")
            cts.Cancel()
            args.Cancel <- true // 프로그램을 종료하지 않도록 설정 (선택 사항)
        Console.CancelKeyPress.Add(handleCancelKey)


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

     
        let mutable key = ""
        while ( key <> null && not cts.IsCancellationRequested ) do
            key <- Console.ReadLine()
            if key <> null then
                match key with
                | "q" | "Q" -> key <- null
                | "" -> ()
                | _ ->
                    match client.SendRequest(key) with
                    | Ok value ->
                        Console.WriteLine($"OK: {value}")
                    | Error err ->
                        Console.WriteLine($"ERR: {err}")

                ()

        while(not server.IsTerminated) do
            logDebug("Waiting server terminated...")
            Thread.Sleep(1000)
        
        logDebug("Exiting...")
        Thread.Sleep(1000)
        0
