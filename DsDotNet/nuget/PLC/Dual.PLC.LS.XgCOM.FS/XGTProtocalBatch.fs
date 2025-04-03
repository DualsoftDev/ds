namespace DsXgComm

open System
open System.Collections.Generic
open System.Reactive.Subjects
open System.Threading
open System.Reactive.Disposables
open Dual.Common.Core.FS
open XGCommLib
open XgtProtocol.FS

[<AutoOpen>]
module XGTProtocalBatchModule =

    type DeviceXGTProtocal() =
        member val Device = "" with get, set
        member val MemoryType = 'B' with get, set
        member val Size = 0 with get, set
        member val Address = 0 with get, set

    type LWBatchXGTProtocal(buffer: byte[], deviceInfos: DeviceXGTProtocal[], tags: XGTTag[]) =
        let mutable tags = tags
        member val Buffer = buffer with get, set
        member val DeviceInfos = deviceInfos with get
        member this.Tags = tags
        member this.LWordAddress =
            if tags.Length > 0 then
                let tag = tags.[0]
                sprintf "%sL%d" tag.Device (tag.BitOffset / 64)
            else ""
        member this.SetTags(newTags) = tags <- newTags
        member this.BatchToText() = 
            tags 
            |> Seq.groupBy (fun t -> t.Device) 
            |> Seq.map (fun (device, tagGroup) -> 
                let maxBitOffset = tagGroup |> Seq.map (fun t -> t.BitOffset) |> Seq.max
                sprintf "Device: %s, Read BitOffset: %d" device maxBitOffset)
            |> String.concat "\n"

    let createDevice(deviceCode: string, memType: char, size: int, offset: int) : DeviceXGTProtocal =
        let dev = new DeviceXGTProtocal()
        dev.Device <- deviceCode
        dev.MemoryType <- memType
        dev.Size <- size
        dev.Address <- offset
        dev

    let prepareReadBatches(tagInfos: XGTTag[]) : LWBatchXGTProtocal[] =
        let grouped = 
            tagInfos
            |> Array.groupBy (fun ti -> ti.Device, ti.BitOffset / 64)
            |> Array.sortBy (fun ((dev, offset), _) -> dev, offset)

        grouped
        |> Array.map (fun ((dev, lwordOffset), tagsInGroup) ->
            tagsInGroup |> Array.iter (fun t -> t.LWordOffset <- lwordOffset)
            let buffer = Array.zeroCreate<byte> 8
            let devices = [| createDevice(dev, 'B', 8, lwordOffset * 8) |]
            LWBatchXGTProtocal(buffer, devices, tagsInGroup))

    let getDataTypeFromSize (size: int) : DataType =
        match size with
        | 1 -> DataType.Bit
        | 8 -> DataType.Byte
        | 16 -> DataType.Word
        | 32 -> DataType.DWord
        | 64 -> DataType.LWord
        | _ -> failwith $"Unsupported DataType size {size}"

    let writeToPLC (conn: XgtProtocol.FS.XgtProtocol) (tags: XGTTag[]) =
        for tag in tags do
            match tag.GetWriteValue() with
            | Some value ->
                let dt = getDataTypeFromSize tag.DataType
                let success = conn.WriteData(tag.Address, dt, value)
                if not success then
                    failwith $"WriteData Failed: {tag.Address}"
                tag.ClearWriteValue()
            | None -> ()

    let readFromPLC (conn: XgtProtocol.FS.XgtProtocol) (batch: LWBatchXGTProtocal) (notify: TagPLCValueChangedEventArgs -> unit) : bool =
        let success = conn.ReadData(batch.LWordAddress, DataType.LWord, batch.Buffer)
        if success then
            batch.Tags
            |> Array.iter (fun tag ->
                match tag with
                | :? XGTTag as xgtTag ->
                    if xgtTag.UpdateValue(batch.Buffer) then
                        notify { Ip = conn.Ip; Tag = tag }
                | _ -> ())
        success

    let readFromPLCBatches
        (conn: XgtProtocol.FS.XgtProtocol)
        (batches: LWBatchXGTProtocal[])
        (notify: TagPLCValueChangedEventArgs -> unit)
        (notifiedOnce: HashSet<LWBatchXGTProtocal>) =
        for batch in batches do
            let success = readFromPLC conn batch notify
            if not success then
                if conn.IsConnected then
                    failwith $"ReadData Failed: {batch.BatchToText()}"
                else
                    conn.ReConnect() |> ignore

            if not (notifiedOnce.Contains(batch)) then
                notifiedOnce.Add(batch) |> ignore






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
