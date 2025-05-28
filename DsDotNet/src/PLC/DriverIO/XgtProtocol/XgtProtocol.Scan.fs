namespace XgtProtocol

open System
open System.Collections.Generic
open Dual.PLC.Common.FS

type XgtPlcScan(ip: string, localEthernet:bool, scanDelay: int, timeoutMs: int, isMonitorOnly: bool) as this =
    inherit PlcScanBase(ip, scanDelay, isMonitorOnly)

    let connection = XgtEthernet(ip, 2004, timeoutMs)
    let mutable xgtTags: XGTTag[] = [||]
    let mutable lowSpeedAreaBatches: LWBatch[] = [||]
    let mutable highSpeedAreaBatches: LWBatch[] = [||]
    let notifiedOnce = HashSet<LWBatch>()
    let tagMap = Dictionary<ScanAddress, PlcTagBase>()
      /// 공통 읽기 함수
    let readAreaBatches ((batches: LWBatch[]), (delayMs: int)) =
        try
            for batch in batches do
                // 중복 제거된 LWord 주소만 추출하여 읽기 요청
                let addresses =
                    batch.Tags
                    |> Seq.map (fun tag -> tag.LWordTag)
                    |> Seq.distinct
                    |> Seq.toArray

                connection.Reads(addresses, PlcDataSizeType.UInt64, batch.Buffer)

                // 태그별 값 업데이트 및 변경 이벤트 발생
                for tag in batch.Tags do
                    if tag.UpdateValue(batch.Buffer) then
                        this.TriggerTagChanged { Ip = ip; Tag = tag }

                // 최초 읽기 알림만 등록
                if notifiedOnce.Add(batch) then
                    () // 추후 알림 로직 확장 여지

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
        if connection.IsConnected then
            connection.Disconnect() |> ignore
            base.TriggerConnectChanged { Ip = ip; State = Disconnected }

    override _.IsConnected = connection.IsConnected

    // ---------------------------
    // 태그 쓰기
    // ---------------------------
    override _.WriteTags() =
        for tag in xgtTags do
            match tag.GetWriteValue() with
            | Some value ->
                if not (connection.Write(tag.Address, tag.DataType, value)) then
                    failwith $"Write 실패: {tag.Address}"
                tag.ClearWriteValue()
            | None -> ()

    override _.ReadLowSpeedArea(delayMs: int) =
        readAreaBatches (lowSpeedAreaBatches, delayMs)
    override _.ReadHighSpeedArea(delayMs: int) =
        readAreaBatches (highSpeedAreaBatches, delayMs)

    // ---------------------------
    // 태그 준비 및 파싱
    // ---------------------------
    override _.PrepareTags(tags: TagInfo seq)  =
        tagMap.Clear()
        let isXGI = LsTagParser.IsXGI(tags |> Seq.map(fun s-> s.Address))

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
        highSpeedAreaBatches <- prepareReadBatches (parsed |> Array.filter (fun b ->not  b.IsLowSpeedArea))
        lowSpeedAreaBatches   <- prepareReadBatches (parsed |> Array.filter (fun b ->  b.IsLowSpeedArea))
        upcast tagMap
