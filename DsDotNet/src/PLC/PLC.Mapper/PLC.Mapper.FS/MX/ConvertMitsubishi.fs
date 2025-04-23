namespace PLC.Mapper.MX

open System
open System.Collections.Generic
open System.Text.RegularExpressions
open Dual.PLC.Common.FS
open MelsecProtocol

module ConvertMitsubishiModule =

    /// ProgramCSVLine -> PlcTerminal 변환
    let classifyContent (programCSVLine: ProgramCSVLine) (comments: Dictionary<string, string>) =
        match programCSVLine.Arguments with
        | [| arg |] when arg.IsMemory() ->
            let name = arg.ToText()
            let comment =
                if comments.ContainsKey(name) then comments.[name].Replace(" ", "_")
                else ""

            let terminalType =
                match programCSVLine.Instruction with
                | il when il.StartsWith("OUT") || il.StartsWith("SET") || il.StartsWith("RST") -> TerminalType.Coil
                | il when il.StartsWith("ANI") || il.StartsWith("ORI") || il.StartsWith("LDI") -> TerminalType.ContactNegated
                | il when il.StartsWith("AND") || il.StartsWith("OR") || il.StartsWith("LD") -> TerminalType.Contact
                | _ -> TerminalType.Empty

            if terminalType <> TerminalType.Empty then
                match MxDeviceInfo.Create(name) with
                | Some mxInfo ->
                    let tag = MelsecTag(name, mxInfo, comment)
                    Some (PlcTerminal(tag, terminalType))
                |  None -> None
            else None
        | _ -> None


    let parseMXFile (files: string[]) =
        let (pous, comments, _) = CSVParser.parseCSVs(files)

        // Networks 생성
        let networks =
            pous
            |> Array.fold (fun acc pou ->
                let pouNetworks =
                    pou.Rungs
                    |> Array.fold (fun accRung rung ->
                        let content =
                            rung
                            |> Array.choose (fun line -> classifyContent line comments)

                        if content.Length > 0 then
                            { Title = pou.Name; Items = content } :: accRung
                        else accRung
                    ) []
                pouNetworks @ acc
            ) []

        // Tags 생성
        let tags =
            comments 
            |> Seq.choose (fun kv -> 
                match MxDeviceInfo.Create(kv.Key) with
                | Some mxInfo ->
                    MelsecTag(kv.Value, mxInfo, kv.Value) |> Some
                | None -> None
            )
            |> Seq.toArray

        // addressTitles 생성: 주소 → 타이틀 리스트 매핑 (TerminalType.Coil만)
        let addressTitles =
            networks
            |> Seq.collect (fun net ->
                net.Items
                |> Seq.choose (fun term ->
                    if term.TerminalType = TerminalType.Coil then
                        Some (term.Tag.Address, net.Title)
                    else None
                )
            )
            |> Seq.groupBy fst
            |> Seq.map (fun (addr, entries) -> addr, entries |> Seq.map snd |> Seq.distinct |> List)
            |> dict

        networks, tags, addressTitles

