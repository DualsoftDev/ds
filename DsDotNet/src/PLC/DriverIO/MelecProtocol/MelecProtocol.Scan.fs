namespace MelsecProtocol

open System
open System.Collections.Generic
open Dual.PLC.Common.FS

/// MELSEC PLC 스캔 구현 (DWord 랜덤 읽기 최적화 기반)
type MxPlcScan(ip: string, scanDelay: int, timeoutMs: int, isMonitorOnly: bool) =
    inherit PlcScanBase(ip, scanDelay, isMonitorOnly)

    let connection = new MxEthernet(ip, 5002, timeoutMs)
    let mutable tags: MelsecTag[] = [||]
    let mutable batches: DWBatch[] = [||]
    let notifiedOnce = HashSet<DWBatch>()
    let tagMap = Dictionary<ScanAddress, PlcTagBase>()

    override _.Connect() =
        try
            base.TriggerConnectChanged { Ip = ip; State = Connected }
        with ex ->
            base.TriggerConnectChanged { Ip = ip; State = ConnectFailed }
            failwith $"[❌ 연결 실패] MELSEC ({ip}): {ex.Message}"

    override _.Disconnect() =
        connection.Close()
        base.TriggerConnectChanged { Ip = ip; State = Disconnected }

    override _.IsConnected = true

    override _.WriteTags() =
        tags
        |> Array.filter (fun tag -> tag.GetWriteValue().IsSome)
        |> Array.groupBy (fun tag -> tag.DeviceCode.ToString())
        |> Array.iter (fun (deviceCode, group) ->
            group
            |> Array.sortBy (fun t -> t.BitOffset)
            |> Seq.chunkBySize 10
            |> Seq.iter (fun chunk ->
                let isBit = chunk[0].IsBit 
                let start = if isBit then chunk[0].BitOffset else chunk[0].BitOffset / 16

                let values =
                    chunk
                    |> Array.map (fun tag ->
                        match tag.GetWriteValue() with
                        | Some (:? bool as b) -> if b then 1 else 0
                        | Some (:? int16 as i) -> int i
                        | Some (:? int as i) -> i
                        | Some v -> failwith $"지원되지 않는 값 타입: {v.GetType().Name}"
                        | None -> 0)

                if isBit then
                    connection.WriteBits(deviceCode, start, values)
                else
                    connection.WriteWord(deviceCode, start, values)

                chunk |> Array.iter (fun tag -> tag.ClearWriteValue())
            )
        )



    override _.ReadTags() =
        try
            for batch in batches do
               
                batch.Buffer <- connection.ReadDWordRandom(batch)

                // 태그별 값 업데이트 및 변경 이벤트 발생
                for tag in batch.Tags do
                    if tag.UpdateValue(batch.Buffer) then
                        base.TriggerTagChanged { Ip = ip; Tag = tag }

                // 최초 읽기 알림만 등록
                if notifiedOnce.Add(batch) then
                    () // 추후 알림 로직 확장 여지

        with ex ->
            eprintfn "[⚠️ MELSEC Read 실패] %s: %s" ip ex.Message

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
        batches <- prepareReadBatches parsed
        upcast tagMap
