namespace XgtProtocol

open System
open Dual.PLC.Common.FS

[<AutoOpen>]
module Batch =


    /// LS (XGT) 전용 배치
    type LWBatch(buffer: byte[], deviceInfos: DeviceInfo[], tags: XGTTag[]) =
        inherit PlcBatchBase<XGTTag>(buffer, deviceInfos, tags)

        override this.LWordAddress =
            if this.Tags.Length > 0 then
                let tag = this.Tags.[0]
                sprintf "%%%sL%d" tag.Device (tag.BitOffset / 64)
            else ""

        override this.BatchToText() =
            this.Tags
            |> Seq.groupBy (fun t -> t.Device)
            |> Seq.map (fun (device, tagGroup) ->
                let maxBitOffset = tagGroup |> Seq.map (fun t -> t.BitOffset) |> Seq.max
                sprintf "Device: %s, Read BitOffset: %d" device maxBitOffset)
            |> String.concat "\n"

    /// 디바이스 정보 생성
    let createDevice (deviceCode: string, offset: int, lWordTag: string) : DeviceInfo =
        DeviceInfo(Device = deviceCode, LWordOffset = offset, LWordTag = lWordTag)

    /// 태그들을 기반으로 배치 생성
    let prepareReadBatches (tagInfos: XGTTag[]) : LWBatch[] =
        let chunkInfos =
            tagInfos
            |> Array.groupBy (fun ti -> ti.LWordTag)
            |> Array.chunkBySize 16

        chunkInfos
        |> Array.map (fun chunk ->
            let allTags = chunk |> Array.collect snd
            let buffer = Array.zeroCreate<byte> (chunk.Length * 8)

            // LWordOffset 지정
            chunk |> Array.iteri (fun n (_, tagsInSameLWord) ->
                tagsInSameLWord |> Array.iter (fun ti ->
                    ti.LWordOffset <- n
                )
            )

            // DeviceInfo 생성
            let devices =
                chunk
                |> Array.collect (fun (_, tis) ->
                    let dev = tis.[0].Device
                    let lwordTag = tis.[0].LWordTag
                    tis
                    |> Array.map (fun ti ->
                        let byteOffset = ti.BitOffset / 8
                        let byteIndex = ti.BitOffset % 64 / 8
                        byteOffset - byteIndex
                    )
                    |> Array.distinct
                    |> Array.map (fun offset -> createDevice(dev, offset, lwordTag))
                )

            LWBatch(buffer, devices, allTags)
        )
