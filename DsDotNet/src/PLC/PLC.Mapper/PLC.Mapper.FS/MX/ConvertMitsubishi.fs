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
                | il when il.StartsWith("OUT") -> TerminalType.Coil
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

    /// MELSEC Network 구조 생성
    let parseMXFile (files: string[]) =
        let (pous, comments, _) = CSVParser.parseCSVs(files)
        let rungs = pous |> Array.collect (fun pou -> pou.Rungs)

        let networks =
            rungs
            |> Array.fold (fun acc rung ->
                let content =
                    rung
                    |> Array.choose (fun line -> classifyContent line comments)

                if content.Length > 0 then
                    { Title = ""; Items = content } :: acc // <-- 명시적으로 Rung 생성
                else acc
            ) []

        let tags =
            comments 
            |> Seq.map (fun kv -> 
                match MxDeviceInfo.Create(kv.Key) with
                | Some mxInfo ->
                    MelsecTag(kv.Value, mxInfo,  kv.Value)
                |  None -> failwith $"지원하지 않는 디바이스: {kv.Key}"
                )
            |> Seq.toArray

        networks, tags
        //networks |> List.rev |> List.toArray, comments
