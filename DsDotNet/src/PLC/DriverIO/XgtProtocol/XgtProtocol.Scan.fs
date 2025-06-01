namespace XgtProtocol

open System
open System.Linq
open System.Collections.Generic
open Dual.PLC.Common.FS

type XgtPlcScan(ip: string, localEthernet: bool, scanDelay: int, timeoutMs: int, isMonitorOnly: bool) as this =
    inherit PlcScanBase(ip, scanDelay, isMonitorOnly)
    let localEthernet = true //임시  

    let connection = XgtEthernet(ip, 2004, timeoutMs)
    let mutable xgtTags: XGTTag[] = [||]
    let mutable lowSpeedAreaBatches: PlcBatchBase<XGTTag>[] = [||]
    let mutable highSpeedAreaBatches: PlcBatchBase<XGTTag>[] = [||]
    let notifiedOnce = HashSet<PlcBatchBase<XGTTag>>()
    let tagMap = Dictionary<ScanAddress, PlcTagBase>()

    /// 공통 읽기 함수
    let readAreaBatches (batches: PlcBatchBase<XGTTag>[], delayMs: int) =
        try
            for batch in batches do
                let addresses =
                    batch.Tags
                    |> Seq.map (fun tag -> if localEthernet then tag.LWordTag else tag.QWordTag)
                    |> Seq.distinct
                    |> Seq.toArray

                let dataType = 
                    if localEthernet then PlcDataSizeType.UInt64 
                    else PlcDataSizeType.UInt128

                let dataTypes = Array.create addresses.Length dataType

                connection.Reads(addresses, localEthernet, dataTypes, batch.Buffer)

                for tag in batch.Tags do
                    if tag.UpdateValue(batch.Buffer) then
                        this.TriggerTagChanged { Ip = ip; Tag = tag }

                if notifiedOnce.Add(batch) then
                    () // 최초 알림 시점에 필요한 로직 확장 가능

                Async.Sleep(delayMs) |> Async.RunSynchronously
        with ex ->
            eprintfn $"[⚠️ XGT Read Error] IP: {ip}, 예외: {ex.Message}"
            connection.ReConnect() |> ignore

    // ---------------------------
    // 연결 관련
    // ---------------------------
    override _.Connect() =
        if not connection.IsConnected then
            if connection.Connect() then
                base.TriggerConnectChanged { Ip = ip; State = Connected }
            else
                base.TriggerConnectChanged { Ip = ip; State = ConnectFailed }
                failwith $"XGT 연결 실패: {ip}"

    override _.Disconnect() =
        this.StopScan() |> ignore
        base.TriggerConnectChanged { Ip = ip; State = Disconnected }

    override _.IsConnected = connection.IsConnected

    // ---------------------------
    // 태그 쓰기
    // ---------------------------
    //override _.WriteTags() =
    //    let tags = xgtTags |> Seq.filter (fun f -> f.GetWriteValue().IsSome)
    //    if tags.Any()
    //    then
    //        let addresses = tags |> Seq.map (fun tag -> tag.Address) |> Seq.toArray
    //        let dataTypes = tags |> Seq.map (fun tag -> tag.DataType) |> Seq.toArray
    //        let values    = tags |> Seq.map (fun tag -> tag.GetWriteValue().Value) |> Seq.toArray

    //        if not (connection.Writes(addresses, localEthernet, dataTypes, values)) then
    //            failwith $"Write 실패: {addresses}"

    //        tags |> Seq.iter(fun f-> f.ClearWriteValue())


    override _.WriteTags() =
        for tag in xgtTags do
            match tag.GetWriteValue() with
            | Some value ->
                if not (connection.Write(tag.Address, localEthernet, tag.DataType, value)) then
                    failwith $"Write 실패: {tag.Address}"
                tag.ClearWriteValue()
            | None -> ()
    // ---------------------------
    // 읽기 영역
    // ---------------------------
    override _.ReadLowSpeedArea(delayMs: int) =
        readAreaBatches (lowSpeedAreaBatches, delayMs)

    override _.ReadHighSpeedArea(delayMs: int) =
        readAreaBatches (highSpeedAreaBatches, delayMs)

    // ---------------------------
    // 태그 준비 및 파싱
    // ---------------------------
    override _.PrepareTags(tags: TagInfo seq) =
        tagMap.Clear()
        let isXGI = LsTagParser.IsXGI(tags |> Seq.map (fun s -> s.Address))

        let parsed =
            tags
            |> Seq.distinct
            |> Seq.choose (fun scanTag ->
                match if isXGI then tryParseXgiTag scanTag.Address else tryParseXgkTag scanTag.Address with
                | Some (_dev, size, offset) ->
                    let tag = XGTTag(scanTag.Name, scanTag.Address, PlcDataSizeType.FromBitSize(size), offset, scanTag.IsOutput)
                    tag.IsLowSpeedArea <- scanTag.IsLowSpeedArea
                    tagMap[scanTag.Address] <- tag
                    Some tag
                | None ->
                    printfn $"[⚠️ 무시됨] 태그 파싱 실패: {scanTag.Name}"
                    None
            )
            |> Seq.toArray

        xgtTags <- parsed

        /// 공용 배치 생성 함수
        let prepareBatches (tags: XGTTag[]) =
            if localEthernet then
                prepareRead64Batches tags |> Array.map (fun b -> b :> PlcBatchBase<XGTTag>)
            else
                prepareRead128Batches tags |> Array.map (fun b -> b :> PlcBatchBase<XGTTag>)

        highSpeedAreaBatches <-
            parsed
            |> Array.filter (fun b -> not b.IsLowSpeedArea)
            |> prepareBatches

        lowSpeedAreaBatches <-
            parsed
            |> Array.filter (fun b -> b.IsLowSpeedArea)
            |> prepareBatches

        upcast tagMap
