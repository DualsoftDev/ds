namespace PLC.Mapper.FS

open System
open System.Drawing
open System.Collections.Generic
open ColorUtilModule
open MapperDataModule
open System.Text.RegularExpressions
open System.Collections.Concurrent
open System.Threading.Tasks

module MappingDeviceModule =

    /// 주어진 문자열에서 특수문자 제거 및 공백 제거
    let SegmentSplit = [|' '; '_'; '-'|]
    [<Literal>]
    let NonGroup = "NonGroup"
    let validName (txt: string) =
        let pattern = @"[ \-\.:/\\()\[\]~<>""|?*]"
        let replaced = Regex.Replace(txt, pattern, "_")
        replaced.Trim(SegmentSplit)
         
    /// 그룹핑 수행을 위한 내부 도우미
    let groupByPrefixLength (names: string array) (prefixLen: int) : (string * string list) list =
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

    /// findCommonPrefix 결과가 충돌할 경우 마지막 segment 제거
    let findSafeGroupName (items: string list) : string =
        let items = items |> Seq.map(fun f->f.Trim(SegmentSplit)) |> Seq.toList
        let rawPrefix = findCommonPrefix items
        let trimmedPrefix = rawPrefix.TrimEnd(SegmentSplit)

        let hasExactMatch = items |> List.exists (fun name -> name = trimmedPrefix)

        if hasExactMatch then
            // 마지막 세그먼트를 원본 문자열에서 잘라냄
            let lastIndex =
                SegmentSplit
                |> Array.map (fun sep -> trimmedPrefix.LastIndexOf(sep))
                |> Array.max

            if lastIndex > 0 then
                trimmedPrefix.Substring(0, lastIndex)
            else
                NonGroup
        else
            trimmedPrefix


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

    ///// tag에서 device를 제외한 나머지를 API로 분리 (마지막 segment만 반환)
    //let extractApiFromTag (tag: string) (device: string) : string =
    //    let rest =
    //        if tag.StartsWith(device) && tag.Length > device.Length then
    //            tag.Substring(device.Length).TrimStart(SegmentSplit)
    //        else
    //            tag

    //    let segments = rest.Split(SegmentSplit, StringSplitOptions.RemoveEmptyEntries)
    //    if segments.Length > 0 then
    //        segments.[segments.Length - 1]
    //    else
    //        rest

 

     /// tag에서 device를 제외한 나머지를 API로 분리
    let extractApiFromTag (tag: string) (device: string) : string =
        if tag.StartsWith(device) && tag.Length <> device.Length then
            $"{tag.Substring(device.Length)}"
        else
            let segments = tag.Split(SegmentSplit, StringSplitOptions.RemoveEmptyEntries)
            if segments.Length > 0 then
                segments.[segments.Length - 1]
            else
                tag

    /// 주어진 변수 이름 리스트를 지정된 그룹 수에 맞춰 디바이스/Api 추출 및 색상 매핑까지 포함해 반환
 
    let extractGroupDeviceApis (names: string array) (targetGroupCount: int) : DeviceApi seq =
        if targetGroupCount = 0 then failwith "targetGroupCount 0 입니다."
        if names.Length = 0 then failwith "이름 리스트가 비어있습니다."

        let maxLen = names |> Seq.map String.length |> Seq.max
        let nameArray = names 

        // 캐시 저장소
        let cache = ConcurrentDictionary<int, (string * string list) list>()

        // 병렬로 groupByPrefixLength 실행
        let allGroupings =
            [|1 .. maxLen|]
            |> Array.Parallel.choose (fun len ->
                let res =
                    cache.GetOrAdd(len, fun l ->
                        groupByPrefixLength  nameArray l
                    )
                if res.Length <= targetGroupCount then Some res else None
            )

        let bestGroups =
            if allGroupings.Length = 0 then
                [ ("ALL", nameArray |> Array.toList) ]
            else
                allGroupings
                |> Array.minBy (fun g -> abs (g.Length - targetGroupCount))

        bestGroups
        |> List.sortBy fst
        |> List.mapi (fun idx (_grp, tags) ->
            let hue = float (idx * 360 / targetGroupCount)
            let color = (hsvToColor hue 0.6 0.9).ToArgb()
            let deviceCandidates = extractDevicePrefixes tags
            let deviceNames = tags |> List.map (fun tag -> findBestMatchingDevice tag deviceCandidates)
            let groupName = findSafeGroupName deviceNames
   
            tags
            |> List.map (fun tag ->
                let deviceFull = findBestMatchingDevice tag deviceCandidates
                let api = extractApiFromTag tag deviceFull  
                let device =

                    if groupName = NonGroup 
                    then 
                        deviceFull
                    elif deviceFull.Length > groupName.Length then
                        deviceFull.Substring(groupName.Length) 
                    else
                        failwith $"error :  device{deviceFull},  groupName{groupName},  tag{tag}"



                if device.Length = 0 then failwith $"device 이름이 없습니다. tag: {tag}, deviceFull: {deviceFull}, groupName: {groupName}"
                DeviceApi(
                    Group = (groupName|> validName),
                    Device = (device|> validName),
                    Api = (api|> validName),
                    Tag = tag,
                    OutAddress = "",
                    InAddress = "",
                    Color = color
                )
            )
        )
        |> List.collect id
        |> Seq.ofList
