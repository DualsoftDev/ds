namespace MelsecProtocol

open System
open System.Collections.Generic
open Dual.PLC.Common.FS

module MelsecProtocol  = ()





//type MxPlcScan(ip: string, scanDelay: int, timeoutMs: int) = 
    //inherit PlcScanBase(ip, scanDelay)

    //let mutable mxComm: MxEthernet option = None
    //let groupTags: Dictionary<string, List<TagInfo>> = Dictionary()

    //override this.Connect() =
    //    mxComm <- Some(MxEthernet(ip, 5000, timeoutMs))
    //    match mxComm.Value.Connect() with
    //    | true ->
    //        base.TriggerConnectChanged { Ip = ip; State = Connected }
    //    | false ->
    //        base.TriggerConnectChanged { Ip = ip; State = ConnectFailed }
    //        failwith $"[MxPlcScan] 연결 실패: {ip}"

    //override this.Disconnect() =
    //    match mxComm with
    //    | Some m -> m.Close()
    //    | None -> ()
    //    mxComm <- None

    //override this.ScanOnce() =
    //    match mxComm with
    //    | None ->
    //        this.LogError("[MxPlcScan] 통신 객체가 초기화되지 않음")
    //    | Some m ->
    //        for kvp in groupTags do
    //            let device = kvp.Key
    //            let tags = kvp.Value
    //            let sorted: List<TagInfo> = tags |> List.sortBy (fun t -> t.Address)
    //            let startAddr = sorted.Head.Address
    //            let endAddr = sorted |> List.last |> fun t -> t.Address
    //            let count = endAddr - startAddr + 1

    //            try
    //                let values = m.ReadWord(device, startAddr, count)
    //                for tag in tags do
    //                    let index = tag.Address - startAddr
    //                    if index >= 0 && index < values.Length then
    //                        this.UpdateTagValue(tag.Name, values.[index])
    //            with ex ->
    //                this.LogError($"[MxPlcScan] {device}{startAddr}~ 읽기 오류: {ex.Message}")

    //override this.OnAddTag(tag: TagInfo) =
    //    let device = tag.Device
    //    if not (groupTags.ContainsKey(device)) then
    //        groupTags.Add(device, List<TagInfo>())
    //    groupTags.[device].Add(tag)

    //override this.OnRemoveTag(tag: TagInfo) =
    //    let device = tag.Device
    //    if groupTags.ContainsKey(device) then
    //        groupTags.[device].RemoveAll(fun t -> t.Name = tag.Name) |> ignore
