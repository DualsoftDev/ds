namespace IO.Core
open System
open System.Threading
open System.IO
open Newtonsoft.Json
open ZmqServerModule
open ZmqClientModule
open Dual.Common.Core.FS

module ZmqTestMain =
    [<EntryPoint>]
    let main _ = 
        let ioSpec:IOSpec =
            "appsettings.json"
            |> File.ReadAllText
            |> JsonConvert.DeserializeObject<IOSpec>

        let port = ioSpec.ServicePort
        let cts = new CancellationTokenSource()

        let handleCancelKey (args: ConsoleCancelEventArgs) =
            logDebug("Ctrl+C pressed!")
            cts.Cancel()
            args.Cancel <- true // 프로그램을 종료하지 않도록 설정 (선택 사항)
        Console.CancelKeyPress.Add(handleCancelKey)

        let server = new Server(ioSpec, cts.Token)
        let serverThread = server.Run()

        let client = new Client($"tcp://localhost:{port}")

        let rr0 = client.SendRequest("read Mw100 Mx30 Md12")
        let result = client.SendRequest("read Mw100 Mx30")
        let result2 = client.SendRequest("read Mw100 Mb70 Mx30 Md50 Ml50")
        //let result3 = client.SendRequest("read [Mw100..Mw30]")
        let wr = client.SendRequest("write Mw100=1 Mx30=false Md12=12")
        let rr = client.SendRequest("read Mw100 Mx30 Md12")
        let xxx = result

        let wr2 = client.WriteBytes("M", [|0; 1; 2; 3|], [|0uy; 1uy; 2uy; 3uy|])
        let bytes:byte[] = client.ReadBytes("M", [|0; 1; 2; 3|])
        let words:uint16[] = client.ReadUInt16s("M", [|0; 1; 2; 3|])


        let wr2 = client.WriteBytes("M", [|0; 1; 2; 3|], [|1uy; 0uy; 55uy; 0uy|])
        let bytes:byte[] = client.ReadBytes("M", [|0; 1; 2; 3|])
        let words:uint16[] = client.ReadUInt16s("M", [|0; 1; 2; 3|])
        //serverThread.Join()
     
        let mutable key = ""
        while ( key <> null && not cts.IsCancellationRequested ) do
            key <- Console.ReadLine()
            if key <> null then
                match key with
                | "q" | "Q" -> key <- null
                | "" -> ()
                | _ ->
                    let result = client.SendRequest(key)
                    Console.WriteLine(result)
                ()

        while(not server.IsTerminated) do
            logDebug("Waiting server terminated...")
            Thread.Sleep(1000)
        
        logDebug("Exiting...")
        Thread.Sleep(1000)
        0
