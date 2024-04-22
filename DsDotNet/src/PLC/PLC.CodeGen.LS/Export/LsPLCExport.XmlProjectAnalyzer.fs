namespace PLC.CodeGen.LS

open System.Xml
open System.Text.RegularExpressions

open Dual.Common.Core.FS
open PLC.CodeGen.LS
open PLC.CodeGen.Common
open Engine.Core
open System.Collections.Generic
open Dual.Common.Core.FS

[<AutoOpen>]
module XgiXmlProjectAnalyzerModule =
    let xmlSymbolNodeToSymbolInfo (xnSymbol: XmlNode) : SymbolInfo =
        let dic = xnSymbol.GetAttributes()

        { defaultSymbolInfo with
            Name = dic["Name"]
            Comment = dic["Comment"]
            Address = dic.TryFindIt("Address") |> Option.toString
            Device = dic.TryFindIt("Device") |> Option.toString
            DevicePos = dic.TryFindIt("DevicePos") |> Option.bind Parse.Int |> Option.defaultValue(-1)
            Kind = dic.TryFindIt("Kind") |> Option.bind Parse.Int |> Option.defaultValue(-1)
        }

    let collectByteIndices target (addresses: string seq) : int list =
        [ for addr in addresses do
              match addr with
              | RegexPattern @"^%M([XBWDL])(\d+)$" [ m; Int32Pattern index ] ->
                  match m with
                  | "X" -> index / 8
                  | ("B" | "W" | "D" | "L") ->
                      let byteSize = getByteSizeFromPrefix m target
                      let s = index * byteSize 
                      let e = s + byteSize - 1
                      yield! [ s..e ]
                  | _ -> failwithlog "ERROR"
              | _ -> failwithlog "ERROR" ]
        |> sort
        |> distinct

    let collectGlobalSymbols (xdoc: XmlDocument) =
        xdoc.SelectMultipleNodes "//Configurations/Configuration/GlobalVariables/GlobalVariable/Symbols/Symbol"
        |> map xmlSymbolNodeToSymbolInfo
        |> List.ofSeq

    let collectAllSymbols (xdoc: XmlDocument) =
        xdoc.SelectMultipleNodes "//Configurations/Configuration//Symbols/Symbol"
        |> map xmlSymbolNodeToSymbolInfo
        |> List.ofSeq

    let collectGlobalSymbolNames (xdoc: XmlDocument) = collectGlobalSymbols xdoc |> map name

    let private collectGlobalVariableAddresses (xdoc: XmlDocument) (prefix:string) =
        collectGlobalSymbols xdoc
        |> map address
        |> filter notNullAny
        |> filter (fun addr -> addr.StartsWith(prefix))

    let collectUsedMermoryByteIndicesInGlobalSymbols (xdoc: XmlDocument) xgx=
        collectGlobalVariableAddresses xdoc "%M"
        |> collectByteIndices xgx


    let private extractNumber (address:string) =
        let fail() = failwith $"Failed to parse address: {address}"
        let pattern = @"\d+"
        let regexMatch = Regex.Match(address, pattern)
        if regexMatch.Success then
            regexMatch.Value |> System.Int32.TryParse
            |> function
                | (true, number) -> number
                | _ -> fail()
        else
            fail()

    let collectCounterAddressXgk (xdoc: XmlDocument) =
        collectGlobalVariableAddresses xdoc "C" |> map extractNumber
    let collectTimerAddressXgk (xdoc: XmlDocument) =
        collectGlobalVariableAddresses xdoc "T" |> map extractNumber

    let collectXgkBasicParameters (xdoc: XmlDocument) : Dictionary<string, int> =
        xdoc.GetXmlNode("//Configurations/Configuration/Parameters/Parameter/XGTBasicParam").GetAttributes()
        |> map (fun (KeyValue(k, v)) ->
            match Parse.Int v with
            | Some v -> Some (k, v)
            | None -> None)
        |> choose id
        |> Tuple.toDictionary