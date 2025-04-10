namespace PLC.Mapper.FS

open System
open System.Drawing
open System.Collections.Concurrent
open System.Text.RegularExpressions
open ColorUtilModule
open Engine.Core.MapperDataModule
open PrefixTrie

[<AutoOpen>]
module MappingDeviceModule =


    let groupByPrefixLength (names: string array) (prefixLen: int) : (string * string[])[] =
        names
        |> Array.groupBy (fun n -> if n.Length >= prefixLen then n.Substring(0, prefixLen) else n)

    let findCommonPrefix (items: string list) : string =
        match items with
        | [] -> ""
        | first :: rest ->
            rest
            |> List.fold (fun acc s ->
                let minLen = min acc.Length s.Length
                let mutable i = 0
                while i < minLen && acc.[i] = s.[i] do i <- i + 1
                acc.Substring(0, i)
            ) first

    let findSafeGroupName (items: string list) : string =
        let items = items |> Seq.map(fun f->f.Trim(SegmentSplit)) |> Seq.toList
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

        if groupName = "" then
            NonGroup
        else
            groupName

    let rec splitDevicesRecursively (prefix: string) (tags: string list) : (string * string list) list =
        if tags.Length <= 2 then
            [ (findCommonPrefix tags, tags) ]
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

    let extractApiFromTag (tag: string) (device: string) : string =
        if tag.StartsWith(device) && tag.Length <> device.Length then
            tag.Substring(device.Length)
        elif tag = device
        then
            "DO"
        else
            failwith $"extractApiFromTag tag {tag} device {device} err"

    let private extractDeviceAndApiFromUserGroup (tags: string[]) : string * (string * string)[] =
        let prefix = findCommonPrefix (tags |> Array.toList)
        let trimmedPrefix = prefix.TrimEnd(SegmentSplit)
        let device = trimmedPrefix

        let deviceApiList =
            tags
            |> Array.map (fun tag ->
                let suffix = tag.Substring(device.Length).TrimStart(SegmentSplit)
                let api =
                    if String.IsNullOrWhiteSpace(suffix) then "ON"
                    else String.concat "_" (suffix.Split(SegmentSplit, System.StringSplitOptions.RemoveEmptyEntries))
                tag, api)

        device, deviceApiList

    let private createDeviceApis (groupName: string) (device: string) (deviceApiList: (string * string)[]) (color: int) : DeviceApi[] =
        let work =
            let devSplit = device.Split(SegmentSplit, StringSplitOptions.RemoveEmptyEntries)
            if groupName = NonGroup && devSplit.Length > 1 then devSplit[1] else devSplit[0]

        deviceApiList
        |> Array.map (fun (tag, api) ->
            if groupName = "" || work = "" || device = "" || api = "" then
                failwith $"createDeviceApis 오류: tag={tag}"

            DeviceApi(
                Area = validName groupName,
                Work = validName work,
                Device = validName device,
                Api = validName api,
                Tag = tag,
                OutAddress = "",
                InAddress = "",
                Color = color
            )
        )
        //userPairs (groupName * tags[] )seq
    let extractGroupDeviceApis (names: string array) (targetGroupCount: int) (userPairs : seq<string * string[]>) : DeviceApi [] =
        if targetGroupCount = 0 then failwith "targetGroupCount 0 입니다."
        if names.Length = 0 then failwith "이름 리스트가 비어있습니다."

        let nameSet = Set.ofArray names

        // 1. userPairs 처리 (그룹 이름 명시된 사용자 그룹 우선 처리)
        let userGroupApis =
            userPairs
            |> Seq.indexed // 색상 분배를 위해 인덱스 부여
            |> Seq.map (fun (idx, (groupName, tags)) ->
                let filtered = tags |> Array.filter nameSet.Contains
                if filtered.Length < 2 then [||]
                else
                    let device, deviceApiList = extractDeviceAndApiFromUserGroup filtered
                    let color = (hsvToColor (float (idx * 360 / targetGroupCount)) 0.7 0.9).ToArgb()
                    createDeviceApis groupName device deviceApiList color
            )
            |> Seq.concat
            |> Seq.toArray

        let userGroupTags = userGroupApis |> Array.map (fun d -> d.Tag) |> Set.ofArray
        let remainingNames = names |> Array.filter (fun n -> not (userGroupTags.Contains n))

        // 2. 자동 처리
        let maxLen = remainingNames |> Array.maxBy String.length |> String.length
        let cache = ConcurrentDictionary<int, (string * string[])[]>()

        let bestGroups =
            [|1 .. maxLen|]
            |> Array.Parallel.choose (fun len ->
                let result = cache.GetOrAdd(len, fun l -> groupByPrefixLength remainingNames l)
                if result.Length <= targetGroupCount then Some result else None)
            |> fun arr ->
                if arr.Length = 0 then [| ("ALL", remainingNames) |]
                else arr |> Array.minBy (fun g -> abs (g.Length - targetGroupCount))

        let autoGroupApis =
            bestGroups
            |> Array.sortBy fst
            |> Array.mapi (fun idx (_grp, tags) ->
                let hue = float ((idx + Seq.length userPairs) * 360 / targetGroupCount)
                let color = (hsvToColor hue 0.7 0.9).ToArgb()
                let tagList = tags |> Array.toList
                let deviceCandidates = extractDevicePrefixes tagList
                let prefixTrie = deviceCandidates |> Array.map fst |> buildPrefixTrie
                let deviceNames = tags |> Array.map (fun tag -> tryFindLongestPrefix prefixTrie tag |> Option.defaultValue tag)
                let groupName = findSafeGroupName (deviceNames |> Array.toList)

                tags
                |> Array.map (fun tag ->
                    let deviceFull = tryFindLongestPrefix prefixTrie tag |> Option.defaultValue tag
                    let api = extractApiFromTag tag deviceFull
                    let device =
                        if groupName = NonGroup then deviceFull
                        elif deviceFull.Length > groupName.Length then deviceFull.Substring(groupName.Length)
                        else failwith $"error: device={deviceFull}, groupName={groupName}, tag={tag}"

                    let work =
                        let devSplit = device.Split(SegmentSplit, StringSplitOptions.RemoveEmptyEntries)
                        if groupName = NonGroup && devSplit.Length > 1 then devSplit[1] else devSplit[0]

                    DeviceApi(
                        Area = validName groupName,
                        Work = validName work,
                        Device = validName device,
                        Api = validName api,
                        Tag = tag,
                        OutAddress = "",
                        InAddress = "",
                        Color = color
                    )
                )
            )
            |> Array.concat

        Array.append userGroupApis autoGroupApis
