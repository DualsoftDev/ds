namespace XgtProtocol

open Dual.Common.Core.FS
open System
open System.Runtime.CompilerServices
open Dual.PLC.Common.FS


[<AutoOpen>]
module LsTagParserModule =

    let isXGI (tags: string seq) :bool=

        if tags |> Seq.filter (fun f -> System.String.IsNullOrWhiteSpace(f)) |> Seq.any
        then 
            failwithlog "태그 Address가 비어 있습니다."

        let tags = tags |> Seq.filter (fun f -> not (System.String.IsNullOrWhiteSpace(f)))
        let hasXGI = tags |> Seq.exists (fun t -> t.StartsWith("%"))
        let hasXGK = tags |> Seq.exists (fun t -> not (t.StartsWith("%")))

        if hasXGI && hasXGK then
            let xgiTags = tags |> Seq.filter (fun t -> t.StartsWith("%")) |> String.concat ", "
            let xgkTags = tags |> Seq.filter (fun t -> not (t.StartsWith("%"))) |> String.concat ", "
            failwithlog $"XGI와 XGK 태그가 혼합되어 있습니다.\n\nXGI 태그: {xgiTags}\nXGK 태그: {xgkTags}\n\n태그는 한 종류로만 구성되어야 합니다."

        hasXGI

[<Extension>]   // For C#
type LsTagParser =

    [<Extension>]
    static member IsXGI (tags: string seq) :bool= isXGI tags

    [<Extension>]
    static member GetDataType(tag:string, isXgi:bool): PlcDataSizeType =
        let size = 
            if isXgi
                then LsXgiTagParser.Parse tag |> fun (_, size, _) ->    size
                else LsXgkTagParser.Parse tag |> fun (_, size, _) ->    size
                
        PlcDataSizeType.FromBitSize size
    
