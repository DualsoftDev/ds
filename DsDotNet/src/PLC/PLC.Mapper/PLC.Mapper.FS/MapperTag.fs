namespace PLC.Mapper.FS

open System.Collections.Generic
open System.Text.RegularExpressions
open System

[<AutoOpen>]
module MapperTagModule = 

    [<Literal>]
    let NonGroup = "NonGroup"
    let SegmentSplit = [|' '; '_'; '-'|]

    let validName (txt: string) =
        if String.IsNullOrWhiteSpace(txt) then
            failwith "Invalid name: empty or whitespace string"
        else
            let pattern = @"[ \.\()\[\]]"
            let cleaned = Regex.Replace(txt, pattern, "_")
            let compressed = Regex.Replace(cleaned, "_{2,}", "_")
            let trimmed = compressed.Trim(SegmentSplit)

            if String.IsNullOrWhiteSpace(trimmed) then "_"
            else trimmed

