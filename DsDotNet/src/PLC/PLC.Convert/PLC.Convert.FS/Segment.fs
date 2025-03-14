namespace PLC.Convert.FS

open System
open System.Text
open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open System.Collections.Generic
open PLC.Convert.LSCore.Expression
open FilterJsonModule

module SegmentModule =

    
    /// **Segment 타입 정의**
    type Segment = {
        Area: string
        Device: string
        Api: string
        SourceName: string
    }
    with 
        //member x.DeviceApi = $"{x.Device}.{x.Api}({x.SourceName})"
        //member x.FullName = $"{x.Area}.{x.Device}.{x.Api}({x.SourceName})"  
        member x.DeviceApi =
                            if x.Device = "" 
                            then x.SourceName
                            else $"{x.Device}.{x.Api}"

        member x.FullName = if x.Area = "" 
                            then x.DeviceApi
                            else $"{x.Area}.{x.Device}.{x.Api}"
                          
        member x.FullNameSkipArea(targetArea: string) =
            if targetArea = x.Area then x.DeviceApi else x.FullName

    

    /// **Device와 API를 `_` 기준으로 분리하여 Segment 생성**
    let getSegment (head: string) (tail: string) (sourceName: string) : Segment = 
        let parts = tail.Split('_') |> Array.toList

        // `이름#숫자` 패턴을 찾기 위한 정규식
        let nameNumberPattern = @"^[A-Za-z]+\d+$"
        let numberOnlyPattern = @"^\d+$"

        // 가장 먼저 등장하는 `이름#숫자` 패턴을 찾음 (단, 마지막 요소가 숫자면 무시)
        let matched =
            parts
            |> List.tryFind (fun part -> 
                Regex.IsMatch(part, nameNumberPattern) && 
                not (Regex.IsMatch(List.last parts, numberOnlyPattern))
            )

        match matched with
        | Some dev -> 
            let api = parts |> List.skipWhile ((<>) dev) |> List.tail |> String.concat "_"  // `dev` 이후의 값들을 API로 설정
            { Area = head; Device = dev; Api = api; SourceName = sourceName}

        | None ->
            match List.rev parts with
            | api :: mid :: devParts when Regex.IsMatch(api, numberOnlyPattern) -> 
                { Area = head; Device = String.concat "_" (List.rev devParts); Api = mid + "_" + api; SourceName = sourceName }  // 마지막 숫자 처리

            | api :: devParts -> 
                { Area = head; Device = String.concat "_" (List.rev devParts); Api = api; SourceName = sourceName }  // `_` 기준 일반 분리

            | [] -> { Area = head; Device = tail; Api = "" ; SourceName = sourceName} // tail이 비어있을 경우 기본값


    /// **Step 1: `_M_`을 기준으로 Area와 Body 분리 (정규식 대응)**
    let splitSegment (input: string) : Segment =
        match input.Split(splitAreaKeywords |> List.toArray, StringSplitOptions.None) with
        | [| area; body |] ->
            match areaExtenstionKeywords |> List.tryFind (fun pattern -> Regex.IsMatch(body, pattern)) with
            | Some pattern ->
                let m = Regex.Match(body, pattern)
                if m.Success then
                      let ext = m.Groups.[0].Value
                      getSegment $"{area}_{ext}" body input
                else
                      getSegment area body input
            | None -> getSegment area body input

        | _ -> getSegment "" input input // Area가 없으면 전체를 Body로 간주