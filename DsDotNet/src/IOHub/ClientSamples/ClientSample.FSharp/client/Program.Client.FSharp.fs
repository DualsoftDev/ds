namespace Client

open IO.Core
open System
open System.Threading
open ZmqTestModule

module ZmqTestClient =
    let onTagChanged (change:TagChangedInfo) =
        match change with
        | :? IOTagChangedInfo as change ->
            let n = change.Offsets.Length
            let offsets = change.Offsets
            let values = change.Values
            Console.WriteLine($"Total {n} tag changed on {change.Path} with bitLength={change.ContentBitLength}");
            match change.ContentBitLength with
            | 1 ->
                let values = values :?> bool[]
                for i = 0 to n - 1 do
                    Console.WriteLine($"  {offsets[i]}: {values[i]}");
            | 8 ->
                let values = values :?> byte[]
                for i = 0 to n - 1 do
                    Console.WriteLine($"  {offsets[i]}: {values[i]}");
            | 16 ->
                let values = values :?> uint16[]
                for i = 0 to n - 1 do
                    Console.WriteLine($"  {offsets[i]}: {values[i]}");
            | 32 ->
                let values = values :?> uint32[]
                for i = 0 to n - 1 do
                    Console.WriteLine($"  {offsets[i]}: {values[i]}");
            | 64 ->
                let values = values :?> uint64[]
                for i = 0 to n - 1 do
                    Console.WriteLine($"  {offsets[i]}: {values[i]}");
            | _ ->
                failwith "Not supported"
        | :? StringTagChangedInfo as change ->
            let n = change.Keys.Length
            Console.WriteLine($"Total {n} string tag changed on {change.Path}");
            for i = 0 to n - 1 do
                Console.WriteLine($"  {change.Keys[i]}: {change.Values[i]}")
        | _ ->
            failwith "ERROR"

    [<EntryPoint>]
    let main _ = 
        let cts = new CancellationTokenSource()
        let port = 5555
        let client = new Client($"tcp://localhost:{port}")
        let meta = client.GetMeta()
        client.TagChangedSubject.Subscribe(onTagChanged) |> ignore

        registerCancelKey cts client
        clientKeyboardLoop client cts.Token

        0
