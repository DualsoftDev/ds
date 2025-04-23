namespace XgtProtocol

open System
open Dual.PLC.Common.FS

[<AutoOpen>]
module Batch =

    let [<Literal>] MaxBatchSize = 16
    /// LS (XGT) 전용 배치
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
    let prepareReadBatches (tagInfos: XGTTag[]) : LWBatch[] =
        tagInfos
        |> Array.groupBy (fun ti -> ti.LWordTag)
        |> Array.chunkBySize MaxBatchSize
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
