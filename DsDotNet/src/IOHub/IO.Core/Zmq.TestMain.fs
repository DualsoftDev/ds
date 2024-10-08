namespace IO.Core

open System
open System.Threading
open Dual.Common.Core.FS
open System.Reactive.Disposables
open System

module ZmqTestModule =
    let helpText =
        """
tr[x,b,w,dw,lw] : test read first 4 [bits, bytes, words, dwords, lwords]
tw[x,b,w,dw,lw] : test write first 4 [bits, bytes, words, dwords, lwords]
trs : test read all p/o strings
ws p/s key=value : write string with key in p/s file
rs p/s key: read string with key in p/s file
rs p/s: read all strings in p/s file
"""

    let clientKeyboardLoop (client: Client) (ct: CancellationToken) =
        let mutable key = ""
        let testReadOffsets = [| 0..3 |]
        let testWriteValues = testReadOffsets |> map ((*) 3)

        while (key <> null && not ct.IsCancellationRequested) do
            key <- Console.ReadLine()

            if key <> null then
                match key with
                | "h" -> Console.WriteLine helpText
                | "q"
                | "Q" ->
                    dispose client
                    key <- null
                | "trx" -> // test read bits
                    match client.ReadBits("p/o", testReadOffsets) with
                    | Ok bytes ->
                        let str = bytes |> map toString |> joinWith ", "
                        Console.WriteLine($"OK: {str}")
                    | Error msg -> failwith $"ERROR: {msg}"
                | "trb" -> // test read bytes
                    match client.ReadBytes("p/o", testReadOffsets) with
                    | Ok bytes ->
                        let str = bytes |> map toString |> joinWith ", "
                        Console.WriteLine($"OK: {str}")
                    | Error msg -> failwith $"ERROR: {msg}"
                | "trw" -> // test read bytes
                    match client.ReadUInt16s("p/o", testReadOffsets) with
                    | Ok words ->
                        let str = words |> map toString |> joinWith ", "
                        Console.WriteLine($"OK: {str}")
                    | Error msg -> failwith $"ERROR: {msg}"
                | "trdw" -> // test read bytes
                    match client.ReadUInt32s("p/o", testReadOffsets) with
                    | Ok dwords ->
                        let str = dwords |> map toString |> joinWith ", "
                        Console.WriteLine($"OK: {str}")
                    | Error msg -> failwith $"ERROR: {msg}"
                | "trlw" -> // test read bytes
                    match client.ReadUInt64s("p/o", testReadOffsets) with
                    | Ok lwords ->
                        let str = lwords |> map toString |> joinWith ", "
                        Console.WriteLine($"OK: {str}")
                    | Error msg -> failwith $"ERROR: {msg}"
                | "trs" -> // test read strings
                    match client.ReadStrings("p/s", [||]) with
                    | Ok kvs ->
                        let str = kvs |> map (fun (k, v) -> $"{k}={v}") |> joinWith ", "
                        Console.WriteLine($"OK: {str}")
                    | Error msg -> failwith $"ERROR: {msg}"




                | "twx" -> // test write bits
                    match client.WriteBits("p/o", testReadOffsets, testWriteValues |> map (fun x -> x <> 0)) with
                    | Ok msg -> Console.WriteLine($"WriteResult: {msg}")
                    | Error msg -> failwith $"ERROR: {msg}"
                | "twb" -> // test write bytes
                    match client.WriteBytes("p/o", testReadOffsets, testWriteValues |> map byte) with
                    | Ok msg -> Console.WriteLine($"WriteResult: {msg}")
                    | Error msg -> failwith $"ERROR: {msg}"

                | "tww" -> // test write words
                    match client.WriteUInt16s("p/o", testReadOffsets, testWriteValues |> map uint16) with
                    | Ok msg -> Console.WriteLine($"WriteResult: {msg}")
                    | Error msg -> failwith $"ERROR: {msg}"
                | "twdw" -> // test write dwords
                    match client.WriteUInt32s("p/o", testReadOffsets, testWriteValues |> map uint32) with
                    | Ok msg -> Console.WriteLine($"WriteResult: {msg}")
                    | Error msg -> failwith $"ERROR: {msg}"
                | "twlw" -> // test write lwords
                    match client.WriteUInt64s("p/o", testReadOffsets, testWriteValues |> map uint64) with
                    | Ok msg -> Console.WriteLine($"WriteResult: {msg}")
                    | Error msg -> failwith $"ERROR: {msg}"

                // e.g "rs p/s hello"
                | (StartsWith "rs" | StartsWith "ws") ->
                    let tokens =
                        key.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
                        |> map (fun s -> s.ToLower())
                        |> Array.ofSeq

                    let name, keys = tokens[1], tokens[2..]

                    match key with
                    | StartsWith "rs" ->
                        match client.ReadStrings(name, keys) with
                        | Ok kvs ->
                            let str = kvs |> map (fun (k, v) -> $"{k}={v}") |> joinWith ", "
                            Console.WriteLine($"OK: {str}")
                        | _ -> failwithlog "ERROR"
                    //| StartsWith "ws" ->
                    //    match client.WriteStrings(name, tokens[2..]) with
                    //    | Ok kvs ->
                    //        let str = kvs |> map (fun (k, v) -> $"{k}={v}") |> joinWith ", "
                    //        Console.WriteLine($"OK: {str}")
                    //    | _ ->
                    //        failwithlog "ERROR"
                    | _ -> failwithlog "ERROR"



                | (StartsWith "rx" | StartsWith "rb" | StartsWith "rw" | StartsWith "rdw" | StartsWith "rlw")
                | (StartsWith "wx" | StartsWith "wb" | StartsWith "ww" | StartsWith "wdw" | StartsWith "wlw")
                | (StartsWith "write" | StartsWith "read") ->
                    match client.SendRequest(key) with
                    | Ok value -> Console.WriteLine($"OK: {value}")
                    | Error err -> Console.WriteLine($"ERR: {err}")
                | _ -> Console.WriteLine($"Unknown command: {key}")

                ()

    let serverKeyboardLoop (server: ServerDirectAccess) (ct: CancellationToken) =
        let mutable key = ""
        let testReadOffsets = [| 0..3 |]
        let testWriteValues = testReadOffsets |> map ((*) 3)

        while (key <> null && not ct.IsCancellationRequested) do
            key <- Console.ReadLine()

            if key <> null then
                let getArgs () =
                    let tokens = key.Split([|' '|], StringSplitOptions.RemoveEmptyEntries) |> Array.ofSeq
                    tokens[1..] |> map (fun s -> s.ToLower())

                match key with
                | "q"
                | "Q" -> key <- null

                | StartsWith "read" ->
                    for tag in getArgs () do
                        let value = server.Read tag
                        Console.WriteLine($"{tag}={value}")

                | StartsWith "write" ->
                    for addressWithAssignValue in getArgs () do
                        match addressWithAssignValue with
                        | RegexPattern "([^=]+)=(\w+)" [ tag; strValue ] ->
                            let uint64Value = UInt64.Parse(strValue)

                            let value =
                                match tag with
                                | AddressPattern ap ->
                                    match ap.MemoryType with
                                    | MemoryType.Bit -> uint64Value <> 0UL |> box
                                    | MemoryType.Byte -> byte uint64Value |> box
                                    | MemoryType.Word -> uint16 uint64Value |> box
                                    | MemoryType.DWord -> uint32 uint64Value |> box
                                    | MemoryType.LWord -> uint64Value |> box
                                    | _ -> failwithf ($"Unknown data type : {ap.MemoryType}")
                                | _ -> failwithf ($"Invalid tag: {tag}")

                            server.Write(tag, value)
                        | _ -> Console.WriteLine($"Illegal argments: {addressWithAssignValue}")

                | _ -> Console.WriteLine($"Unknown command: {key}")

                ()

    let registerCancelKey (cts: CancellationTokenSource) (disposable: IDisposable) =
        let handleCancelKey (args: ConsoleCancelEventArgs) =
            logDebug ("Ctrl+C pressed!")
            cts.Cancel()
            dispose disposable
            args.Cancel <- true // 프로그램을 종료하지 않도록 설정 (선택 사항)

        Console.CancelKeyPress.Add(handleCancelKey)

    let runServer withServerKeyboardLoop =
        let zmqInfo = Zmq.InitializeServer "zmqsettings.json"
        let server, cts = zmqInfo.Server, zmqInfo.CancellationTokenSource

        use subs =
            server.MemoryChangedObservable.Subscribe(fun change ->
                match change with
                | :? IOChangeInfo as change ->
                    for (tag, value) in change.GetTagNameAndValues() do
                        logDebug $"Tag change detected on server side for {tag}: {value}"
                | :? SingleStringChangeInfo as change ->
                    for (key, value) in change.GetKeysAndValues() do
                        logDebug $"Tag change detected on server side for {key}: {value}"
                | _ -> failwithlog "ERROR"

                ())

        registerCancelKey cts zmqInfo

        if withServerKeyboardLoop then
            serverKeyboardLoop server cts.Token

        while (not server.IsTerminated) do
            Thread.Sleep(1000)

        logDebug ("Exiting...")
        Thread.Sleep(1000)

    /// server 와 client 를 동시에 실행
    let runServerAndClient () =
        let zmqInfo = Zmq.Initialize "zmqsettings.json"

        let server, client, cts =
            zmqInfo.Server, zmqInfo.Client, zmqInfo.CancellationTokenSource

        use subs =
            server.MemoryChangedObservable.Subscribe(fun change ->
                match change with
                | :? IOChangeInfo as change ->
                    for (tag, value) in change.GetTagNameAndValues() do
                        logDebug $"Tag change detected on server side for {tag}: {value}"
                | :? SingleStringChangeInfo as change ->
                    for (key, value) in change.GetKeysAndValues() do
                        logDebug $"Tag change detected on server side for {key}: {value}"
                | _ -> failwithlog "ERROR"

                ())

        registerCancelKey cts zmqInfo

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
        let wr2 = client.WriteBytes("p/o", [| 0; 1; 2; 3 |], [| 99uy; 98uy; 97uy; 96uy |])

        match client.ReadBytes("p/o", [| 0; 1; 2; 3 |]) with
        | Ok bytes -> ()
        | _ -> failwithlog "ERROR"


        // 서버 직접 접근 API test
        serverKeyboardLoop server cts.Token

        // Client 경유 API test
        //clientKeyboardLoop client cts.Token

        while (not server.IsTerminated) do
            logDebug ("Waiting server terminated...")
            Thread.Sleep(1000)

        logDebug ("Exiting...")
        Thread.Sleep(1000)
        0

    [<EntryPoint>]
    let main _ =
        // test 로 server 와 client 를 동시에 실행
        runServerAndClient ()
