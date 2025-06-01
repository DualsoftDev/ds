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
module MappingApiModule =

    let extractApiFromTag (tag: MapperTag) (device: string) : string =
        if tag.Name.StartsWith(device) && tag.Name.Length <> device.Length then
            tag.Name.Substring(device.Length)
        elif tag.Name = device then "DO"
        else failwithf "extractApiFromTag tag %s device %s" tag.Name device

    let createDeviceApi (groupName: string) (device: string) (_work: string) (api: string) (tag: MapperTag) (color: int) (_usingPouGroup:bool): DeviceApi =
        let area = validName (defaultValueIfEmpty groupName "UnknownGroup")
        DeviceApi(
            Area = area,
            //Work = (if usingPouGroup then  area else validName (defaultValueIfEmpty work "UnknownWork")),
            Work = area,
            Device = validName (defaultValueIfEmpty device "UnknownDevice"),
            Api = validName (defaultValueIfEmpty api "Default"),
            MapperTag = tag,
            OutAddress = "",
            InAddress = "",
            Color = color
        )

    let createDeviceApis (groupName: string) (device: string) (deviceApiList: (MapperTag * string)[])  (color: int) usingPouGroup: DeviceApi[] =
        let work =
            let devSplit = device.Split(SegmentSplit, StringSplitOptions.RemoveEmptyEntries)
            if groupName = NonGroup && devSplit.Length > 1 then devSplit.[1]
            elif devSplit.Length > 0 then devSplit.[0]
            else "UnknownWork"

        deviceApiList
        |> Array.map (fun (tag, api) -> createDeviceApi groupName device work api tag color usingPouGroup)

    let createUserGroupApis (mapperTags: MapperTag[]) (userPairs: seq<string * MapperTag[]>)  (usingPouGroup:bool) : DeviceApi[] =
        let mapperSet = mapperTags |> Array.map (fun tag -> tag.OpcName) |> Set.ofArray

        let extractDeviceAndApiFromUserGroup (tags: MapperTag[]) (groupName: string) : string * (MapperTag * string)[] =
            let prefix = findCommonPrefix (tags |> Array.map (fun t -> t.Name) |> Array.toList)
            let device = prefix.TrimStart(groupName.ToCharArray()).TrimEnd(SegmentSplit)

            let deviceApiList =
                tags |> Array.map (fun tag ->
                    let suffix = tag.Name.Substring(device.Length).TrimStart(SegmentSplit)
                    let api =
                        if String.IsNullOrWhiteSpace(suffix) then "DO"
                        else String.concat "_" (suffix.Split(SegmentSplit, StringSplitOptions.RemoveEmptyEntries))
                    tag, api)

            device, deviceApiList

        let globalDeviceNameMap = System.Collections.Generic.Dictionary<string, int>()

        userPairs
        |> Seq.groupBy fst
        |> Seq.indexed
        |> Seq.collect (fun (idx, (groupName, pairSeq)) ->
            pairSeq
            |> Seq.collect (fun (_, tags) ->
                let filtered = tags |> Array.filter (fun t -> mapperSet.Contains t.OpcName)
                if filtered.Length < 1 then [||]
                else
                    let device, deviceApiList = extractDeviceAndApiFromUserGroup filtered groupName
                    let color = (hsvToColor (float (idx * 360 / 1)) 0.7 0.9).ToArgb()

                    let finalDevice =
                        if globalDeviceNameMap.ContainsKey(device) then
                            globalDeviceNameMap.[device] <- globalDeviceNameMap.[device] + 1
                            sprintf "%s_%d" device globalDeviceNameMap.[device]
                        else
                            globalDeviceNameMap.[device] <- 0
                            device

                    createDeviceApis groupName finalDevice deviceApiList color usingPouGroup))
        |> Seq.toArray
