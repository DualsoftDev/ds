namespace PLC.Convert.Rockwell

open System
open System.IO
open System.Collections.Generic
open System.Text.RegularExpressions
open PLC.Convert.FS.ConvertCoilModule

module ConvertRockwellModule =
  

    let classifyContent (line: string) =
        let extractContent (line: string) =
            let matchResult = Regex.Match(line, @">(.+?)</Element>")
            if matchResult.Success then Some matchResult.Groups.[1].Value else None

        if line.Contains($"ElementType=\"{14}\"") then
            match extractContent line with
            | Some content -> Some (Coil content)
            | None -> None

        elif line.Contains($"ElementType=\"{6}\"") then
            match extractContent line with
            | Some content -> Some (ContactNega content)
            | None -> None

        elif line.Contains($"ElementType=\"{7}\"") then
            match extractContent line with
            | Some content -> Some (ContactPosi content)
            | None -> None
        else
            None

    let parseLSEFile (filePath: string) =
        let lines = File.ReadLines(filePath) // Stream 방식으로 메모리 절약
        let networks = ResizeArray<Network>()
        let mutable currentTitle = ""
        let mutable currentContent = ResizeArray<ContentType>()

        let titlePattern = Regex("<Program Task\s*=(.*)")
        let networkStartPattern = Regex("<Rung BlockMask")

        for line in lines do
            if networkStartPattern.IsMatch(line) then
                if currentContent.Count > 0 then
                    networks.Add({ Title = currentTitle; Content = currentContent.ToArray() })
                currentTitle <- ""
                currentContent.Clear()
            elif titlePattern.IsMatch(line) then
                let m = titlePattern.Match(line)
                currentTitle <- m.Groups.[1].Value.Trim()
            else 
                match classifyContent line with
                | Some content -> currentContent.Add(content)
                | None -> ()
        
        if currentContent.Count > 0 then
            networks.Add({ Title = currentTitle; Content = currentContent.ToArray() })

        networks.ToArray()
