namespace MelsecProtocol

open System
open System.Collections.Generic
open Dual.PLC.Common.FS

/// MELSEC PLC 스캔 구현 (DWord 랜덤 읽기 최적화 기반)
type MxPlcScan(ip: string, scanDelay: int, timeoutMs: int, isMonitorOnly: bool) =
    inherit PlcScanBase(ip, scanDelay, isMonitorOnly)

    let connection = new MxEthernet(ip, 5000, timeoutMs, true)
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

    // ---------------------------
    // 태그 쓰기
    // ---------------------------
    override _.WriteTags() =
        for tag in tags do
            match tag.GetWriteValue() with
            | Some value ->
                let deviceCode = tag.DeviceCode
                let start = if tag.IsBit then tag.BitOffset else tag.BitOffset / 16
                let values =
                    match value with
                    | :? bool as b ->  if b then 1 else 0 
                    | :? int16 as i -> int i 
                    | :? int as i ->  i 
                    | _ -> failwith $"지원되지 않는 값 타입: {value.GetType().Name}"
                if tag.IsBit 
                then 
                    connection.WriteBit(deviceCode, start, values)
                    //let a = connection.ReadBits(deviceCode, start, 1)
                    //Console.WriteLine($" [PLC  {tag.Name} ({tag.Address}) = {tag.Value} ({a[0]})");
                else
                    connection.WriteWord(deviceCode, start, values)
                    //let a = connection.ReadWords(deviceCode, start, 1)
                    //Console.WriteLine($" [PLC  {tag.Name} ({tag.Address}) = {tag.Value} ({a[0]})");

                tag.ClearWriteValue()
            | None -> ()

    // ---------------------------
    // 태그 랜덤 쓰기 (구현필요)
    // ---------------------------
    //override _.WriteTags() =
    //    let writeItems =
    //        tags
    //        |> Seq.filter (fun tag ->  tag.GetWriteValue().IsSome)
                
    //    let wordItems =
    //        writeItems
    //        |> Seq.filter (fun tag ->not tag.IsBit)
    //        |> Seq.map (fun tag ->
    //            let value =  tag.GetWriteValue() .Value
    //            let deviceCode = tag.DeviceCode
    //            let start = tag.BitOffset / 16
    //            let intValue =
    //                match value with
    //                | :? int16 as i -> int i
    //                | :? int as i -> i
    //                | _ -> failwith $"Unsupported value type: {value.GetType().Name}"
    //            (deviceCode, start, intValue))
    //        |> Seq.toArray

    //    if wordItems.Length > 0 then
    //        connection.WriteWordRandom(wordItems)

    //    let bitItems =
    //        writeItems
    //        |> Seq.filter (fun tag -> tag.IsBit)
    //        |> Seq.map (fun tag ->
    //                let value =  tag.GetWriteValue().Value
    //                let deviceCode = tag.DeviceCode
    //                let start = tag.BitOffset 
    //                let intValue =
    //                    match value with
    //                    | :? bool as b ->  if b then 1 else 0 
    //                    | _ -> failwith $"Unsupported value type: {value.GetType().Name}"
    //                (deviceCode, start, intValue))
    //        |> Seq.toArray

    //    if bitItems.Length > 0 then
    //        connection.WriteBitRandom(bitItems)

    //    // Clear write values
    //    for tag in writeItems do
    //        tag.ClearWriteValue()

    override _.ReadTags(delayMs:int) =
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

                Async.Sleep(delayMs) |> Async.RunSynchronously  

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
