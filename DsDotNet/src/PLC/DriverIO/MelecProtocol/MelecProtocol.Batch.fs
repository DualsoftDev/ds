namespace MelsecProtocol

open System
open System.Collections.Generic
open Dual.PLC.Common.FS

/// MELSEC 전용 배치 - 디지털 DWord 단위 최적화
[<AutoOpen>]
module Batch =

    /// 디지털 DWord 기준으로 구성된 MELSEC 배치
    type DWBatch(buffer: byte[], tags: MelsecTag[]) =
        inherit PlcBatchBase<MelsecTag>(buffer, tags)

        /// 배치 주소: 비트는 K8 접두어 + DWord 주소, 워드는 2Word 정렬된 시작 주소 기준
        override this.BatchAddress =
            match this.Tags with
            | [||] -> ""
            | tags ->
                let head = tags[0]
                if head.IsBit then
                    $"K8{head.DeviceCode}{head.BitOffset / 32}"
                else
                    $"{head.DeviceCode}{(head.BitOffset / 16) * 2}"

        member this.BatchAddressOffset = 
            match MxTagParser.TryParseToMxTag this.BatchAddress with
            | Some tag -> tag.BitOffset / 16
            | None -> failwith $"Invalid BatchAddress format {this.BatchAddress}"

        member this.BatchAddressHead = 
            match MxTagParser.TryParseToMxTag this.BatchAddress with
            | Some tag -> tag.DeviceCode
            | None -> failwith $"Invalid BatchAddress format {this.BatchAddress}"

        /// 디버깅용 출력 문자열
        override this.BatchToText() =
            this.Tags
            |> Seq.groupBy (fun t -> t.DWordTag)
            |> Seq.map (fun (tagKey, group) ->
                let dev = group |> Seq.head |> fun t -> t.DeviceCode
                let maxOffset = group |> Seq.map (fun t -> t.BitOffset) |> Seq.max
                $"DWordTag: {tagKey}, Device: {dev}, Max BitOffset: {maxOffset}")
            |> String.concat "\n"

    /// DWord 단위 최적화된 읽기 배치 구성
    let prepareReadBatches (tags: MelsecTag[]) : DWBatch[] =
        let createBatch groupedTags =
            let tagArray = groupedTags |> Seq.toArray
            let buffer = Array.zeroCreate<byte> 4
            DWBatch(buffer, tagArray)

        tags
        |> Seq.groupBy (fun t -> t.DWordTag)
        |> Seq.map (fun (_, group) -> createBatch group)
        |> Seq.toArray

    /// MELSEC 랜덤 리드 구현 - 배치 기준
    type MxEthernet with
        member this.ReadDWordRandom(batches: DWBatch[]) : byte[] =
            if batches.Length > 192 then failwith "MELSEC MC 프로토콜 제한: 최대 192 항목까지 지원됨"

            let buildCommand () =
                let header = ResizeArray<byte>()
                header.AddRange([| 0x50uy; 0x00uy; 0x00uy; 0xFFuy; 0xFFuy; 0x03uy; 0x00uy |])

                let bodyLength = 2 + batches.Length * 6
                header.AddRange(BitConverter.GetBytes(uint16 (bodyLength + 12)))
                header.AddRange(BitConverter.GetBytes(uint16 0x0010))
                header.AddRange([| 0x03uy; 0x04uy; 0x01uy; 0x00uy |])
                header.AddRange(BitConverter.GetBytes(uint16 batches.Length))

                for batch in batches do
                    let dev = batch.BatchAddressHead.ToString()
                    let addr = batch.BatchAddressOffset
                    let addrBytes = BitConverter.GetBytes(uint32 addr)
                    header.AddRange(addrBytes.[0..2])
                    header.Add(this.GetDeviceCode(dev))
                    header.AddRange(BitConverter.GetBytes(uint16 2)) // 2워드(4바이트) 고정

                header.ToArray()

            let cmd = buildCommand()
            let res = this.SendAndReceive(cmd)
            if res.Length < 11 then failwith "Invalid response"
            res.[11..]