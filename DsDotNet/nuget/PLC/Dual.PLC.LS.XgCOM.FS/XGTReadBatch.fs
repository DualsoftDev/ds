namespace DsXgComm

open System
open System.Collections.Generic
open System.Reactive.Subjects
open System.Threading
open System.Reactive.Disposables
open Dual.Common.Core.FS
open Connect
open Dual.PLC.TagParser.FS
open XGCommLib

[<AutoOpen>]
module XGTReadBatchModule =

    type LWBatch(buffer: byte[], deviceInfos: DeviceInfo[], tags: XGTTag[]) =
        let mutable tags = tags
        member val Buffer = buffer with get, set
        member val DeviceInfos = deviceInfos with get
        member this.Tags = tags // get

        member this.SetTags(newTags) =
            tags <- newTags // set
        member x.BatchToText() = 
            tags 
            |> Seq.groupBy(fun t -> t.Device) 
            |> Seq.map (fun (device, tagGroup) -> 
                let maxBitOffset = tagGroup |> Seq.map(fun t -> t.BitOffset) |> Seq.max
                sprintf "Device: %s, Read BitOffset: %d" device maxBitOffset
            )
            |> String.concat "\n"

    // Prepare batches for communication with PLC
    let prepareReadBatches(conn: DsXgConnection, tagInfos: XGTTag[]) : LWBatch[] =
        let chunkInfos = tagInfos |> Array.groupBy(fun ti -> ti.LWordTag) |> Array.chunkBySize 64

        chunkInfos 
        |> Array.map (fun ci -> 
            let buffer = Array.zeroCreate<byte> (ci.Length * 8)

            ci |> Array.iteri (fun n (_, tis) -> 
                tis |> Array.iter (fun ti -> 
                    ti.LWordOffset <- n
                )
            )
            let devices = ci |> Array.collect (fun (_, tis) -> 
                let dev = tis.[0].Device
                let offsets = tis |> Array.map (fun ti ->
                                let byteOffset = ti.BitOffset/8
                                let byteIndex = ti.BitOffset % 64 / 8
                                byteOffset-byteIndex
                                ) |> Array.distinct

                offsets |> Array.map(fun offset ->
                        conn.CreateDevice(dev, 'B', 8, offset) //LWord 로 무조건 읽기
                )
            )

            LWBatch(buffer, devices, ci |> Array.collect snd)
        )
