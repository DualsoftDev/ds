namespace DsMxComm

open System
open System.Collections.Generic
open System.Reactive.Subjects
open System.Threading
open System.Reactive.Disposables
open Dual.Common.Core.FS

[<AutoOpen>]
module MelsecReadBatchModule =
// WordBatch 클래스 및 PLC 배치 처리 모듈
    type WordBatch(buffer: byte[], tags: MxTag[]) =
        let mutable tags = tags
        member val Buffer = buffer with get, set
        member this.Tags = tags
        member this.SetTags(newTags) = tags <- newTags



    // Prepare batches for communication with PLC
    let prepareReadBatches(conn: DsMxConnection, tagInfos: MxTag[]) : WordBatch[] =
        let chunkInfos = tagInfos |> Array.groupBy(fun ti -> ti.WordTag) |> Array.chunkBySize 16
        chunkInfos 
        |> Array.map (fun ci -> 
            let buffer = Array.zeroCreate<byte> (ci.Length * 8)
            ci |> Array.iteri (fun n (_, tis) -> 
                tis |> Array.iter (fun ti -> ti.WordOffset <- n)
            )
            let devices = ci |> Array.collect (fun (_, tis) -> 
                let dev = tis.[0].Device
                let offsets = tis |> Array.map (fun ti ->
                    let byteOffset = ti.BitOffset / 8
                    let byteIndex = ti.BitOffset % 64 / 8
                    byteOffset - byteIndex
                ) |> Array.distinct
                offsets |> Array.map(fun offset ->
                    conn.CreateDevice(dev, 'B', 8, offset)
                )
            )
            WordBatch(buffer, devices, ci |> Array.collect snd)
        )