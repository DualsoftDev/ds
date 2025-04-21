namespace MelsecProtocol

open System
open System.Collections.Generic
open Dual.PLC.Common.FS

type MxPlcScan(ip: string, scanDelay: int, timeoutMs: int, isMonitorOnly: bool) =
    inherit PlcScanBase(ip, scanDelay, isMonitorOnly)

    let connection = new MxEthernet(ip, 5002, timeoutMs)
    let mutable tags: MelsecTag[] = [||]
    let mutable batches: DWBatch[] = [||]
    let notifiedOnce = HashSet<DWBatch>()
    let tagMap = Dictionary<ScanAddress, PlcTagBase>()

    // 연결 상태
    override _.Connect() =
        try
            base.TriggerConnectChanged { Ip = ip; State = Connected }
        with ex ->
            base.TriggerConnectChanged { Ip = ip; State = ConnectFailed }
            failwith $"[❌ 연결 실패] MELSEC ({ip}): {ex.Message}"

    override _.Disconnect() =
        connection.Close()
        base.TriggerConnectChanged { Ip = ip; State = Disconnected }

    override _.IsConnected = true // 실제 연결 상태 체크 필요시 확장 가능

    override _.WriteTags() =
        tags
        |> Array.filter (fun tag -> tag.GetWriteValue().IsSome)
        |> Array.groupBy (fun tag -> tag.DeviceCode)
        |> Array.iter (fun (deviceCode, group) ->
            // 같은 디바이스에서 연속된 BitOffset을 묶어서 한 번에 전송
            group
            |> Array.sortBy (fun t -> t.BitOffset)
            |> Seq.chunkBySize 10 // 연속 쓰기를 위해 최대 10개씩 묶기
            |> Seq.iter (fun chunk ->
                let start = chunk[0].BitOffset
                let values =
                    chunk
                    |> Array.map (fun tag ->
                        match tag.GetWriteValue() with
                        | Some (:? bool as b) -> if b then 1 else 0
                        | Some (:? int16 as i) -> int i
                        | Some (:? int as i) -> i
                        | Some v -> failwith $"지원되지 않는 값 타입: {v.GetType().Name}"
                        | None -> 0)

                connection.WriteWord(deviceCode, start, values)
                chunk |> Array.iter (fun tag -> tag.ClearWriteValue())
            )
        )


    // 태그 읽기
    override _.ReadTags() =
        for batch in batches do
            try
                let deviceCode = batch.Tags[0].DeviceCode
                let offsets = batch.Tags |> Array.map (fun t -> t.BitOffset)
                let start = Array.min offsets
                let stop = Array.max offsets
                let count = (stop - start) / 16 + 1 |> uint16

                let rawWords = connection.ReadWords(deviceCode, start, count)
                let buffer = rawWords |> Array.collect (fun w -> BitConverter.GetBytes(uint16 w))
                Array.Copy(buffer, batch.Buffer, min buffer.Length batch.Buffer.Length)

                for tag in batch.Tags do
                    if tag.UpdateValue(batch.Buffer) then
                        base.TriggerTagChanged { Ip = ip; Tag = tag }

                // 최초 알림 1회
                notifiedOnce.Add(batch) |> ignore

            with ex ->
                eprintfn $"[⚠️ MELSEC Read 실패] {ip}: {ex.Message}"

    // 태그 파싱 및 배치 구성
    override _.PrepareTags(tagsInput: TagInfo seq) =
        tagMap.Clear()

        let isValid = MelsecDevice.IsMelsecAddress

        let parsed =
            tagsInput
            |> Seq.distinct
            |> Seq.choose (fun tagInfo ->
                if isValid tagInfo.Address then
                    let _, offset, _ = MelsecDevice.Parsing(tagInfo.Address)
                    let tag = MelsecTag(tagInfo.Name, tagInfo.Address, PlcDataSizeType.Boolean, offset, tagInfo.Comment)
                    tagMap.[tagInfo.Address] <- tag
                    Some tag
                else
                    printfn $"[⚠️ 파싱 무시됨] {tagInfo.Name} ({tagInfo.Address})"
                    None
            )
            |> Seq.toArray

        tags <- parsed
        batches <- prepareReadBatches parsed
        upcast tagMap
