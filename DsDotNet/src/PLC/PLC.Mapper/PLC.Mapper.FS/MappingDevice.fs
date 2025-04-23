namespace PLC.Mapper.FS

open System


[<AutoOpen>]
module MappingDeviceModule =

    let extractDevicePrefixes (tags: string list) : (string * string[])[] =
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

        splitDevicesRecursively "" tags
        |> List.sortByDescending (fun (prefix, lst) -> prefix.Length, lst.Length)
        |> List.map (fun (p, l) -> p, List.toArray l)
        |> Array.ofList
