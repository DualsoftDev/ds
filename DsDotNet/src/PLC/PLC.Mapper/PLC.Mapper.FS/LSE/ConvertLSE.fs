namespace PLC.Mapper.LSElectric

open System
open System.IO
open System.Text.RegularExpressions
open PLC.Mapper.FS
open Dual.PLC.Common.FS     
open System.Collections.Generic
open XgtProtocol

module ConvertLSEModule =

            
    let parseLSEFile (filePath: string) =
        let lines = File.ReadLines(filePath) // Stream 방식으로 메모리 절약
        let isXGI = IsXg5kXGI filePath

        let tags = XmlReader.ReadTags (filePath, false) |> fun (tag, _, _)  -> tag |> Seq.cast<XGTTag>
        let tagsByAddress = tags |> Seq.map(fun t -> t.GetAddressAlias(Boolean), t) |> dict
        let tagsByName = tags |> Seq.map(fun t -> t.Name, t) |> dict
        let _newTagAddressDic = Dictionary<string, XGTTag>()

        let networks = ResizeArray<Rung>()
        let mutable currentTitle = ""
        let mutable currentContent = ResizeArray<PlcTerminal>()


        let classifyContent (line: string) : PlcTerminal option =
            let extractContent (line: string) =
                let m = Regex.Match(line, @"<Element[^>]*>(.*?)</Element>")
                if m.Success then Some m.Groups[1].Value else None

            let containsElementType (etype: ElementType) =
                line.Contains($"ElementType=\"{etype |> int}\"")

            let getTerminal (variable: string) (tType: TerminalType) =
                let lsNotTag = variable |> Seq.forall System.Char.IsDigit 
                                || variable.Contains("#")
                                || variable.StartsWith("_")
                if lsNotTag then None
                else
                    let tag = 
                        let isAddress  = 
                            if isXGI 
                            then (tryParseXgiTag variable).IsSome 
                            else (tryParseXgkTag variable).IsSome
                        
                        if isAddress then 
                            let newTag = XGTTag(variable, isXGI, false)
                            let boolAddress = newTag.GetAddressAlias(Boolean)
                            if tagsByAddress.ContainsKey boolAddress then
                                tagsByAddress.[boolAddress]  |> Some
                            else 
                                if _newTagAddressDic.ContainsKey (boolAddress)
                                then
                                    _newTagAddressDic.[boolAddress] |> Some
                                else 
                                    _newTagAddressDic.Add(newTag.GetAddressAlias(Boolean), newTag)   
                                    newTag  |> Some
                       
                        else 
                            if tagsByName.ContainsKey variable then
                                tagsByName.[variable]  |> Some
                            else
                                None




                    if tag.IsSome then 
                        PlcTerminal(tag.Value, tType) |> Some
                    else 
                        None


            match () with
            | _ when containsElementType ElementType.CoilMode
                     || containsElementType ElementType.VariableMode ->
                extractContent line
                |> Option.bind (fun v -> getTerminal v TerminalType.Coil)

            | _ when containsElementType ElementType.ClosedContactMode ->
                extractContent line
                |> Option.bind (fun v -> getTerminal v TerminalType.ContactNegated)

            | _ when containsElementType ElementType.ContactMode ->
                extractContent line
                |> Option.bind (fun v -> getTerminal v TerminalType.Contact)

            | _ -> None

        let titlePattern = Regex("<Program Task\s*=(.*)")
        let networkStartPattern = Regex("<Rung BlockMask")

        let tryAddLine(line) =   
            match classifyContent line with
                | Some content -> currentContent.Add(content)
                | None -> ()

        for line in lines do

            if networkStartPattern.IsMatch(line) then
                if currentContent.Count > 0 then
                    networks.Add({ Title = currentTitle; Items = currentContent.ToArray() })
                currentContent.Clear()
                tryAddLine(line)
            elif titlePattern.IsMatch(line) then
                let mm = Regex.Match(line, @">([^<]+)");
                currentTitle <- mm.Groups.[1].Value.Trim()
            else 
                tryAddLine(line)
        
        // addressTitles 생성: 주소 → 타이틀 리스트 매핑
        let addressTitles =
            networks
            |> Seq.collect (fun net ->
                net.Items
                |> Seq.map (fun term -> term.Tag.Address, net.Title)
            )
            |> Seq.groupBy fst
            |> Seq.map (fun (addr, entries) -> addr, entries |> Seq.map snd |> Seq.distinct |> List)
            |> dict

        networks.ToArray(), addressTitles


    let parseActionOutLSEFile (filePath: string) = XmlReader.ReadTags (filePath, false)
