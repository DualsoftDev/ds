namespace DsMxComm

open System
open System.Collections.Generic
open System.Linq
open System.Threading
open Dual.Common.Core.FS
open Dual.PLC.Common.FS

[<AutoOpen>]
module MelsecScanModule =

    type MelsecScan(channels: seq<int>, scanDelay: int) =
        let writeBuff = Array.zeroCreate<byte> (512 * 2)
        let _scanDelay = scanDelay

        let tagValueChangedNotify = new Event<TagPLCValueChangedEventArgs>()
        let connectChangedNotify = new Event<ConnectChangedEventArgs>()
        let cancelScanIps = Dictionary<int, CancellationTokenSource>()

        let connections =
            channels
            |> Seq.map (fun ip ->
                ip,
                DsMxConnection(ip, fun args -> connectChangedNotify.Trigger(args))
            )
            |> dict

        let scanCancel(ch: int) = 
            if cancelScanIps.ContainsKey(ch) then
                cancelScanIps.[ch].Cancel()
                async {
                    while cancelScanIps.[ch].IsCancellationRequested do
                        do! Async.Sleep 50
                } |> Async.RunSynchronously

        let checkExistIp(plcIp) = 
            if not (connections.ContainsKey(plcIp)) then
                failwithlog $"PLC {plcIp} is not managed in the current connections."

        do
            if Seq.isEmpty channels then failwithlog "PLC IPs are not set"
            else channels |> Seq.iter (fun ip -> logInfo $"PLC IP is set to {ip}")
            
            channels |> Seq.iter (fun ch -> cancelScanIps.Add(ch, new CancellationTokenSource()))

        new (channels) = MelsecScan(channels, 10)
        new (channel) = MelsecScan([channel])

        [<CLIEvent>]
        member x.TagValueChangedNotify = tagValueChangedNotify.Publish
        [<CLIEvent>]
        member x.ConnectChangedNotify = connectChangedNotify.Publish

        member x.Connections = connections
        member x.IsConnected(ch: int) = connections.[ch].IsConnected

        member private x.ParseTags(tags: string seq) =
            tags
            |> Seq.distinct
            |> Seq.toArray
            |> Array.map (fun tag ->
                match tryParseMxTag tag with
                | Some (_, size, offset) -> tag, MxTag(tag, size, offset)
                | None -> failwithlog $"Unknown device {tag}"
            )

        /// PLC 데이터를 읽는 함수 (최대 512 Word 읽기)
        member private x.ReadFromPLC(conn: DsMxConnection, dicTags: IDictionary<string, MxTag seq>) =
            let batches = dicTags.Keys 
                            |> Seq.distinct
                            |> Seq.chunkBySize 512 

            for batch in batches do
                let readWords = conn.ReadDeviceRandom(batch) 
                for i in 0..(readWords.Length-1) do
                    let word = readWords.[i]
                    let tagWord = batch.[i]
                    dicTags[tagWord]
                    |> Seq.iter(fun t -> 
                        if t.UpdateValue(word)
                        then 
                            tagValueChangedNotify.Trigger({ Ip = conn.StationNumber.ToString(); Tag = t }  )
                    )
        
        /// PLC 데이터를 쓰는 함수 (최대 512 Word 쓰기)
        member private x.WriteToPLC(conn: DsMxConnection, dicTags: IDictionary<string, MxTag seq>) =
            let batches = dicTags.Keys 
                            |> Seq.filter (fun k -> dicTags.[k] |> Seq.exists (fun t -> t.GetWriteValue().IsSome))
                            |> Seq.distinct
                            |> Seq.chunkBySize 512 

            for batch in batches do   LWBatch 객채 다시만들어 구현 필요
                let readWords = conn.WriteDeviceRandom(batch, ) 
                for i in 0..(readWords.Length-1) do
                    let word = readWords.[i]
                    let tagWord = batch.[i]
                    dicTags[tagWord]
                    |> Seq.iter(fun t -> 
                        if t.UpdateValue(word)
                        then 
                            tagValueChangedNotify.Trigger({ Ip = conn.StationNumber.ToString(); Tag = t }  )
                    )
        
        
        /// PLC 모니터링을 시작하는 함수
        member private x.StartMonitoring(ch: int, tags: string seq) =
            checkExistIp ch
            let conn = connections.[ch]

            let mxTags = x.ParseTags(tags) |> Seq.groupBy fst |> Seq.map (fun (key, group) -> key, group |> Seq.map snd) |> dict

            if mxTags.Count > 0 then
                logInfo $"Starting monitoring for ch {ch}."

                async {
                    try
                        while not cancelScanIps.[ch].IsCancellationRequested do
                            do! Async.Sleep _scanDelay
                            x.WriteToPLC(conn, mxTags)
                            x.ReadFromPLC(conn, mxTags)
                    with
                    | ex -> logError $"Monitoring error for PLC {ch}: {ex}"
                
                    logInfo $"Stopped monitoring for PLC {ch}."
                    cancelScanIps.[ch] <- new CancellationTokenSource()

                } |> Async.Start
            else
                logWarn $"No valid monitoring tags provided for PLC {ch}."
