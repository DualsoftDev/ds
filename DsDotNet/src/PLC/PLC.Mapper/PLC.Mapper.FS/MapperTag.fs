namespace PLC.Mapper.FS

open System
open System.ComponentModel
open System.Diagnostics
open System.Runtime.Serialization
open System.Text.Json.Serialization
open System.Collections.Generic
open System.Text.RegularExpressions

[<AutoOpen>]
module MapperTagModule = 

    [<Literal>]
    let NonGroup = "NonGroup"
    let SegmentSplit = [|' '; '_'; '-'|]

    let validName (txt: string) =
        let pattern = @"[ \-\.:/\\()\[\]~<>""|?*]"
        let txt = Regex.Replace(txt, pattern, "_")
        
        Regex.Replace(txt, "__", "_").Trim(SegmentSplit)
