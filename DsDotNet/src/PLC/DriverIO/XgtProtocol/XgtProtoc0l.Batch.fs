namespace XgtProtocol

open System
open System.Collections.Generic

[<AutoOpen>]
module Batch =

    type DeviceInfo() =
        member val Device = "" with get, set
        member val LWordOffset = 0 with get, set
        member val LWordTag =  ""  with get, set

        

    type LWBatch(buffer: byte[], deviceInfos: DeviceInfo[], tags: XGTTag[]) =
        let mutable tags = tags
        member val Buffer =  buffer with get, set
        member val DeviceInfos = deviceInfos with get
        member this.Tags = tags
        member this.LWordAddress =
            if tags.Length > 0 then
                let tag = tags.[0]
                sprintf "%%%sL%d" tag.Device (tag.BitOffset / 64)
            else ""
        member this.SetTags(newTags) = tags <- newTags
        member this.BatchToText() =
            tags
            |> Seq.groupBy (fun t -> t.Device)
            |> Seq.map (fun (device, tagGroup) ->
                let maxBitOffset = tagGroup |> Seq.map (fun t -> t.BitOffset) |> Seq.max
                sprintf "Device: %s, Read BitOffset: %d" device maxBitOffset)
            |> String.concat "\n"

    let createDevice(deviceCode: string, offset: int, lWordTag:string) : DeviceInfo =
        let dev = new DeviceInfo()
        dev.Device <- deviceCode
        dev.LWordOffset <- offset
        dev.LWordTag <- lWordTag
        dev

    let prepareReadBatches (tagInfos: XGTTag[]) : LWBatch[] =
        let chunkInfos =
            tagInfos
            |> Array.groupBy (fun ti -> ti.LWordTag)
            |> Array.chunkBySize 16

        chunkInfos
        |> Array.map (fun chunk ->
            let allTags = chunk |> Array.collect snd
            let buffer = Array.zeroCreate<byte> (chunk.Length * 8)

            chunk |> Array.iteri (fun n (_, tagsInSameLWord) ->
                tagsInSameLWord |> Array.iter (fun ti ->
                    ti.LWordOffset <- n
                )
            )

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