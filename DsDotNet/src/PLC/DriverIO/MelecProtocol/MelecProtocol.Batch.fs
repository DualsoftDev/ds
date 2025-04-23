namespace MelsecProtocol

open System
open System.Collections.Generic
open Dual.PLC.Common.FS

/// MELSEC 전용 배치 - 디지털 DWord 단위 최적화
[<AutoOpen>]
module Batch =
    let [<Literal>] MaxBatchSize = 192  // 구버전 ahn 96 ?????
    /// 디지털 DWord 기준으로 구성된 MELSEC 배치
    type DWBatch(buffer: byte[], tags: MelsecTag[]) =
        inherit PlcBatchBase<MelsecTag>(buffer, tags)

        /// 디버깅용 출력 문자열
        override this.BatchToText() =
            this.Tags
            |> Seq.groupBy (fun t -> t.DWordTag)
            |> Seq.map (fun (tagKey, group) ->
                let dev = group |> Seq.head |> fun t -> t.DeviceCode
                let maxOffset = group |> Seq.map (fun t -> t.BitOffset) |> Seq.max
                $"DWordTag: {tagKey}, Device: {dev}, Max BitOffset: {maxOffset}")
            |> String.concat "\n"

        
    /// 태그들을 기반으로 배치 생성
    let prepareReadBatches (tagInfos: MelsecTag[]) : DWBatch[] =
        tagInfos
        |> Array.groupBy (fun ti -> ti.DWordTag)
        |> Array.chunkBySize MaxBatchSize
        |> Array.map (fun chunk ->
            let allTags = chunk |> Array.collect snd
            let buffer = Array.zeroCreate<byte> (chunk.Length * 4)

            // 각 태그에 DWordOffset 설정
            chunk
            |> Array.iteri (fun i (_, group) ->
                group |> Array.iter (fun tag -> tag.DWordOffset <- i)
            )

            DWBatch(buffer, allTags)
        )

