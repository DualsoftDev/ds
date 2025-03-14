namespace PLC.Convert.FS

open System.Text
open ConvertPLCModule
open SegmentModule
open PLC.Convert.LSCore.Expression
open FilterJsonModule

module MermaidExportModule =


    /// - Area별 그룹화 및 Subgraph 자동 생성
    let generateMermaid (targets: Map<string, string list>) (nofilter:bool) =
        let sb = StringBuilder()
        sb.AppendLine("graph TD;") |> ignore
 
        // **Area별 그룹화**
        let groupedTargets = targets |> Seq.groupBy (fun kv ->  (splitSegment kv.Key).Area)
        let mutable index = 0
        for (targetArea, nodes) in groupedTargets do
            //if (targetArea = "S204") then
                let targetArea = if targetArea = "" 
                                  then
                                        index <- index+1
                                        index.ToString()
                                  else targetArea

                sb.AppendLine($"    subgraph {targetArea}") |> ignore

                for kvp in nodes do  
                    let target =  replaceWords targetReplacements kvp.Key  |> splitSegment
                    if isTargetOfType safetyKeywords kvp.Key  
                    then 
                        let safetyItemText =
                            kvp.Value 
                            |> List.map (fun s ->  (replaceWords sourceReplacements s) |> splitSegment)
                            |> List.filter (fun s -> s.Area <> "" || nofilter)
                            |> List.map (fun s ->  s.FullNameSkipArea(targetArea))
                            |> String.concat "\r\n\t\t\t" 
                         
                        if safetyItemText <> "" 
                        then
                            sb.AppendLine($"        {target.FullName}.Safety{{{{\r\n\t\t\t{safetyItemText}\r\n\t\t}}}} --> {target.FullName};") |> ignore

                    let sourcesText = 
                        kvp.Value 
                        |> List.map (fun source ->  (replaceWords sourceReplacements source) |> splitSegment)
                        |> List.filter (fun seg -> seg.Area <> "" || nofilter)
                        |> List.map  (fun seg-> seg.FullNameSkipArea(targetArea))
                        |> String.concat " & " 

                    if sourcesText <> ""
                    then
                        sb.AppendLine($"        {sourcesText} --> {target.DeviceApi};") |> ignore

                sb.AppendLine("    end") |> ignore

        sb.ToString()


    /// **Rung 데이터를 Mermaid 다이어그램으로 변환하여 저장하는 함수**
    let Convert (coils: Terminal seq) =

        let rungMap = 
            coils 
            //|> Seq.take 20
            |> Seq.filter (fun coil -> (splitSegment coil.Name ).Area <> "" )  
            |> Seq.filter (fun coil -> isTargetOfType (autoKeywords@safetyKeywords) coil.Name)  
            |> Seq.filter (fun coil -> not(isTargetOfType (skipKeywords) coil.Name)) 
            |> Seq.map (fun coil -> coil.Name, getContactNamesFromCoil coil )  
            |> Map.ofSeq

        // ✅ **Mermaid 다이어그램 변환**
        let mermaidText = generateMermaid rungMap  false
        mermaidText    /// **Rung 데이터를 Mermaid 다이어그램으로 변환하여 저장하는 함수**


    let ConvertEdges (coils: Terminal seq) =

        let rungMap = 
            coils 
            |> Seq.filter (fun coil -> not(isTargetOfType (skipKeywords) coil.Name)) 
            |> Seq.map (fun coil -> coil.Name, getContactNamesFromCoil coil )  
            |> Map.ofSeq

        // ✅ **Mermaid 다이어그램 변환**
        let mermaidText = generateMermaid rungMap  true
        mermaidText
      

      