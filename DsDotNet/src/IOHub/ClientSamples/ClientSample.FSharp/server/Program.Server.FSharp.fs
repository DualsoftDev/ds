namespace Server

open IO.Core
open System
open System.Threading
open Dual.Common.Core.FS

module ZmqTestMain =
    [<EntryPoint>]
    let main _ = 
        let zmqInfo = Zmq.InitializeServer "zmqsettings.json"
        let server, cts = zmqInfo.Server, zmqInfo.CancellationTokenSource

        //use subs =
        //    server.IOChangedObservable.Subscribe(fun change ->
        //        for (tag, value) in change.GetTagNameAndValues() do
        //            logDebug $"Tag change detected on server side for {tag}: {value}"
        //        ())

        let handleCancelKey (args: ConsoleCancelEventArgs) =
            logDebug("Ctrl+C pressed!")
            cts.Cancel()
            args.Cancel <- true // 프로그램을 종료하지 않도록 설정 (선택 사항)
        Console.CancelKeyPress.Add(handleCancelKey)


        while(not server.IsTerminated) do
            //logDebug("Waiting server terminated...")
            Thread.Sleep(1000)
        
        logDebug("Exiting...")
        Thread.Sleep(1000)
        0
