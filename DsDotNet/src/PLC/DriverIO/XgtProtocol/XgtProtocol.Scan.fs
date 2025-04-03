namespace XgtProtocol

open System
open System.Collections.Generic
open System.Threading
open Dual.PLC.Common.FS

module Scan =

    let writeToPLC (conn: XgtEthernet) (tags: XGTTag[]) =
        for tag in tags do
            match tag.GetWriteValue() with
            | Some value ->
                let dt = 
                    match tag.DataType with
                    | 1 -> DataType.Bit
                    | 8 -> DataType.Byte
                    | 16 -> DataType.Word
                    | 32 -> DataType.DWord
                    | 64 -> DataType.LWord
                    | _ -> failwithf "Unsupported DataType size %d" tag.DataType
                let success = conn.WriteData(tag.Address, dt, value)
                if not success then
                    failwithf "WriteData Failed: %s" tag.Address
                tag.ClearWriteValue()
            | None -> ()

    let readFromPLCBatches (conn: XgtEthernet) (batches: LWBatch[]) (notify: TagPLCValueChangedEventArgs -> unit) (notifiedOnce: HashSet<LWBatch>) =
        for batch in batches do
            let tags  = batch.DeviceInfos |> Seq.map(fun t-> t.LWordTag) |> Seq.toArray  
            try
                conn.ReadData(tags, DataType.LWord, batch.Buffer)
                for tag in batch.Tags do
                    if tag.UpdateValue(batch.Buffer) then
                        notify { Ip = conn.Ip; Tag = tag }
                
            with _ ->
                //if conn.IsConnected then
                //    failwithf "ReadData Failed: %s" (batch.BatchToText())
               
                conn.ReConnect() |> ignore

            if not (notifiedOnce.Contains(batch)) then
                notifiedOnce.Add(batch) |> ignore


    type XgtProtocalScan(plcIp: string, scanDelay: int) =
        let tagValueChangedNotify = new Event<TagPLCValueChangedEventArgs>()
        let connectChangedNotify = new Event<ConnectChangedEventArgs>()
        let notifiedOnce = HashSet<LWBatch>()
        let mutable cancelToken = new CancellationTokenSource()
        let mutable isScanRunning = false;

        let connection = XgtEthernet(plcIp, 2004)

        do
            if String.IsNullOrWhiteSpace(plcIp) then
                failwith "PLC IP is not set"

        member _.Connection = connection
        member _.IsConnected = connection.IsConnected
        member _.Ip = plcIp

        [<CLIEvent>]
        member _.TagValueChangedNotify = tagValueChangedNotify.Publish
        [<CLIEvent>]
        member _.ConnectChangedNotify = connectChangedNotify.Publish

        member private _.WriteToPLC(conn: XgtEthernet, tags: XGTTag[]) =
            writeToPLC conn tags

        member private _.ReadFromPLC(conn: XgtEthernet, batches: LWBatch[]) =
            readFromPLCBatches conn batches (fun evt -> tagValueChangedNotify.Trigger(evt)) notifiedOnce

        member private x.ParseTags(tags: string seq, isXGI: bool) =
            tags
            |> Seq.distinct
            |> Seq.toArray
            |> Array.map (fun tag ->
                match if isXGI then tryParseXgiTag tag else tryParseXgkTag tag with
                | Some (_, size, offset) -> tag, (XGTTag(tag, size, offset):>ITagPLC)
                | None -> failwithf "Unknown device or format: %s" tag
            )

        member private x.StartMonitoring(tags: string seq) =
            if not connection.IsConnected then connection.Connect() |> ignore

            let isXGI = LsXgiTagParser.IsXGI(tags)
            let xgtDictTags = x.ParseTags(tags, isXGI) |> dict
            let xgtTags = xgtDictTags.Values |>Seq.cast<XGTTag> |> Seq.toArray
            let batches = prepareReadBatches xgtTags

            async {
                while isScanRunning do
                    do! Async.Sleep 100

                cancelToken <- new CancellationTokenSource()
                try
                    isScanRunning <- true
                    while not cancelToken.IsCancellationRequested do
                        x.WriteToPLC(connection, xgtTags)
                        x.ReadFromPLC(connection, batches)
                        do! Async.Sleep scanDelay
                    isScanRunning <- false
                with ex ->
                    printfn "[!] Monitoring error on %s: %A" plcIp ex
            } |> Async.Start

            xgtDictTags

        member x.Scan(tags: string seq) =
            cancelToken.Cancel()
            x.StartMonitoring(tags)

        member x.ScanUpdate(tags: string list) =
            cancelToken.Cancel()
            x.StartMonitoring(tags)

        member x.Disconnect() =
            cancelToken.Cancel()
            if connection.IsConnected then
                connection.Disconnect() |> ignore