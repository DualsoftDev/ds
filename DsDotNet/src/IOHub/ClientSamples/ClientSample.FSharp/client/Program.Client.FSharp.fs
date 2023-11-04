namespace Client

open IO.Core
open System
open System.Threading
open Dual.Common.Core.FS
open ZmqTestModule

module ZmqTestClient =
    let onTagChanged (change:TagChangedInfo) =
        Console.WriteLine($"Total {change.Offsets.Length} tag changed on {change.Path} with bitLength={change.ContentBitLength}");
        match change.ContentBitLength with
        | 1 ->
            let values = change.Values :?> bool[]
            for (i, offset) in change.Offsets |> indexed do
                Console.WriteLine($"  {offset}: {values[i]}");
        | 8 ->
            let values = change.Values :?> byte[]
            for (i, offset) in change.Offsets |> indexed do
                Console.WriteLine($"  {offset}: {values[i]}");
        | 16 ->
            let values = change.Values :?> uint16[]
            for (i, offset) in change.Offsets |> indexed do
                Console.WriteLine($"  {offset}: {values[i]}");
        | 32 ->
            let values = change.Values :?> uint32[]
            for (i, offset) in change.Offsets |> indexed do
                Console.WriteLine($"  {offset}: {values[i]}");
        | 64 ->
            let values = change.Values :?> uint64[]
            for (i, offset) in change.Offsets |> indexed do
                Console.WriteLine($"  {offset}: {values[i]}");
        | _ ->
            failwith "Not supported"

    [<EntryPoint>]
    let main _ = 
        let cts = new CancellationTokenSource()
        let port = 5555
        let client = new Client($"tcp://localhost:{port}")
        client.TagChangedSubject.Subscribe(onTagChanged) |> ignore

        registerCancelKey cts client
        clientKeyboardLoop client cts.Token

        0
