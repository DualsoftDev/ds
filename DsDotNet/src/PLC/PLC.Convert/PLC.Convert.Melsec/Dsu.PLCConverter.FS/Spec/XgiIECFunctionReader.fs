namespace Dsu.PLCConverter.FS

open System
open System.IO
open System.Collections.Generic
open System.Reflection
open Dsu.PLCConverter.FS.XgiSpecs
open Dsu.PLCConverter.FS.ActivePattern
[<AutoOpen>]
/// IEC FunctionReader 규격을 읽기 위한 모듈
module IECFunctionReader =

    /// `pathFBtxt`: 기본 경로에서 `XG5000_IEC_FUNC.txt` 파일의 경로를 결정
    let pathFBtxt =
        let path = Directory.GetCurrentDirectory()+"/Config/XG5000_IEC_FUNC.txt"
        if File.Exists(path) 
        then path 
        else  __SOURCE_DIRECTORY__ + @"/../Config/XG5000_IEC_FUNC.txt" //유닛테스트용
    
    /// `createReader`: 텍스트 파일을 읽어 딕셔너리로 변환
    /// 각 함수 블록(FB)의 정의를 딕셔너리로 저장하여 접근 가능하도록 함
    let createReader =
        let filePath = if(XgiOption.PathFBList = "") then pathFBtxt else XgiOption.PathFBList
        let dic =
            File.ReadAllLines(filePath)
            |> Seq.filter (fun s -> not <| s.StartsWith("#"))
            |> Seq.map (fun s -> s.Trim())
            |> Seq.splitOn (fun s1 s2 -> s1.isEmpty() && not <| s2.isEmpty())
            |> Seq.map (Seq.filter (String.IsNullOrEmpty >> not) >> Array.ofSeq)
            |> Seq.map (fun fb ->
                let fName =
                    let fnameLine = fb |> Array.find (fun l -> l.StartsWith("FNAME:"))
                    match fnameLine with
                    | RegexPattern @"FNAME: (\w+)$" [fn] ->
                        fn
                    | _ -> failwithf "ERROR"
                fName, fb )
            |> dict |> Dictionary
        dic

    /// `existFBXML`: 특정 FB 이름이 딕셔너리에 존재하는지 확인
    let existFBXML fnameSource =
        createReader.ContainsKey(fnameSource)

    /// `getFBXML`: FB 이름을 기준으로 XML 저장 파라미터 생성
    /// FB의 이름과 인스턴스, 인덱스 등의 정보를 XML 형태의 문자열로 반환
    let getFBXML (fnameSource, fnameTarget, instance, index) =
        let fbXml = 
            createReader.[fnameSource] 
            |> Array.map (function
                | StartsWith "FNAME: " -> sprintf "FNAME: %s" fnameTarget
                | StartsWith "INSTANCE: " -> sprintf "INSTANCE: %s" instance
                | StartsWith "INDEX: " -> sprintf "INDEX: %d" index
                | str -> str)
            |> String.concat "&#xA;" 
        fbXml + " &#xA;"

    /// `getFBInCount`: FB 이름을 기준으로 입력 파라미터 개수를 반환
    /// FB에서 VAR_IN 및 VAR_IN_OUT 타입의 파라미터 수를 계산하여 반환
    let getFBInCount (fnameSource: string) =
        let lstIn = 
            createReader.[fnameSource]
            |> Array.filter (fun f -> not (f.StartsWith("VAR_IN: EN"))) // EN 파라미터 제외
            |> Array.filter (fun f -> f.StartsWith("VAR_IN: ") || f.StartsWith("VAR_IN_OUT: "))
            |> Array.toSeq
        let onlyIn = 
            lstIn 
            |> Seq.filter (fun f -> f.StartsWith("VAR_IN: ")) 

        onlyIn.length(), lstIn.length() // 순수 입력 파라미터와 전체 파라미터 수 반환

    /// `getFBIndex`: FB 이름을 기준으로 인덱스를 반환
    /// FB의 정의에서 "INDEX:"로 시작하는 라인을 찾아 해당 인덱스를 정수로 반환
    let getFBIndex (fnameSource: string) =
        let lstIndex = 
            if not (createReader.ContainsKey fnameSource)
            then 
                failwithf $"XG5000_IEC_FUNC.txt 에 {fnameSource} 항목이 없습니다." 

            createReader.[fnameSource]
            |> Array.filter (fun f -> f.StartsWith("INDEX: "))
            |> Array.toSeq

        lstIndex |> Seq.head |> fun f -> f.Replace("INDEX: ", "") |> int
