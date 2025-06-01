namespace MelsecProtocol

open System
open System.Collections.Generic
open Dual.PLC.Common.FS

/// MELSEC PLC 스캔 구현 (DWord 랜덤 읽기 최적화 기반)
type MxPlcScan(ip: string, port:uint16, isUPD, scanDelay: int, timeoutMs: int, isMonitorOnly: bool) as this =
    inherit PlcScanBase(ip, scanDelay, isMonitorOnly)

    let connection = new MxEthernet(ip, port |> int, timeoutMs, isUPD)
    let mutable tags: MelsecTag[] = [||]
    let mutable lowSpeedAreaBatches: DWBatch[] = [||]
    let mutable highSpeedAreaBatches: DWBatch[] = [||]
    let notifiedOnce = HashSet<DWBatch>()
    let tagMap = Dictionary<ScanAddress, PlcTagBase>()
    
    /// 공통 읽기 함수
    let readAreaBatches ((batches: DWBatch[]), (delayMs: int)) =
        try
            for batch in batches do
               
                batch.Buffer <- connection.ReadDWordRandom(batch)

                // 태그별 값 업데이트 및 변경 이벤트 발생
                for tag in batch.Tags do
                    if tag.UpdateValue(batch.Buffer) then
                        this.TriggerTagChanged { Ip = ip; Tag = tag }

                // 최초 읽기 알림만 등록
                if notifiedOnce.Add(batch) then
                    () // 추후 알림 로직 확장 여지

                Async.Sleep(delayMs) |> Async.RunSynchronously  

        with ex -> 
            failwith $"[❌ 연결 실패] MELSEC ({ip}): {ex.Message}"

    override _.ReadLowSpeedArea(delayMs: int) =
        readAreaBatches (lowSpeedAreaBatches, delayMs)

    override _.ReadHighSpeedArea(delayMs: int) =
        readAreaBatches (highSpeedAreaBatches, delayMs)

    override _.Connect() =
        try
            base.TriggerConnectChanged { Ip = ip; State = Connected }
        with ex ->
            base.TriggerConnectChanged { Ip = ip; State = ConnectFailed }
            failwith $"[❌ 연결 실패] MELSEC ({ip}): {ex.Message}"

    override _.Disconnect() =
        this.StopScan() |> ignore
        base.TriggerConnectChanged { Ip = ip; State = Disconnected }

    override _.IsConnected = true

    override _.WriteTags() =
        for tag in tags do
            match tag.GetWriteValue() with
            | Some value ->
                let deviceCode = tag.DeviceCode
                let start = if tag.IsBit then tag.BitOffset else tag.BitOffset / 16
                let values =
                    match value with
                    | :? bool as b -> if b then 1 else 0
                    | :? int16 as i -> int i
                    | :? int as i -> i
                    | _ -> failwith $"지원되지 않는 값 타입: {value.GetType().Name}"
                if tag.IsBit then
                    connection.WriteBit(deviceCode, start, values)
                else
                    connection.WriteWord(deviceCode, start, values)

                tag.ClearWriteValue()
            | None -> ()


    override _.PrepareTags(tagsInput: TagInfo seq) =
        tagMap.Clear()
        let parsed =
            tagsInput
            |> Seq.distinct
            |> Seq.choose (fun tagInfo ->
                match MxTagParser.TryParseToMxTag(tagInfo) with
                | Some tag ->
                    tagMap.[tagInfo.Address] <- tag
                    Some tag
                | None -> None)
            |> Seq.toArray

        tags <- parsed
        highSpeedAreaBatches <- prepareReadBatches (parsed |> Array.filter (fun b ->not  b.IsLowSpeedArea))
        lowSpeedAreaBatches   <- prepareReadBatches (parsed |> Array.filter (fun b ->  b.IsLowSpeedArea))
        upcast tagMap
