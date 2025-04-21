namespace MelsecProtocol

open System
open Dual.PLC.Common.FS
open System.Collections.Generic

[<AutoOpen>]
module Batch =

    /// MELSEC 전용 배치 - 디지털 워드 단위
    type DWBatch(buffer: byte[], tags: MelsecTag[]) =
        inherit PlcBatchBase<MelsecTag>(buffer, tags)

        /// 배치 주소: 첫 번째 태그 기준으로 생성
        override this.BatchAddress =
            match this.Tags with
            | [||] -> ""
            | tags ->
                let head = tags.[0]
                $"{head.DeviceCode}{head.BitOffset / 64}"

        /// 디버깅용 문자열 출력
        override this.BatchToText() =
            this.Tags
            |> Seq.groupBy (fun t -> t.DeviceCode)
            |> Seq.map (fun (dev, group) ->
                let maxOffset = group |> Seq.map (fun t -> t.BitOffset) |> Seq.max
                $"Device: {dev}, Max BitOffset: {maxOffset}"
            )
            |> String.concat "\n"



    let prepareReadBatches (tags: MelsecTag[]) : DWBatch[] =
        tags
        |> Seq.groupBy (fun t -> t.LWordTag)
        |> Seq.map (fun (_lword, group) ->
            let tagArray = group |> Seq.toArray
            let bufferSize = 8 * 1  // 64비트 = 8바이트, LWord 하나 기준
            let buffer = Array.zeroCreate<byte> bufferSize
            DWBatch(buffer, tagArray)
        )
        |> Seq.toArray
