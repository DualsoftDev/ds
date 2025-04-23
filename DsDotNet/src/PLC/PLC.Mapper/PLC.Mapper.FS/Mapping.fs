namespace PLC.Mapper.FS

open System
open System.Drawing
open System.Collections.Concurrent
open System.Text.RegularExpressions
open ColorUtilModule
open Engine.Core.MapperDataModule
open PrefixTrie
open System.Collections.Generic
open MappingDeviceModule
[<AutoOpen>]
module MappingModule =

    let extractGroupDeviceApis (mapperTags: MapperTag[]) (targetGroupCount: int) (usingPouGroup:bool) (userPairs: seq<string * MapperTag[]>) : DeviceApi[] =
        if targetGroupCount = 0 then failwith "targetGroupCount 0 입니다."
        if mapperTags.Length = 0 then failwith "이름 리스트가 비어있습니다."

        let userGroupApis = MappingApiModule.createUserGroupApis mapperTags userPairs usingPouGroup

        let cache = ConcurrentDictionary<int, (string * MapperTag[])[]>()
        let _dicMapperTag = Dictionary<string, MapperTag>()
        let getUniqDevName (device: string, tag: MapperTag) =
            if _dicMapperTag.ContainsKey(tag.Name) then
                $"{device}({tag.Address})"
            else
                _dicMapperTag.Add(tag.Name, tag)
                device

        let userGroupKeys = userGroupApis |> Array.map (fun d -> d.MapperTag.OpcName) |> Set.ofArray
        let remainingTags = mapperTags |> Array.filter (fun tag -> not (userGroupKeys.Contains tag.OpcName))

        let bestGroups =
            if usingPouGroup then
                mapperTags
                |> Array.filter (fun tag -> not (userGroupKeys.Contains tag.OpcName))
                |> Array.filter (fun tag -> tag.UsedPous.Count > 0)
                |> Array.groupBy (fun tag -> tag.UsedPous.[0])
            else
                let maxLen = remainingTags |> Array.maxBy (fun t -> t.Name.Length) |> fun t -> t.Name.Length
                [|1 .. maxLen|]
                |> Array.Parallel.choose (fun len ->
                    let result = cache.GetOrAdd(len, fun l -> groupByPrefixLength remainingTags l)
                    if result.Length <= targetGroupCount then Some result else None)
                |> fun arr ->
                    if arr.Length = 0 then [| ("ALL", remainingTags) |]
                    else arr |> Array.minBy (fun g -> abs (g.Length - targetGroupCount))

        let autoGroupApis =
            bestGroups
            |> Array.sortBy fst
            |> Array.mapi (fun idx (grp, tags) ->
                let hue = float ((idx + Seq.length userPairs) * 360 / targetGroupCount)
                let color = (hsvToColor hue 0.7 0.9).ToArgb()
                let tagList = tags |> Array.toList
                let deviceCandidates = extractDevicePrefixes (tagList |> List.map (fun t -> t.Name))
                let prefixTrie = deviceCandidates |> Array.map fst |> buildPrefixTrie
                let deviceNames = tags |> Array.map (fun tag -> tryFindLongestPrefix prefixTrie tag.Name |> Option.defaultValue tag.Name)
                let groupName = if usingPouGroup then grp else findGroupName deviceNames

                tags |> Array.map (fun tag ->
                    let deviceFull = tryFindLongestPrefix prefixTrie tag.Name |> Option.defaultValue tag.Name
                    let api = extractApiFromTag tag deviceFull
                    let device =
                        if groupName = NonGroup || usingPouGroup then deviceFull
                        elif deviceFull.Length > groupName.Length then deviceFull.Substring(groupName.Length)
                        else failwithf "error: device=%s, groupName=%s, tag=%s" deviceFull groupName tag.Name

                    let work =
                        let devSplit = device.Split(SegmentSplit, StringSplitOptions.RemoveEmptyEntries)
                        if groupName = NonGroup && devSplit.Length > 1 then devSplit.[1] else devSplit.[0]

                    let dev = getUniqDevName (device, tag)
                    if validName work = "" then failwithf "createDeviceApis 오류: tag=%A" tag

                    MappingApiModule.createDeviceApi groupName dev work api tag color usingPouGroup))
            |> Array.concat

        Array.append userGroupApis autoGroupApis