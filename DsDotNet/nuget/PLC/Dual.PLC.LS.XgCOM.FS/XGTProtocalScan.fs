namespace DsXgComm

open System
open System.Collections.Generic
open System.Linq
open System.Threading
open Dual.Common.Core.FS
open XgtProtocol.FS
open Dual.PLC.Common.FS

[<AutoOpen>]
module XGTProtocalScanModule =

    type XGTProtocalScan(plcIps: seq<string>, scanDelay: int) =
        let tagValueChangedNotify = new Event<TagPLCValueChangedEventArgs>()
        let connectChangedNotify = new Event<ConnectChangedEventArgs>()
        let notifiedOnce = HashSet<LWBatchXGTProtocal>()
        let writeBuff = Array.zeroCreate<byte> 512
        let _scanDelay = scanDelay
        let cancelScanIps = Dictionary<string, CancellationTokenSource>()

        let connections =
            plcIps
            |> Seq.map (fun ip -> ip, XgtProtocol.FS.XgtProtocol(ip, 2004))
            |> dict

            
    


        do
            if Seq.isEmpty plcIps then failwithlog "PLC IPs are not set"
            else plcIps |> Seq.iter (fun ip -> logInfo $"PLC IP is set to {ip}")
            plcIps |> Seq.iter (fun ip -> cancelScanIps.Add(ip, new CancellationTokenSource()))

        interface IScanPLC with
            member x.TagValueChangedNotify = tagValueChangedNotify
            member x.ConnectChangedNotify = connectChangedNotify

        new(plcIps) = XGTProtocalScan(plcIps, 20)
        new(plcIp) = XGTProtocalScan([plcIp])

        [<CLIEvent>]
        member x.TagValueChangedNotify = tagValueChangedNotify.Publish
        [<CLIEvent>]
        member x.ConnectChangedNotify = connectChangedNotify.Publish

        member x.Connections = connections
        member x.IsConnected(ip: string) = connections.[ip].IsConnected

        member private x.NotifyTagChanges(conn: XgtProtocol, buffer: byte[], batch: LWBatchXGTProtocal, firstRead: bool) =
            let oldBuffer = batch.Buffer
            let lwsOld = Array.init (oldBuffer.Length / 8) (fun n -> oldBuffer.GetLWord(n))
            let lwsNew = Array.init (buffer.Length / 8) (fun n -> buffer.GetLWord(n))

            if firstRead then
                batch.Tags |> Array.iter (fun t ->
                    if t.UpdateValue(buffer) then
                        tagValueChangedNotify.Trigger({ Ip = conn.Ip; Tag = t }))
            else
                lwsOld
                |> Array.iteri (fun i o ->
                    let n = lwsNew.[i]
                    if o <> n then
                        batch.Tags
                        |> Array.filter (fun t -> t.LWordOffset = i)
                        |> Array.iter (fun t ->
                            if t.UpdateValue(buffer) then
                                tagValueChangedNotify.Trigger({ Ip = conn.Ip; Tag = t })))

            Array.blit buffer 0 batch.Buffer 0 (min buffer.Length oldBuffer.Length)

        member private x.WriteToPLC(conn: XgtProtocol, tags: ITagPLC array) =
            let writingTags = tags |> Array.filter (fun t -> t.GetWriteValue().IsSome)
            for tag in writingTags do
                let dataType =
                    match (tag :?> XGTTag).DataType with
                    | 1 -> DataType.Bit
                    | 8 -> DataType.Byte
                    | 16 -> DataType.Word
                    | 32 -> DataType.DWord
                    | 64 -> DataType.LWord
                    | dt -> failwith $"Unsupported DataType {dt} for tag {tag.Address}"
                let value = tag.GetWriteValue().Value
                if conn.WriteData(tag.Address, dataType, value) <> true then
                    failwith $"WriteData Failed for tag {tag.Address}"
                tag.ClearWriteValue()

        member private x.ReadFromPLC(conn: XgtProtocol, batches: LWBatchXGTProtocal[]) =
            XGTProtocalBatchModule.readFromPLCBatches
                conn
                batches
                (fun evt -> tagValueChangedNotify.Trigger(evt))
                notifiedOnce



        member private x.StartMonitoring(plcIp: string, tags: string seq) =
            if not (connections.ContainsKey(plcIp)) then failwithlog $"PLC {plcIp} not found"
            let conn = connections.[plcIp]
            if not conn.IsConnected then conn.Connect() |> ignore

            let xgtTags =
                tags
                |> Seq.distinct
                |> Seq.toArray
                |> Array.map (fun tag ->
                    match tryParseXgiTag tag with
                    | Some (_, size, offset) -> tag, XGTTag(tag, size, offset)
                    | None -> failwithlog $"Unknown device {tag}")
                |> dict

            if xgtTags.Count > 0 then
                logInfo $"Starting monitoring for PLC {plcIp}"
                let batches = XGTProtocalBatchModule.prepareReadBatches(xgtTags.Values.ToArray())

                async {
                    try
                        while not cancelScanIps.[plcIp].IsCancellationRequested do
                            do! Async.Sleep _scanDelay
                            x.WriteToPLC(conn, xgtTags.Values.Select(fun f -> f :> ITagPLC).ToArray())
                            let readBuff = Array.zeroCreate<byte> 512
                            x.ReadFromPLC(conn, readBuff, batches)
                    with ex -> logError $"Monitoring error for PLC {plcIp}: {ex}"
                } |> Async.Start
            else
                logWarn $"No valid monitoring tags provided for PLC {plcIp}"

            xgtTags |> Seq.map (fun kv -> kv.Key, kv.Value :> ITagPLC) |> dict

        member x.Scan(tagsPerPLC: IDictionary<string, string seq>) =
            let totalTags = Dictionary<string, IDictionary<string, ITagPLC>>()
            for kv in tagsPerPLC do
                let plcIp, tags = kv.Key, kv.Value
                let dict = x.StartMonitoring(plcIp, tags)
                totalTags.Add(plcIp, dict)
            totalTags

        member x.ScanSingle(ip: string, tags: string seq) =
            x.Scan(dict [ ip, tags ]).First().Value

        member x.ScanUpdate(plcIp: string, tags: List<string>) : IDictionary<string, ITagPLC> =
            if cancelScanIps.ContainsKey(plcIp) then
                cancelScanIps.[plcIp].Cancel()
                cancelScanIps.[plcIp] <- new CancellationTokenSource()
            x.StartMonitoring(plcIp, tags)

        member x.Disconnect(plcIp: string) =
            if connections.ContainsKey(plcIp) && connections.[plcIp].IsConnected then
                cancelScanIps.[plcIp].Cancel()
                connections.[plcIp].Disconnect() |>ignore
