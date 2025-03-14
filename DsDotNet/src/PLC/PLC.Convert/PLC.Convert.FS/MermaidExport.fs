namespace PLC.Convert.FS

open System
open System.Text
open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open System.Collections.Generic
open ConvertPLCModule

module MermaidExportModule =


    /// - Area별 그룹화 및 Subgraph 자동 생성
    let generateMermaid (targets: Map<string, string list>) =
        let sb = StringBuilder()
        sb.AppendLine("graph TD;") |> ignore
        // **JSON에서 키워드 로드 (올바른 변환 적용)**
        let targetReplacements = loadJsonMap config.["targetReplacements"]
        let sourceReplacements = loadJsonMap config.["sourceReplacements"]

        // **Area별 그룹화**
        let groupedTargets = targets |> Seq.groupBy (fun kv -> fst (splitArea kv.Key))

        for (area, nodes) in groupedTargets do
            sb.AppendLine($"    subgraph {area}") |> ignore

            for kvp in nodes do  
                let target = kvp.Key 
                let sources = kvp.Value

                let formattedTarget = replaceWords targetReplacements target

                for source in sources do
                    let formattedSource = replaceWords sourceReplacements source
                    sb.AppendLine($"        {formattedSource} --> {formattedTarget};") |> ignore

            sb.AppendLine("    end") |> ignore

        sb.ToString()


    /// **Rung 데이터를 Mermaid 다이어그램으로 변환하여 저장하는 함수**
    let Convert (rungs: Dictionary<string, List<string>>) =

        // ✅ **Dictionary → Map 변환 (F# 호환)**
        let rungMap = 
            rungs 
            |> Seq.take 10
            |> Seq.map (fun kvp -> kvp.Key, List.ofSeq kvp.Value)  // `List<string>`을 `string list`로 변환
            |> Map.ofSeq

        // ✅ **Mermaid 다이어그램 변환**
        let mermaidText = generateMermaid rungMap 
        mermaidText
      