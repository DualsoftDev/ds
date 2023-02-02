namespace PLC.CodeGen.LSXGI

open System.Xml

open Engine.Common.FS
open PLC.CodeGen.LSXGI
open PLC.CodeGen.Common

[<AutoOpen>]
module XgiXmlProjectAnalyzerModule =
    let xmlSymbolNodeToSymbolInfo (xnSymbol:XmlNode) : SymbolInfo =
        let dic = xnSymbol.GetAttributes()
        { defaultSymbolInfo with
            Name          = dic["Name"]
            Comment       = dic["Comment"]
            Address       = dic.TryFindIt("Address") |> Option.toString
            Kind          = dic["Kind"] |> System.Int32.Parse
        }

    let collectByteIndices (addresses: string seq) : int list =
        [
            for addr in addresses do
                match addr with
                | RegexPattern @"^%M([XBWDL])(\d+)$" [m; Int32Pattern index] ->
                    match m with
                    | "X" -> index / 8
                    | ("B" | "W" | "D" | "L") ->
                        let byteSize = getByteSizeFromPrefix m
                        let s = index * byteSize
                        let e = s + byteSize - 1
                        yield! [s..e]
                    | _ -> failwith "ERROR"
                | _ -> failwith "ERROR"
        ] |> sort |> distinct

    let collectGlobalSymbols (xdoc:XmlDocument) =
        xdoc.SelectMultipleNodes "//Configurations/Configuration/GlobalVariables/GlobalVariable/Symbols/Symbol"
        |> map xmlSymbolNodeToSymbolInfo
        |> List.ofSeq

    let collectGlobalSymbolNames (xdoc:XmlDocument) = collectGlobalSymbols xdoc |> map name

    let collectUsedMermoryIndicesInGlobalSymbols (xdoc:XmlDocument) =
        //let globalsWithAddress      = collectGlobalSymbols xdoc |> filter (fun symbolInfo -> symbolInfo.Address.NonNullAny())
        //let globalsWithMAreaAddress = globalsWithAddress |> filter (fun symbolInfo -> symbolInfo.Address.StartsWith("%M"))
        //let usedMAddresses          = globalsWithMAreaAddress |> map (fun symbolInfo -> symbolInfo.Address)
        //let usedMIndices            = usedMAddresses |> collectByteIndices
        //usedMIndices
        collectGlobalSymbols xdoc
        |> filter (fun symbolInfo -> symbolInfo.Address.NonNullAny())
        |> filter (fun symbolInfo -> symbolInfo.Address.StartsWith("%M"))
        |> map (fun symbolInfo -> symbolInfo.Address)
        |> collectByteIndices

