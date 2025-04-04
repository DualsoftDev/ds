namespace DsMxComm

open System
open System.Collections.Generic
open System.Threading
open Dual.Common.Core.FS
open Dual.PLC.Common.FS
open MelsecReadBatchModule
open System.Threading.Tasks

[<AutoOpen>]
module MelsecScanModule =

    type MelsecScan(channels: seq<int>, scanDelay: int) =

        let tagValueChangedNotify = new Event<PlcTagValueChangedEventArgs>()
        let connectChangedNotify = new Event<ConnectChangedEventArgs>()
        let cancelScanChannels = Dictionary<int,  CancellationTokenSource>()

        let connections =
            channels
            |> Seq.map (fun channel ->
                channel,
                DsMxConnection(channel, fun args -> connectChangedNotify.Trigger(args))
            )
            |> dict

        let checkExistChannel(ch) = 
            if not (connections.ContainsKey(ch)) then
                failwithlog $"PLC {ch} is not managed in the current connections."

        let scanCancel(channel: int) = 
            if cancelScanChannels.ContainsKey(channel) then
                cancelScanChannels.[channel].Cancel()
                async {
                    while cancelScanChannels.[channel].IsCancellationRequested do
                        do! Async.Sleep 50
                } |> Async.RunSynchronously
        do 
            if Seq.isEmpty channels then failwithlog "MxComponent channels are not set"
            else channels |> Seq.iter (fun ip -> logInfo $"MxComponent channels is set to {ip}")
            
            channels 
            |> Seq.iter (fun ip -> cancelScanChannels.Add(ip, new CancellationTokenSource()))

        interface IPlcConnector with
            member this.Connect(): unit = raise (System.NotImplementedException())
            member this.Disconnect(): unit = raise (System.NotImplementedException())
            member this.IpOrStation: string = raise (System.NotImplementedException())
            member this.IsConnected: bool = raise (System.NotImplementedException())
            member this.ReConnect(): unit = raise (System.NotImplementedException())
            member this.Read(_address: string, _dataType: PlcDataSizeType): obj = raise (System.NotImplementedException())
            member this.Write(_address: string, _dataType: PlcDataSizeType, _value: obj): bool = raise (System.NotImplementedException())
            member x.ConnectChanged = connectChangedNotify.Publish
            member x.TagValueChanged = tagValueChangedNotify.Publish

        new (channels) = MelsecScan(channels, 5)
        new (channel) = MelsecScan([channel])
        [<CLIEvent>]
        member x.PlcTagValueChangedNotify = tagValueChangedNotify.Publish
        [<CLIEvent>]
        member x.ConnectChangedNotify = connectChangedNotify.Publish

        /// PLC 데이터를 읽는 함수 (최대 512 Word 읽기)
        member private x.ReadFromPLC(conn: DsMxConnection, batches: WordBatch[]) =
            for batch in batches do
                let batchKeys = batch.Buffer.Keys |> Seq.toArray
                let readWords = conn.ReadDeviceRandom(batchKeys) 

                for i in 0..(readWords.Length-1) do
                    let word = readWords.[i]
                    let tagWord = batchKeys.[i]
                    batch.Tags
                    |> Seq.filter (fun t -> t.WordTag = tagWord)
                    |> Seq.iter (fun t -> 
                        if t.UpdateValue(word) then
                            tagValueChangedNotify.Trigger({ Ip = conn.LogicalStationNumber.ToString(); Tag = t })
                    )

        /// PLC 데이터를 쓰는 함수 (최대 512 Word 쓰기)
        member private x.WriteToPLC(conn: DsMxConnection, batches: WordBatch[]) =
            for batch in batches do
                let writingTags = batch.Tags |> Seq.filter (fun t -> (t:>IPlcTagReadWrite).GetWriteValue().IsSome)
                let keys = writingTags |> Seq.map (fun t -> t.Address) |> Seq.toArray
                let values = writingTags 
                             |> Seq.map (fun t -> Convert.ToInt16((t:>IPlcTagReadWrite).GetWriteValue().Value))
                             |> Seq.toArray

                if keys.any()
                then
                    try
                        conn.WriteDeviceRandom(keys, values) |> ignore
                    with
                    | ex -> logError $"WriteToPLC error: {ex}"

                writingTags |> Seq.cast<IPlcTagReadWrite> |> Seq.iter (fun t -> t.ClearWriteValue())


        /// PLC 모니터링을 시작하는 함수
        /// PLC 모니터링을 시작하는 함수
        member private x.StartMonitoring(ch: int, tags: string seq) =
            checkExistChannel ch
            let conn = connections.[ch]
            let batches = prepareReadBatches tags

            if batches.Length > 0 then
                logInfo $"Starting monitoring for ch {ch}."
                async {
                    try
                        while not cancelScanChannels[ch].IsCancellationRequested do
                            let! writeTask = Async.StartChild (async { x.WriteToPLC(conn, batches) }, 3000)
                            let! readTask = Async.StartChild (async { x.ReadFromPLC(conn, batches) }, 3000)

                            try
                                do! writeTask
                                do! readTask
                                do! Async.Sleep scanDelay // 필요 시 활성화

                            with
                            | :? TimeoutException ->
                                logError $"Timeout occurred while communicating with PLC {ch}."
                    
                    with
                    | ex ->
                        logError $"Error in Write/Read operation for PLC {ch}: {ex}"
        
                    logInfo $"Stopped monitoring for PLC {ch}."
                    cancelScanChannels.Remove(ch) |> ignore
                    cancelScanChannels[ch] <- new CancellationTokenSource()
                } |> Async.StartImmediate
            else
                logWarn $"No valid monitoring tags provided for PLC {ch}."

            batches |> Seq.collect (fun batch -> batch.Tags) 
                    |> Seq.map (fun t -> t.Address, t :> IPlcTagReadWrite)
                    |> dict


        /// PLC 모니터링을 시작하는 다중 스켄 함수
        member x.Scan(tagsPerPLC: IDictionary<int, string seq>) =
            let totalTags = Dictionary<int, IDictionary<string, IPlcTagReadWrite>>()   
            tagsPerPLC |> Seq.iter (fun kv ->
                let ch, tags = kv.Key, kv.Value  

                if connections.ContainsKey(ch) then
                    let conn = connections.[ch]
                    if not conn.IsConnected then
                        conn.Connect()

                    // 공용 함수 호출
                    let mxTags = x.StartMonitoring(ch, tags)
                    totalTags.[ch] <- mxTags
            )
            totalTags

        /// PLC 모니터링을 시작하는 싱글 스켄 함수
        member x.ScanSingle(channel:int, tags: string seq) =
            let tagSet = x.Scan([(channel, tags)]|> dict) |> Seq.head
            tagSet.Value

        /// PLC 모니터링 대상을 업데이트하는 함수
        member x.ScanUpdate(channel: int, tags: List<string>) : IDictionary<string, IPlcTagReadWrite> =
            checkExistChannel channel
            scanCancel(channel) // 기존 모니터링 취소
            let xgtTags = x.StartMonitoring(channel, tags)
            xgtTags

        member x.Disconnect(channel: int) =
            checkExistChannel channel
            if connections.[channel].IsConnected
            then 
                scanCancel channel
                connections.[channel].Disconnect()
