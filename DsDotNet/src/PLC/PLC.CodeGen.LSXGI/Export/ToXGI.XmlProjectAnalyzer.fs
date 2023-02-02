namespace PLC.CodeGen.LSXGI

open System.Xml

open Engine.Common.FS
open PLC.CodeGen.LSXGI
open PLC.CodeGen.Common

[<AutoOpen>]
module XgiXmlProjectAnalyzerModule =
    // symbolContainerXmlNode: e.g "//Configurations/Configuration/GlobalVariables/GlobalVariable"
    let internal collectSymbolInfos (symbolContainerXmlNode:XmlNode) : SymbolInfo list =
        let symbols = symbolContainerXmlNode.SelectNodes(".//Symbols/Symbol")
        [
            for sym in symbols do
                (* [| "Name"; "Kind"; "Type"; "Address"; "Comment"; "Device"; "State" |] *)
                let dic = sym.GetAttributes()
                { defaultSymbolInfo with
                    Name          = dic["Name"]
                    Comment       = dic["Comment"]
                    Address       = dic.TryFindIt("Address") |> Option.toString
                    Kind          = dic["Kind"] |> System.Int32.Parse
                }
        ]

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

    let collectGlobalSymbols existingLSISprj =
        XmlNode.ofDocumentAndXPath existingLSISprj "//Configurations/Configuration/GlobalVariables/GlobalVariable"
        |> collectSymbolInfos

    let collectGlobalSymbolNames existingLSISprj = collectGlobalSymbols existingLSISprj |> map name

    let collectUsedMermoryIndicesInGlobalSymbols existingLSISprj =
        let globalsWithAddress = collectGlobalSymbols existingLSISprj |> filter (fun symbolInfo -> symbolInfo.Address.NonNullAny())
        let globalsWithMAreaAddress = globalsWithAddress |> filter (fun symbolInfo -> symbolInfo.Address.StartsWith("%M"))
        let usedMAddresses = globalsWithMAreaAddress |> map (fun symbolInfo -> symbolInfo.Address)
        let usedMIndices = usedMAddresses |> collectByteIndices
        usedMIndices
