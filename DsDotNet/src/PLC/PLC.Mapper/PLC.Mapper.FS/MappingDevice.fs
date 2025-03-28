namespace PLC.Mapper.FS

open System
open System.Drawing
open System.Collections.Generic
open ColorUtilModule
open DeviceApiModule

module MappingDeviceModule =

    /// 그룹핑 수행을 위한 내부 도우미
    let groupByPrefixLength (names: List<string>) (prefixLen: int) : (string * string list) list =
        names
        |> Seq.groupBy (fun n -> if n.Length >= prefixLen then n.Substring(0, prefixLen) else n)
        |> Seq.map (fun (k, vs) -> k, List.ofSeq vs)
        |> List.ofSeq

    /// 주어진 문자열 리스트에서 가장 긴 공통 접두어 계산
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

    /// 재귀적으로 가능한 모든 device 후보 접두어 추출 (각 접두어에 대해 2개 이상의 API 조건 만족 시 재귀 분할)
    let rec splitDevicesRecursively (prefix: string) (tags: string list) : (string * string list) list =
        if tags.Length <= 2 then
            let common = findCommonPrefix tags
            [ (common, tags) ]
        else
            let groups =
                tags
                |> List.groupBy (fun t ->
                    if t.Length > prefix.Length then t.[prefix.Length].ToString() else "")
                |> List.filter (fun (_, lst) ->
                    let apis =
                            lst |> List.map (fun t -> 
                                    if t.Length = prefix.Length
                                    then 
                                        t
                                    else 
                                        t.Substring(prefix.Length + 1)

                              ) |> List.distinct
                    apis.Length >= 2)

            if groups.IsEmpty then [ (prefix, tags) ]
            else
                groups
                |> List.collect (fun (suffixChar, subTags) ->
                    let newPrefix = prefix + suffixChar
                    splitDevicesRecursively newPrefix subTags)

    /// 태그 리스트에서 가능한 device 접두어 추출 (재귀적 분할 포함)
    let extractDevicePrefixes (tags: string list) : (string * string list) list =
        splitDevicesRecursively "" tags

    /// 주어진 태그에서 가장 잘 맞는 Device 후보 prefix 추출 (가장 긴 일치)
    let findBestMatchingDevice (tag: string) (deviceCandidates: (string * string list) list) : string =
        deviceCandidates
        |> List.filter (fun (prefix, _) -> tag.StartsWith(prefix))
        |> List.sortByDescending (fun (prefix, lst) -> prefix.Length, lst.Length)
        |> List.tryHead
        |> Option.map fst
        |> Option.defaultValue tag

    /// tag에서 device를 제외한 나머지를 API로 분리
    let extractApiFromTag (tag: string) (device: string) : string =
        if tag.StartsWith(device) && tag.Length <> device.Length then
            tag.Substring(device.Length)
        else
            tag

    /// 그룹 이름을 가장 많은 태그 수를 가진 device 접두어로 재지정
    let inferBestGroupName (tags: string list) : string =
        let deviceCandidates = extractDevicePrefixes tags
        deviceCandidates
        |> List.sortByDescending (fun (_, lst) -> lst.Length)
        |> function
           | (prefix, _) :: _ -> prefix
           | [] -> "Group"

    /// 주어진 변수 이름 리스트를 지정된 그룹 수에 맞춰 디바이스/Api 추출 및 색상 매핑까지 포함해 반환
    let extractGroupDeviceApis (names: List<string>) (targetGroupCount: int) : DeviceApi seq =
        if targetGroupCount = 0 then
            failwithf "targetGroupCount 0 입니다."
        if names.Count = 0 then
            failwithf "extractGroupDeviceApis names count 0 입니다."

        let maxLen = names |> Seq.map String.length |> Seq.max

        let allGroupings =
            [1 .. maxLen]
            |> List.map (fun len -> groupByPrefixLength names len)
            |> List.filter (fun g -> g.Length <= targetGroupCount)

        let bestGroups =
            match allGroupings with
            | [] -> [ ("ALL", names |> Seq.toList) ]
            | groups -> groups |> List.minBy (fun g -> abs (g.Length - targetGroupCount))

        bestGroups
        |> List.sortBy fst
        |> List.mapi (fun idx (_grp, tags) ->
            let hue = float (idx * 360 / targetGroupCount)
            let color = (hsvToColor hue 0.6 0.9).ToArgb()
            let deviceCandidates = extractDevicePrefixes tags
            let deviceNames = tags
                                |> List.map (fun tag -> findBestMatchingDevice tag deviceCandidates)
            let groupName = findCommonPrefix deviceNames

            tags
            |> List.map (fun tag ->
                let device = findBestMatchingDevice tag deviceCandidates
                let api = extractApiFromTag tag device
                DeviceApi(
                    Group = groupName.Trim(' ', '_', '-'),
                    Device = device.Trim(' ', '_', '-'),
                    Api = api.Trim(' ', '_', '-'),
                    Tag = tag,
                    Address = "",
                    Color = color
                ))

        )
        |> List.collect id
        |> Seq.ofList