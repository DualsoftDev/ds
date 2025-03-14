namespace PLC.Convert.FS

open System
open System.Text
open System.IO
open System.Text.Json
open System.Text.RegularExpressions

module ConvertPLC =

    // 필터링 설정을 로드하는 함수
    let loadFilterConfig (filePath: string) =
        let json = File.ReadAllText(filePath)
        JsonSerializer.Deserialize<Map<string, obj>>(json)

    // Step 1: `_M_`을 기준으로 Area와 Body 분리
    let splitArea (input: string) =
        match input.Split([| "_M_" |], StringSplitOptions.None) with
        | [| area; body |] -> area, body
        | _ -> "", input  // Area가 없으면 전체를 Body로 간주

    // Step 2 & 3: 특정 Target 키워드 추출
    let isTargetOfType (keywords: string list) (input: string) =
        keywords |> List.exists (fun kw -> input.Contains(kw))

    // Step 4: 조건의 Source 추적 (재귀적으로 N번 추적)
    let rec traceSources (targets: Map<string, string list>) (source: string) (n: int) =
        match n, Map.tryFind source targets with
        | 0, _ | _, None -> []
        | _, Some nextSources ->
            nextSources @ (nextSources |> List.collect (fun s -> traceSources targets s (n - 1)))

    // Step 5: 치환 함수 (XXX를 임의의 문자로 변환 포함)
    let replaceWords (replacements: Map<string, string>) (input: string) =
        replacements 
        |> Map.fold (fun acc oldWord newWord -> Regex.Replace(acc, oldWord.Replace("XXX", ".*"), newWord)) input

    // Step 6: Mermaid 그래프 생성
    //let generateMermaid (targets: Map<string, string list>) (config: Map<string, obj>) =
    //    let sb = StringBuilder()
    //    sb.AppendLine("graph TD;") |> ignore

    //    // JSON에서 키워드 로드
    //    let targetReplacements = config.["targetReplacements"] :?> Map<string, string>
    //    let sourceReplacements = config.["sourceReplacements"] :?> Map<string, string>

    //    // **에러 해결: 올바른 타입 변환**
    //    let groupedTargets: Map<string, list<string * string list>> =
    //        targets 
    //        |> Seq.groupBy (fun (key, _) -> fst (splitArea key))  
    //        |> Seq.map (fun (area, nodes) -> area, nodes |> Seq.map (fun (k, v) -> k, v) |> Seq.toList) 
    //        |> Map.ofSeq

    //    groupedTargets |> Map.iter (fun area nodes ->
    //        sb.AppendFormat("    subgraph {0}\n", area) |> ignore
    //        nodes |> List.iter (fun (target, sources) ->
    //            let formattedTarget = replaceWords targetReplacements target
    //            sources |> List.iter (fun source ->
    //                let formattedSource = replaceWords sourceReplacements source
    //                sb.AppendFormat("        {0} --> {1};\n", formattedSource, formattedTarget) |> ignore
    //            )
    //        )
    //        sb.AppendLine("    end") |> ignore
    //    )

    //    sb.ToString()




    // Example 실행
    let runExample () =
        let config = loadFilterConfig "filters.json"

        let exampleData =
            [ ("AREA1_M_TARGET_AUX", ["AREA1_M_SOURCE1"; "AREA1_M_SOURCE2"])
              ("AREA2_M_TARGET_IN_OK", ["AREA2_M_SOURCE3"; "AREA2_M_SOURCE4"])
              ("AREA3_M_SAFETY_AUX", ["AREA3_M_SOURCE5"; "AREA3_M_SOURCE6"]) ]
            |> Map.ofList
        ()
        //let mermaidOutput = generateMermaid exampleData config
        //printfn "%s" mermaidOutput

    // 실행
    runExample ()
