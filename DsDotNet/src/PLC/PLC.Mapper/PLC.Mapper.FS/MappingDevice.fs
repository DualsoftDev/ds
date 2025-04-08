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
            "ON"
        else
            failwith $"extractApiFromTag tag {tag} device {device} err"
            //let segments = tag.Split(SegmentSplit, StringSplitOptions.RemoveEmptyEntries)
            //if segments.Length > 0 then segments.[segments.Length - 1] else tag

    let extractGroupDeviceApis (names: string array) (targetGroupCount: int) : DeviceApi [] =
        if targetGroupCount = 0 then failwith "targetGroupCount 0 입니다."
        if names.Length = 0 then failwith "이름 리스트가 비어있습니다."

        let maxLen = names |> Array.maxBy String.length |> String.length
        let cache = ConcurrentDictionary<int, (string * string[])[]>()

        let bestGroups =
            [|1 .. maxLen|]
            |> Array.Parallel.choose (fun len ->
                let result = cache.GetOrAdd(len, fun l -> groupByPrefixLength names l)
                if result.Length <= targetGroupCount then Some result else None)
            |> fun arr ->
                if arr.Length = 0 then [| ("ALL", names) |]
                else arr |> Array.minBy (fun g -> abs (g.Length - targetGroupCount))

        bestGroups
        |> Array.sortBy fst
        |> Array.mapi (fun idx (_grp, tags) ->
            let hue = float (idx * 360 / targetGroupCount)
            let color = (hsvToColor hue 0.6 0.9).ToArgb()
            let tagList = tags |> Array.toList
            let deviceCandidates = extractDevicePrefixes tagList
            let prefixTrie = deviceCandidates |> Array.map fst |> buildPrefixTrie

            let deviceNames =
                tags |> Array.map (fun tag -> tryFindLongestPrefix prefixTrie tag |> Option.defaultValue tag)

            let groupName = findSafeGroupName (deviceNames |> Array.toList)
            let groupPrefixLen = groupName.Length

            tags
            |> Array.map (fun tag ->
                let deviceFull = tryFindLongestPrefix prefixTrie tag |> Option.defaultValue tag
                let api = extractApiFromTag tag deviceFull
                let device =
                    if groupName = NonGroup then deviceFull
                    elif deviceFull.Length > groupPrefixLen then deviceFull.Substring(groupPrefixLen)
                    else failwith $"error: device={deviceFull}, groupName={groupName}, tag={tag}"

                if groupName = "" || device = "" || api = "" 
                then 
                    failwith $"extractGroupDeviceApis {tag} err"

                //if  tag.Contains  "#201 S/OTR 파레트 선택"
                //then 
                //    try
                //        failwith $"extractGroupDeviceApis {tag} err"
                //    with _->
                //        DeviceApi(
                //            Group = validName groupName,
                //            Device = validName device,
                //            Api = validName api,
                //            Tag = tag,
                //            OutAddress = "",
                //            InAddress = "",
                //            Color = color
                //        )
                //else 
                DeviceApi(
                        Group = validName groupName,
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
