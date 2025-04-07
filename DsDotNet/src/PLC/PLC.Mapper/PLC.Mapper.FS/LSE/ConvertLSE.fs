namespace PLC.Mapper.LSElectric

open System
open System.IO
open System.Text.RegularExpressions
open PLC.Mapper.FS
open Dual.PLC.Common.FS     

module ConvertLSEModule =

            
    let parseLSEFile (filePath: string) =
        let lines = File.ReadLines(filePath) // Stream 방식으로 메모리 절약
        let tags = XmlReader.ReadTags (filePath, false) |> fst
        let tagsByAddress = tags |> Seq.map(fun t -> t.Address, t) |> dict
        let tagsByName = tags |> Seq.map(fun t -> t.Name, t) |> dict

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
                let tag = 
                    if tagsByAddress.ContainsKey variable then
                        tagsByAddress.[variable]
                    elif tagsByName.ContainsKey variable then
                        tagsByName.[variable]
                    else
                        failwith $"Tag not found: {variable}"

                PlcTerminal(tag, tType)


            match () with
            | _ when containsElementType ElementType.CoilMode
                     || containsElementType ElementType.VariableMode ->
                extractContent line
                |> Option.map (fun v -> getTerminal v TerminalType.Coil)

            | _ when containsElementType ElementType.ClosedContactMode ->
                extractContent line
                |> Option.map (fun v -> getTerminal v TerminalType.ContactNegated)

            | _ when containsElementType ElementType.ContactMode ->
                extractContent line
                |> Option.map (fun v -> getTerminal v TerminalType.Contact)

            | _ -> None

        let titlePattern = Regex("<Program Task\s*=(.*)")
        let networkStartPattern = Regex("<Rung BlockMask")

        let addLine(line) =   
            match classifyContent line with
                | Some content -> currentContent.Add(content)
                | None -> ()

        for line in lines do

            if networkStartPattern.IsMatch(line) then
                if currentContent.Count > 0 then
                    networks.Add({ Title = currentTitle; Items = currentContent.ToArray() })
                currentTitle <- ""
                currentContent.Clear()
                addLine(line)
            elif titlePattern.IsMatch(line) then
                let m = titlePattern.Match(line)
                currentTitle <- m.Groups.[1].Value.Trim()
            else 
                addLine(line)
        
   
        networks.ToArray()


    let parseActionOutLSEFile (filePath: string) = XmlReader.ReadTags (filePath, false)
