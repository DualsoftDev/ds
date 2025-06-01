namespace XgtProtocol

open System
open Dual.PLC.Common.FS

[<AutoOpen>]
module Batch =

    let [<Literal>] MaxLWBatchSize = 16
    let [<Literal>] MaxUWBatchSize = 64

    type LWBatch(buffer: byte[], tags: XGTTag[]) =
        inherit PlcBatchBase<XGTTag>(buffer, tags)
        override this.BatchToText() =
            this.Tags
            |> Seq.groupBy (fun t -> t.Device)
            |> Seq.map (fun (device, tagGroup) ->
                let maxBitOffset = tagGroup |> Seq.map (fun t -> t.BitOffset) |> Seq.max
                sprintf "Device: %s, Max BitOffset: %d" device maxBitOffset)
            |> String.concat "\n"

    let prepareRead64Batches (tagInfos: XGTTag[]) : LWBatch[] =
        tagInfos
        |> Array.groupBy (fun ti -> ti.LWordTag)
        |> Array.chunkBySize MaxLWBatchSize
        |> Array.map (fun chunk ->
            let allTags = chunk |> Array.collect snd
            let buffer = Array.zeroCreate<byte> (chunk.Length * 8)
            chunk |> Array.iteri (fun i (_, group) -> group |> Array.iter (fun tag -> tag.LWordOffset <- i))
            LWBatch(buffer, allTags))

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
            chunk |> Array.iteri (fun i (_, group) -> group |> Array.iter (fun tag -> tag.QWordOffset <- i))
            QWBatch(buffer, allTags))
