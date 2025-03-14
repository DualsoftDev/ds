namespace PLC.Convert.FS

open System
open System.Text
open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open System.Collections.Generic
open PLC.Convert.LSCore.Expression

module ConvertPLCModule =

    
    /// **Step 1: JSON 설정 파일 로드**
    /// `filters.json` 파일을 자동으로 불러와 설정을 적용합니다.
    let loadFilterConfig () =
        let filePath = Path.Combine(__SOURCE_DIRECTORY__, "filters.json")
        if File.Exists(filePath) then
            let json = File.ReadAllText(filePath)
            JsonSerializer.Deserialize<Map<string, obj>>(json)
        else
            failwithf "filters.json 파일을 찾을 수 없습니다: %s" filePath
        /// JSON 데이터를 `Map<string, string>`으로 변환하는 함수**
    let loadJsonMap (jsonElement: obj) =
        match jsonElement with
        | :? JsonElement as v -> 
            v.EnumerateObject()
            |> Seq.map (fun kvp -> kvp.Name, kvp.Value.GetString())
            |> Map.ofSeq
        | _ -> failwithf "JSON 변환 오류: %s" (jsonElement.ToString())

    /// **JSON에서 특정 키를 `List<string>`으로 변환하는 함수**
    let loadJsonList (jsonElement: obj) =
        match jsonElement with
        | :? JsonElement as v -> 
            v.EnumerateArray()
            |> Seq.map (fun elem -> elem.GetString())
            |> List.ofSeq
        | _ -> failwithf "JSON 변환 오류: %s" (jsonElement.ToString())

    /// **JSON 설정 로드**
    let config = loadFilterConfig ()
    let keySkipNames = loadJsonList config.["skipKeywords"]

    /// **Step 2: `_M_`을 기준으로 Area와 Body 분리**
    /// - Area: `AREA1`, `AREA2` 등의 구분자 역할
    /// - Body: `_TARGET_AUX`, `_SOURCE1` 등
    let splitArea (input: string) =
        match input.Split([| "_M_" |], StringSplitOptions.None) with
        | [| area; body |] -> area, body
        | _ -> "", input  // Area가 없으면 전체를 Body로 간주

    /// **Step 3: 특정 키워드(Target/Safety) 포함 여부 확인**
    /// - 특정 키워드가 포함된 경우 해당 요소를 처리
    let isTargetOfType (keywords: string list) (input: string) =
        keywords |> List.exists (fun kw -> input.Contains(kw))

    /// **Step 4: 조건의 Source 추적 (재귀적으로 N번 수행)**
    /// - 특정 Source가 여러 단계의 관계를 가질 경우 N번 반복적으로 추적
    let rec traceSources (targets: Map<string, string list>) (source: string) (n: int) =
        match n, Map.tryFind source targets with
        | 0, _ | _, None -> []
        | _, Some nextSources ->
            nextSources @ (nextSources |> List.collect (fun s -> traceSources targets s (n - 1)))

    /// **Step 5: 단어 치환**
    /// - XXX를 동적 패턴으로 변환하여 처리
    let replaceWords (replacements: Map<string, string>) (input: string) =
        replacements 
        |> Map.fold (fun acc oldWord newWord -> Regex.Replace(acc, oldWord.Replace("XXX", ".*"), newWord)) input

      /// **Contact 탐색 (재귀적으로 내부 Expression 검사)**
  
    let rec getContactNames (contactTerminals: Terminal list) (contactUnits: Terminal list) (expressionText: string) (depth: int) (maxDepth: int) (bPositive: bool) =
        if depth >= maxDepth then contactUnits  // **최대 깊이 도달 시 종료**
        else
            contactTerminals
            |> List.fold (fun acc terminal ->
                if acc |> List.exists (fun c -> c.Name = terminal.Name) then acc // **중복 방지**
                else
                    let isPositive = not (expressionText.Contains($"!{terminal.Name}"))

                    if isPositive = bPositive && not (keySkipNames |> List.exists terminal.Name.Contains) then
                        let updatedUnits = terminal :: acc  // **유효한 터미널 추가**

                        // **내부 Expression 탐색 최적화 (패턴 매칭 적용)**
                        match terminal.HasInnerExpr, terminal.InnerExpr with
                        | true, innerExpr ->
                            getContactNames (List.ofSeq (innerExpr.GetTerminals())) updatedUnits (innerExpr.ToText()) (depth + 1) maxDepth bPositive
                        | _ -> updatedUnits
                    else acc
            ) contactUnits  // **초기 값 contactUnits 설정**

    let getContactNamesForCSharp (contactTerminals: List<Terminal>) (contactUnits: List<Terminal>) (expressionText: string) (depth: int) (maxDepth: int) (bPositive: bool) =
        getContactNames (List.ofSeq contactTerminals) (List.ofSeq contactUnits) expressionText depth maxDepth bPositive
        |> List
        
