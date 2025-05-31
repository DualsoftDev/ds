namespace XgtProtocol

open System
open Dual.PLC.Common.FS

[<AutoOpen>]
module Batch =

    let [<Literal>] MaxLWBatchSize = 16
    let [<Literal>] MaxUWBatchSize = 64
    /// LS (XGT) 전용 LocalEthernet 64 bit 배치
    type LWBatch(buffer: byte[], tags: XGTTag[]) =
        inherit PlcBatchBase<XGTTag>(buffer, tags)

        override this.BatchToText() =
            this.Tags
            |> Seq.groupBy (fun t -> t.Device)
            |> Seq.map (fun (device, tagGroup) ->
                let maxBitOffset = tagGroup |> Seq.map (fun t -> t.BitOffset) |> Seq.max
                sprintf "Device: %s, Max BitOffset: %d" device maxBitOffset)
            |> String.concat "\n"

    

    /// 태그들을 기반으로 배치 생성
    let prepareRead64Batches (tagInfos: XGTTag[]) : LWBatch[] =
        tagInfos
        |> Array.groupBy (fun ti -> ti.LWordTag)
        |> Array.chunkBySize MaxLWBatchSize
        |> Array.map (fun chunk ->
            let allTags = chunk |> Array.collect snd
            let buffer = Array.zeroCreate<byte> (chunk.Length * 8)

            // 각 태그에 LWordOffset 설정
            chunk
            |> Array.iteri (fun i (_, group) ->
                group |> Array.iter (fun tag -> tag.LWordOffset <- i)
            )

            LWBatch(buffer, allTags)
        )

    /// LS (XGT) 전용 EFMTB 128 bit 배치 (QW  = LW * 2)
    type QWBatch(buffer: byte[], tags: XGTTag[]) =
        inherit PlcBatchBase<XGTTag>(buffer, tags)

        override this.BatchToText() =
            this.Tags
            |> Seq.groupBy (fun t -> t.Device)
            |> Seq.map (fun (device, tagGroup) ->
                let maxBitOffset = tagGroup |> Seq.map (fun t -> t.BitOffset) |> Seq.max
                sprintf "Device: %s, Max BitOffset: %d" device maxBitOffset)
            |> String.concat "\n"

    let prepareRead128Batches (tagInfos: XGTTag[]) : QWBatch[] =
        tagInfos
        |> Array.groupBy (fun ti -> ti.QWordTag)
        |> Array.chunkBySize MaxUWBatchSize 
        |> Array.map (fun chunk ->
            let allTags = chunk |> Array.collect snd
            let buffer = Array.zeroCreate<byte> (chunk.Length * 16)

            chunk
            |> Array.iteri (fun i (_, group) ->
                group |> Array.iter (fun tag -> tag.QWordOffset <- i)
            )

            QWBatch(buffer, allTags)
        )