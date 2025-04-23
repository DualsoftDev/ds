namespace PLC.Mapper.FS

open System


[<AutoOpen>]
module MappingGroupModule =

    let findGroupName (tags: string[]) : string =
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
