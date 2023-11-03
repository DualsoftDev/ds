namespace Client

open IO.Core
open System
open System.Threading
open Dual.Common.Core.FS
open ZmqTestModule

module ZmqTestClient =
    [<EntryPoint>]
    let main _ = 
        let cts = new CancellationTokenSource()
        let port = 5555
        let client = new Client($"tcp://localhost:{port}")

        registerCancelKey cts client
        clientKeyboardLoop client cts.Token

        //let zmqInfo = Zmq.InitializeServer "zmqsettings.json"

        //let handleCancelKey (args: ConsoleCancelEventArgs) =
        //    logDebug("Ctrl+C pressed!")
        //    cts.Cancel()
        //    args.Cancel <- true // 프로그램을 종료하지 않도록 설정 (선택 사항)
        //Console.CancelKeyPress.Add(handleCancelKey)



        //// ---- third party ----
        //// wb
        //let wr2 = client.WriteBytes("p/o", [|0; 1; 2; 3|], [|99uy; 98uy; 97uy; 96uy|])
        //match client.ReadBytes("p/o", [|0; 1; 2; 3|]) with
        //| Ok bytes ->
        //    ()
        //| _ ->
        //    failwith "ERROR"

     
        //let mutable key = ""
        //while ( key <> null && not cts.IsCancellationRequested ) do
        //    key <- Console.ReadLine()
        //    if key <> null then
        //        logDebug($"Got request [{key}]...")
        //        match key with
        //        | "q" | "Q" ->
        //            logDebug("Got quit request...")
        //            key <- null
        //        | "" -> ()
        //        | _ ->
        //            match client.SendRequest(key) with
        //            | Ok value ->
        //                Console.WriteLine($"OK: {value}")
        //            | Error err ->
        //                Console.WriteLine($"ERR: {err}")

        //        ()

        //logDebug("Exiting...")
        //Thread.Sleep(1000)
        0
