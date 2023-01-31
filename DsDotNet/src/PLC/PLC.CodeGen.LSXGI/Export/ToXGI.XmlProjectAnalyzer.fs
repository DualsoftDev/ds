namespace PLC.CodeGen.LSXGI

open System.Linq
open System.Xml

open Engine.Common.FS
open Engine.Core
open PLC.CodeGen.LSXGI
open PLC.CodeGen.Common

[<AutoOpen>]
module XgiXmlProjectAnalyzerModule =
    // symbolContainerXmlNode: e.g "//Configurations/Configuration/GlobalVariables/GlobalVariable"
    let internal collectSymbolInfos (symbolContainerXmlNode:XmlNode) : SymbolInfo list =
        let symbols = symbolContainerXmlNode.SelectNodes("//Symbols/Symbol")
        [
            for sym in symbols do
                // [| "Name"; "Kind"; "Type"; "Address"; "Comment"; "Device"; "State" |]
                let dic = sym.GetAttributes()
                { defaultSymbolInfo with
                    Name          = dic["Name"]
                    Comment       = dic["Comment"]
                    Address       = dic.TryFindIt("Address") |> Option.toString
                    Kind          = dic["Kind"] |> System.Int32.Parse
                }
        ]

    let analyzeXmlProject existingLSISprj =
        let xdoc = XmlDocument.loadFromFile existingLSISprj
        let xnGlobalVar = xdoc.SelectSingleNode("//Configurations/Configuration/GlobalVariables/GlobalVariable")
        let globalsWithAddress = collectSymbolInfos xnGlobalVar |> filter (fun symbolInfo -> symbolInfo.Address.NonNullAny())

        let countExistingGlobal = xnGlobalVar.Attributes.["Count"].Value |> System.Int32.Parse
        let xnGlobalVarSymbols = xnGlobalVar.GetXmlNode "Symbols"

        noop()