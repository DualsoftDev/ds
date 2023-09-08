// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office

open System.Collections.Concurrent
open System

[<AutoOpen>]
module Util =


  /// ConcurrentDictionary 를 이용한 hash
    type ConcurrentHash<'T>() =
        inherit ConcurrentDictionary<'T, 'T>()
        member x.TryAdd(item:'T) = x.TryAdd(item, item)

    let trimSpace(text:string) =   text.Trim()

    let GetSquareBrackets(name:string, bHead:bool) =
        let pattern   = "(?<=\[).*?(?=\])"  //대괄호 안에 내용은 무조건 가져온다
        let matches     = System.Text.RegularExpressions.Regex.Matches(name, pattern)
        if(bHead)
        then
            if(name.StartsWith("[") && name.Contains("]"))
            then matches.[0].Value else ""
        else
            if(name.EndsWith("]")   && name.Contains("["))
            then matches.[matches.Count-1].Value else ""

    let GetTailNumber(name:string) =
        let pattern   = "\d+$"  //글자 마지막 숫자를 찾음
        let matches   = System.Text.RegularExpressions.Regex.Matches(name, pattern)
        if matches.Count > 0
        then
             let name = System.Text.RegularExpressions.Regex.Replace(name, pattern, "")
             let number = matches.[matches.Count-1].Value |> trimSpace |> Convert.ToInt32
             name, number
        else name, 0

    //특수 대괄호 제거후 순수 이름 추출
    //[yy]xx[xxx]Name[1,3] => xx[xxx]Name
    //앞뒤가 아닌 대괄호는 사용자 이름 뒷단에서 "xx[xxx]Name" 처리
    let GetBracketsReplaceName(name:string) =
        let patternHead   = "^\[[^]]*]" // 첫 대괄호 제거
        let replaceName = System.Text.RegularExpressions.Regex.Replace(name, patternHead, "")
        let patternTail   = "\[[^]]*]$" // 끝 대괄호 제거
        let replaceName = System.Text.RegularExpressions.Regex.Replace(replaceName, patternTail, "")
        replaceName
