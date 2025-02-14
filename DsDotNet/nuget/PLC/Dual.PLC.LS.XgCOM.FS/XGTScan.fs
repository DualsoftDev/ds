namespace DsXgComm

open System
open System.Collections.Generic
open System.Linq
open System.Threading
open Dual.Common.Core.FS
open Connect
open Dual.PLC.TagParser.FS
open XGCommLib

[<AutoOpen>]
module XGTScanModule =
    type XGTTagValueChangedEventArgs = { Ip: string; Tag: XGTTag }

    type XGTScan(plcIps: seq<string>, scanDelay: int) =
        let notifiedOnce = HashSet<LWBatch>()
        let writeBuff = Array.zeroCreate<byte> (512)
        let _scanDelay = scanDelay

     
        let tagValueChangedNotify = new Event<XGTTagValueChangedEventArgs>()
        let connectChangedNotify = new Event<ConnectChangedEventArgs>()
        // PLC별 개별 `CancellationTokenSource` 생성
        let cancelScanIps =  Dictionary<string, CancellationTokenSource>()

        // PLC별 연결을 관리하는 Dictionary
        let connections =
            plcIps
            |> Seq.map (fun ip ->
                ip,
                DsXgConnection(ip, 2004, fun args ->
                    connectChangedNotify.Trigger(args) // ScanModule에서 이벤트를 처리
                )
            )
            |> dict

        let isXGI(tags:string seq) = 
            let isXGI = tags |> Seq.exists (fun t -> t.StartsWith("%"))
            if isXGI && tags |> Seq.exists (fun t -> not (t.StartsWith("%"))) then
                failwithlog "XGI and XGK tags are mixed. Tags should consist of only one type."
            isXGI

        let scanCancel(plcIp: string) = 
            if cancelScanIps.ContainsKey(plcIp) then
                cancelScanIps.[plcIp].Cancel()
                async {
                    while cancelScanIps.[plcIp].IsCancellationRequested do
                        do! Async.Sleep 50
                } |> Async.RunSynchronously

                
        let checkExistIp(plcIp) = 
            if not (connections.ContainsKey(plcIp)) then
                failwithlog $"PLC {plcIp} is not managed in the current connections."


        do
            if Seq.isEmpty plcIps then failwithlog "PLC IPs are not set"
            else plcIps |> Seq.iter (fun ip -> logInfo $"PLC IP is set to {ip}")
            
            plcIps 
            |> Seq.iter (fun ip -> cancelScanIps.Add(ip, new CancellationTokenSource()))

        new (plcIps) = XGTScan(plcIps, 10)
        new (plcIp) = XGTScan([plcIp])

        [<CLIEvent>]
        member x.TagValueChangedNotify = tagValueChangedNotify.Publish
        [<CLIEvent>]
        member x.ConnectChangedNotify = connectChangedNotify.Publish

        member x.Connections = connections
        member x.IsConnected(ip: string) = connections.[ip].IsConnected

        member private x.ParseTags(tags: string seq, isXGI: bool) =
            tags
            |> Seq.distinct
            |> Seq.toArray
            |> Array.map (fun tag ->
                match if isXGI then tryParseXgiTag tag else tryParseXgkTag tag with
                | Some (devHead, size, offset) -> tag, XGTTag(tag, size, offset)
                | None -> 
                    if ['P';'M';'K';'F';'L'].Contains tag.[0] then
                        failwithlog $"Unknown device {tag} (P, M, K, F, L device length > 4)"
                    else
                        failwithlog $"Unknown device {tag}"

            )


        /// 태그 변경을 감지하고 알림을 발송하는 함수
        member private x.NotifyTagChanges(conn: DsXgConnection, buffer: byte[], batch: LWBatch, firstRead: bool) =
            let oldBuffer = batch.Buffer
            let lwsOld = Array.init (oldBuffer.Length / 8) (fun n -> oldBuffer.GetLWord(n))
            let lwsNew = Array.init (buffer.Length / 8) (fun n -> buffer.GetLWord(n))

            if firstRead then
                batch.Tags |> Array.iter (fun t ->
                    if t.UpdateValue(buffer) then
                        tagValueChangedNotify.Trigger({ Ip = conn.Ip; Tag = t })
                )
            else
                lwsOld |> Array.iteri (fun i o ->
                    let n = lwsNew.[i]
                    if o <> n then
                        batch.Tags
                        |> Array.filter (fun t -> t.LWordOffset = i)
                        |> Array.iter (fun t ->
                            if t.UpdateValue(buffer) then
                                tagValueChangedNotify.Trigger({ Ip = conn.Ip; Tag = t })
                        )
                )

            Array.blit buffer 0 batch.Buffer 0 (min buffer.Length oldBuffer.Length)

        /// PLC 데이터를 읽는 함수
        member private x.ReadFromPLC(conn: DsXgConnection, buffer: byte[], batches: LWBatch[]) =
            for batch in batches do
                conn.CommObject.RemoveAll()
                batch.DeviceInfos |> Seq.iter conn.CommObject.AddDeviceInfo

                if conn.CommObject.ReadRandomDevice(buffer) <> 1 then
                    if conn.IsConnected then
                        failwith $"ReadRandomDevice Failed: {batch.BatchToText()}"
                    else 
                        conn.ReConnect() |> ignore

                let isFirstRead = not (notifiedOnce.Contains(batch))
                if isFirstRead then notifiedOnce.Add(batch) |> ignore

                x.NotifyTagChanges(conn, buffer, batch, isFirstRead)

        /// PLC 데이터를 쓰는 함수
        member private x.WriteToPLC(conn: DsXgConnection, tags: XGTTag array) =
            let writingTags = tags |> Array.filter (fun t -> t.GetWriteValue().IsSome)

            if writingTags.Length > 0 then
                let batches = writingTags.ChunkBySize(64)
                for batch in batches do
                    conn.CommObject.RemoveAll()
                    batch |> Seq.map (fun t -> conn.CreateDevice(t.Device, t.MemType, t.Size, t.BitOffset / 8))
                          |> Seq.iter conn.CommObject.AddDeviceInfo

                    let mutable iWrite = 0

                    batch |> Array.iter (fun item ->
                        match item.DataType with
                        | 1 | 8 -> writeBuff.[iWrite] <- Convert.ToByte(item.GetWriteValue().Value); iWrite <- iWrite + 1
                        | 16 -> let wordBytes = BitConverter.GetBytes(Convert.ToUInt16(item.GetWriteValue().Value))
                                Array.blit wordBytes 0 writeBuff iWrite wordBytes.Length
                                iWrite <- iWrite + 2
                        | 32 -> let dwordBytes = BitConverter.GetBytes(Convert.ToUInt32(item.GetWriteValue().Value))
                                Array.blit dwordBytes 0 writeBuff iWrite dwordBytes.Length
                                iWrite <- iWrite + 4
                        | 64 -> let lwordBytes = BitConverter.GetBytes(Convert.ToUInt64(item.GetWriteValue().Value))
                                Array.blit lwordBytes 0 writeBuff iWrite lwordBytes.Length
                                iWrite <- iWrite + 8
                        | _ -> failwith $"Unsupported DataType {item.DataType} for tag {item.TagName}"
                    )

                    if conn.CommObject.WriteRandomDevice(writeBuff[..iWrite-1]) <> 1 then
                         if conn.IsConnected then
                            let errMsg = String.Join(", ", tags.Select(fun f->f.TagName))
                            failwith $"WriteRandomDevice Failed. {errMsg}"
                        else 
                            conn.ReConnect() |> ignore

                    batch |> Array.iter (fun t -> t.ClearWriteValue())

         /// PLC 모니터링을 수행하는 공용 함수
        member private x.StartMonitoring(plcIp: string, tags: string seq) =
            checkExistIp plcIp
            let conn = connections.[plcIp]

            // XGI/XGK 태그 구분 및 변환
            let isXGI = isXGI(tags)
            let xgtTags = x.ParseTags(tags, isXGI) |> dict

            if xgtTags.Count > 0 then
                logInfo $"Starting monitoring for PLC {plcIp}."

                let batches = prepareReadBatches(conn, xgtTags.Values.ToArray())

                async {
                    try
                        while not cancelScanIps.[plcIp].IsCancellationRequested do
                            do! Async.Sleep _scanDelay
                            x.WriteToPLC(conn, xgtTags.Values.ToArray())
                            let readBuff = Array.zeroCreate<byte> (512)
                            x.ReadFromPLC(conn, readBuff, batches)
                    with
                    | ex -> logError $"Monitoring error for PLC {plcIp}: {ex}"
            
                    logInfo $"Stopped monitoring for PLC {plcIp}."
                    cancelScanIps.Remove(plcIp) |> ignore // 기존 항목 제거
                    cancelScanIps.Add(plcIp, new CancellationTokenSource()) // 새로운 토큰 추가

                } |> Async.Start
            else
                logWarn $"No valid monitoring tags provided for PLC {plcIp}."

            xgtTags

        /// PLC 모니터링을 시작하는 다중 스켄 함수
        member x.Scan(tagsPerPLC: IDictionary<string, string seq>) =
            let totalTags = Dictionary<string, IDictionary<string, XGTTag>>()   
            tagsPerPLC |> Seq.iter (fun kv ->
                let plcIp, tags = kv.Key, kv.Value  

                if connections.ContainsKey(plcIp) then
                    let conn = connections.[plcIp]
                    if not conn.IsConnected then
                        conn.Connect()

                    // 공용 함수 호출
                    let xgtTags = x.StartMonitoring(plcIp, tags)
                    totalTags.[plcIp] <- xgtTags
            )
            totalTags

        /// PLC 모니터링을 시작하는 싱글 스켄 함수
        member x.ScanSingle(ip:string, tags: string seq) =
            x.Scan([(ip, tags)]|> dict)


        /// PLC 모니터링 대상을 업데이트하는 함수
        member x.ScanUpdate(plcIp: string, tags: List<string>) : IDictionary<string, XGTTag> =
            checkExistIp plcIp
            scanCancel(plcIp) // 기존 모니터링 취소
            let xgtTags = x.StartMonitoring(plcIp, tags)
            xgtTags

        member x.Disconnect(plcIp: string) =
            checkExistIp plcIp
            if connections.[plcIp].IsConnected
            then 
                scanCancel plcIp
                connections.[plcIp].Disconnect()
