namespace PLC.Mapper.FS

open System
open System.Drawing
open System.Collections.Concurrent
open System.Text.RegularExpressions
open ColorUtilModule
open Engine.Core.MapperDataModule
open PrefixTrie
open System.Collections.Generic

[<AutoOpen>]
module MappingDeviceModule =


    // ========== 유틸리티 함수 ==========

    let groupByPrefixLength (tags: MapperTag[]) (prefixLen: int) : (string * MapperTag[])[] =
        tags
        |> Array.groupBy (fun t -> if t.Name.Length >= prefixLen then t.Name.Substring(0, prefixLen) else t.Name)

    let findCommonPrefix (items: string list) : string =
        match items with
        | [] -> ""
        | first :: rest ->
            rest |> List.fold (fun acc s ->
                let minLen = min acc.Length s.Length
                let mutable i = 0
                while i < minLen && acc.[i] = s.[i] do i <- i + 1
                acc.Substring(0, i)
            ) first

    let findSafeGroupName (tags: string[]) : string =
        let items = tags |> Array.map (fun t -> t.Trim(SegmentSplit)) |> Array.toList
        let rawPrefix = findCommonPrefix items
        let trimmedPrefix = rawPrefix.TrimEnd(SegmentSplit)
        let hasExactMatch = items |> List.exists ((=) trimmedPrefix)
        let groupName =
            if hasExactMatch then
                let lastIndex =
                    SegmentSplit
                    |> Array.map (fun sep -> trimmedPrefix.LastIndexOf(sep))
                    |> Array.max
                if lastIndex > 0 then trimmedPrefix.Substring(0, lastIndex) else NonGroup
            else trimmedPrefix
        if groupName = "" then NonGroup else groupName

    let rec splitDevicesRecursively (prefix: string) (tags: string list) : (string * string list) list =
        if tags.Length <= 2 then [ (findCommonPrefix tags, tags) ]
        else
            tags
            |> List.groupBy (fun t -> if t.Length > prefix.Length then t.[prefix.Length].ToString() else "")
            |> List.filter (fun (_, lst) ->
                lst |> List.map (fun t -> if t.Length = prefix.Length then t else t.Substring(prefix.Length + 1))
                    |> List.distinct
                    |> fun apis -> apis.Length >= 2)
            |> function
                | [] -> [ (prefix, tags) ]
                | groups ->
                    groups
                    |> List.collect (fun (suffixChar, subTags) ->
                        splitDevicesRecursively (prefix + suffixChar) subTags)

    let extractDevicePrefixes (tags: string list) : (string * string[])[] =
        splitDevicesRecursively "" tags
        |> List.sortByDescending (fun (prefix, lst) -> prefix.Length, lst.Length)
        |> List.map (fun (p, l) -> p, List.toArray l)
        |> Array.ofList

    let extractApiFromTag (tag: MapperTag) (device: string) : string =
        if tag.Name.StartsWith(device) && tag.Name.Length <> device.Length then
            tag.Name.Substring(device.Length)
        elif tag.Name = device then "DO"
        else failwithf "extractApiFromTag tag %s device %s" tag.Name device

    let private extractDeviceAndApiFromUserGroup (tags: MapperTag[]) (groupName: string) : string * (MapperTag * string)[] =
        let prefix = findCommonPrefix (tags |> Array.map (fun t -> t.Name) |> Array.toList)
        let trimmedPrefix = prefix.Substring(groupName.Length).TrimEnd(SegmentSplit)
        let device = trimmedPrefix
        let deviceApiList =
            tags |> Array.map (fun tag ->
                let suffix = tag.Name.Substring(device.Length).TrimStart(SegmentSplit)
                let api =
                    if String.IsNullOrWhiteSpace(suffix) then "DO"
                    else String.concat "_" (suffix.Split(SegmentSplit, StringSplitOptions.RemoveEmptyEntries))
                tag, api)
        device, deviceApiList

    let private createDeviceApis (groupName: string) (device: string) (deviceApiList: (MapperTag * string)[]) (color: int) : DeviceApi[] =
        let work =
            let devSplit = device.Split(SegmentSplit, StringSplitOptions.RemoveEmptyEntries)
            if groupName = NonGroup && devSplit.Length > 1 then devSplit[1] else devSplit[0]
        deviceApiList
        |> Array.map (fun (tag, api) ->
            if groupName = "" || work = "" || device = "" || api = "" then
                failwithf "createDeviceApis 오류: tag=%A" tag
            DeviceApi(
                Area = validName groupName,
                Work = validName work,
                Device = validName device,
                Api = validName api,
                MapperTag = tag,
                OutAddress = "",
                InAddress = "",
                Color = color
            ))

    let extractGroupDeviceApis (mapperTags: MapperTag[]) (targetGroupCount: int) (usingPouGroup:bool) (userPairs: seq<string * MapperTag[]>) : DeviceApi[] =
        if targetGroupCount = 0 then failwith "targetGroupCount 0 입니다."
        if mapperTags.Length = 0 then failwith "이름 리스트가 비어있습니다."

        let mapperSet = mapperTags |> Array.map (fun tag -> tag.OpcName) |> Set.ofArray

        let userGroupApis =
            userPairs
            |> Seq.indexed
            |> Seq.map (fun (idx, (groupName, tags)) ->
                let filtered = tags |> Array.filter (fun t -> mapperSet.Contains t.OpcName)
                if filtered.Length < 2 then [||]
                else
                    let device, deviceApiList = extractDeviceAndApiFromUserGroup filtered groupName
                    let color = (hsvToColor (float (idx * 360 / targetGroupCount)) 0.7 0.9).ToArgb()
                    createDeviceApis groupName device deviceApiList color)
            |> Seq.concat
            |> Seq.toArray

        let cache = ConcurrentDictionary<int, (string * MapperTag[])[]>()
        let _dicMapperTag = Dictionary<string, MapperTag>()
        let getUniqDevName (device: string, tag: MapperTag) =
            if _dicMapperTag.ContainsKey(tag.Name) then
                $"{device}({tag.Address})"
            else
                _dicMapperTag.Add(tag.Name, tag)
                device








        let userGroupKeys = userGroupApis |> Array.map (fun d -> d.MapperTag.OpcName) |> Set.ofArray
        let remainingTags = mapperTags
                            |> Array.filter (fun tag -> 
                                not (userGroupKeys.Contains tag.OpcName)
                                || (usingPouGroup && tag.UsedPous.Count = 0)
                            )
                              
        let maxLen = remainingTags |> Array.maxBy (fun t -> t.Name.Length) |> fun t -> t.Name.Length
        let bestGroups =
            [|1 .. maxLen|]
            |> Array.Parallel.choose (fun len ->
                let result = cache.GetOrAdd(len, fun l -> groupByPrefixLength remainingTags l)
                if result.Length <= targetGroupCount then Some result else None)
            |> fun arr ->
                if arr.Length = 0 then [| ("ALL", remainingTags) |]
                else arr |> Array.minBy (fun g -> abs (g.Length - targetGroupCount))

        let groupByPou =
            mapperTags |> Array.except remainingTags 
            |> Array.groupBy (fun tag -> tag.UsedPous[0])     
            

        let autoGroupApis =
            bestGroups 
            |> Array.append groupByPou
            |> Array.sortBy fst
            |> Array.mapi (fun idx (_grp, tags) ->
                let hue = float ((idx + Seq.length userPairs) * 360 / targetGroupCount)
                let color = (hsvToColor hue 0.7 0.9).ToArgb()
                let tagList = tags |> Array.toList
                let deviceCandidates = extractDevicePrefixes (tagList |> List.map (fun t -> t.Name))
                let prefixTrie = deviceCandidates |> Array.map fst |> buildPrefixTrie
                let deviceNames = tags |> Array.map (fun tag -> tryFindLongestPrefix prefixTrie tag.Name |> Option.defaultValue tag.Name)
                let groupName = findSafeGroupName deviceNames
                tags |> Array.map (fun tag ->
                    let deviceFull = tryFindLongestPrefix prefixTrie tag.Name |> Option.defaultValue tag.Name
                    let api = extractApiFromTag tag deviceFull
                    let device =
                        if groupName = NonGroup then deviceFull
                        elif deviceFull.Length > groupName.Length then deviceFull.Substring(groupName.Length)
                        else failwithf "error: device=%s, groupName=%s, tag=%s" deviceFull groupName tag.Name
                    let work =
                        let devSplit = device.Split(SegmentSplit, StringSplitOptions.RemoveEmptyEntries)
                        if groupName = NonGroup && devSplit.Length > 1 then devSplit[1] else devSplit[0]

                    let dev = getUniqDevName (device, tag) 

                    DeviceApi(
                        Area = validName groupName,
                        Work = validName work,
                        Device = validName dev,
                        Api = validName api,
                        MapperTag = tag,
                        OutAddress = "",
                        InAddress = "",
                        Color = color
                    )))
            |> Array.concat

        Array.append userGroupApis autoGroupApis
