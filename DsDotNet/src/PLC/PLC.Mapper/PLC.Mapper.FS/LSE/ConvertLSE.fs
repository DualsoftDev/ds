namespace PLC.Mapper.LSElectric

open System
open System.IO
open System.Text.RegularExpressions
open PLC.Mapper.FS

module ConvertLSEModule =


    let classifyContent (line: string) =
        let extractContent (line: string) =
            let matchResult = Regex.Match(line, @"<Element[^>]*>(.*?)</Element>")
            if matchResult.Success then Some matchResult.Groups[1].Value else None

        if line.Contains($"ElementType=\"{ElementType.CoilMode |> int}\"") || 
           line.Contains($"ElementType=\"{ElementType.VariableMode |> int}\"") 
        then
            match extractContent line with
            | Some content -> Some (Coil (PlcTerminal(variable = content)))
            | None -> None

        elif line.Contains($"ElementType=\"{ElementType.ClosedContactMode |> int}\"") then
            match extractContent line with
            | Some content -> Some (ContactNega (PlcTerminal(variable = content)))
            | None -> None

        elif line.Contains($"ElementType=\"{ElementType.ContactMode |> int}\"") then
            match extractContent line with
            | Some content -> Some (ContactPosi (PlcTerminal(variable = content)))
            | None -> None
        else
            None
            
    let parseLSEFile (filePath: string) =
        let lines = File.ReadLines(filePath) // Stream 방식으로 메모리 절약
        let networks = ResizeArray<Network>()
        let mutable currentTitle = ""
        let mutable currentContent = ResizeArray<Terminal>()

        let titlePattern = Regex("<Program Task\s*=(.*)")
        let networkStartPattern = Regex("<Rung BlockMask")

        let addLine(line) =   
            match classifyContent line with
                | Some content -> currentContent.Add(content)
                | None -> ()

        for line in lines do

            if networkStartPattern.IsMatch(line) then
                if currentContent.Count > 0 then
                    networks.Add({ Title = currentTitle; Content = currentContent.ToArray() })
                currentTitle <- ""
                currentContent.Clear()
                addLine(line)
            elif titlePattern.IsMatch(line) then
                let m = titlePattern.Match(line)
                currentTitle <- m.Groups.[1].Value.Trim()
            else 
                addLine(line)
        
   
        networks.ToArray()


    let parseActionOutLSEFile (filePath: string) = XmlReader.ReadTags  (filePath, false)
