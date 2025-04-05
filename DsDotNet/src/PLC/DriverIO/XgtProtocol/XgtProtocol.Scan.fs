namespace XgtProtocol

open System
open System.Collections.Generic
open Dual.PLC.Common.FS

type XgtPlcScan(ip: string, scanDelay: int) =
    inherit PlcScanBase(ip, scanDelay)

    let connection = XgtEthernet(ip, 2004)
    let mutable xgtTags: XGTTag[] = [||]
    let mutable batches: LWBatch[] = [||]
    let notifiedOnce = HashSet<LWBatch>()
    let tagMap = Dictionary<string, IPlcTagReadWrite>()

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
                if not (connection.WriteData(tag.Address, tag.DataType, value)) then
                    failwith $"WriteData 실패: {tag.Address}"
                tag.ClearWriteValue()
            | None -> ()

    // ---------------------------
    // 태그 읽기
    // ---------------------------
    override _.ReadTags() =
        for batch in batches do
            try
                connection.ReadData(
                    batch.DeviceInfos |> Seq.map (fun d -> d.LWordTag) |> Seq.toArray,
                    PlcDataSizeType.UInt64,
                    batch.Buffer
                )

                for tag in batch.Tags do
                    if tag.UpdateValue(batch.Buffer) then
                        base.TriggerTagChanged { Ip = ip; Tag = tag }

            with ex ->
                printfn "[XGT Read Error] %s: %s" ip ex.Message
                connection.ReConnect() |> ignore

            if not (notifiedOnce.Contains(batch)) then
                notifiedOnce.Add(batch) |> ignore

    // ---------------------------
    // 태그 준비 및 파싱
    // ---------------------------
    override _.PrepareTags(tags: string seq) : IDictionary<string, IPlcTagReadWrite> =
        tagMap.Clear()
        let isXGI = LsXgiTagParser.IsXGI(tags)

        let parsedTags =
            tags
            |> Seq.distinct
            |> Seq.choose (fun tagStr ->
                match if isXGI then tryParseXgiTag tagStr else tryParseXgkTag tagStr with
                | Some (_dev, size, offset) ->
                    let tag = XGTTag(tagStr, size, offset)
                    tagMap.[tagStr] <- tag :> IPlcTagReadWrite
                    Some tag
                | None ->
                    printfn "[⚠️ 태그 무시됨] 파싱 실패: %s" tagStr
                    None
            )
            |> Seq.toArray

        xgtTags <- parsedTags
        batches <- prepareReadBatches parsedTags
        tagMap :> IDictionary<string, IPlcTagReadWrite>
