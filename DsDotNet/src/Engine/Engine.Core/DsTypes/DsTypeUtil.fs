// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Core

open System
open System.Collections

[<AutoOpen>]
module DsTypeUtilModule =

    /// 공통 함수: 문자열에서 마지막으로 닫히는 기호를 찾고, 그에 대응하는 여는 기호를 찾음
    let FindEnclosedGroup (name: string, openSymbol: char, closeSymbol: char, searchFromStart: bool) =
        let mutable startIdx = -1
        let mutable endIdx = -1
        let stack = new Stack()

        let findIndices direction startPos =
            let rec loop i =
                if i < 0 || i >= name.Length then ()
                else
                    if direction = 1 then
                        // 정방향 검색
                        if name.[i] = openSymbol then
                            stack.Push(i)
                            if stack.Count = 1 then startIdx <- i
                        elif name.[i] = closeSymbol then
                            if stack.Count > 0 then
                                stack.Pop() |> ignore
                            if stack.Count = 0 then endIdx <- i
                    else
                        // 역방향 검색
                        if name.[i] = closeSymbol then
                            stack.Push(i)
                            if stack.Count = 1 then endIdx <- i
                        elif name.[i] = openSymbol then
                            if stack.Count > 0 then
                                stack.Pop() |> ignore
                            if stack.Count = 0 then startIdx <- i

                    if startIdx = -1 || endIdx = -1 then loop (i + direction)
            loop startPos


        if searchFromStart then
            if name.StartsWith(openSymbol.ToString()) then findIndices 1 0  //첫 매칭만 처리
        else
            if name.EndsWith(closeSymbol.ToString())  then findIndices -1 (name.Length - 1) //마지막 매칭만 처리

        // endIdx가 유효하고 마지막 닫는 괄호 이후의 텍스트 포함
        //if endIdx <> -1 && endIdx < name.Length - 1 then
        //    endIdx <- name.Length - 1

        startIdx, endIdx


    let getFindText (name:string)  startIdx endIdx=
        if startIdx <> -1 && endIdx <> -1 then
            name.Substring(startIdx + 1, endIdx - startIdx - 1)
        else
            ""

    let getRemoveText (name:string)  startIdx endIdx=
        if startIdx <> -1 && endIdx <> -1 then
            name.Substring(0, startIdx) + name.Substring(endIdx + 1)
        else
            name

    /// 첫 번째 대괄호 그룹 제거
    let GetHeadBracketRemoveName (name: string) =
        let startIdx, endIdx = FindEnclosedGroup(name, '[', ']', true)
        getRemoveText name startIdx endIdx

    /// 마지막 대괄호 그룹 제거
    let GetLastBracketRemoveName (name: string) =
        let startIdx, endIdx = FindEnclosedGroup(name, '[', ']', false)
        getRemoveText name startIdx endIdx

    /// 마지막 괄호 그룹 제거
    let GetLastParenthesesRemoveName (name: string) =
        let startIdx, endIdx = FindEnclosedGroup(name, '(', ')', false)
        getRemoveText name startIdx endIdx

    /// 마지막 괄호 그룹 내용 반환
    let GetLastParenthesesContents (name: string) =
        let startIdx, endIdx = FindEnclosedGroup(name, '(', ')', false)
        getFindText name startIdx endIdx

    /// 마지막 대괄호 그룹 내용 반환
    let GetLastBracketContents (name: string) =
        let startIdx, endIdx = FindEnclosedGroup(name, '[', ']', false)
        getFindText name startIdx endIdx

    /// 첫 번째 또는 마지막 대괄호 그룹을 반환
    let GetSquareBrackets (name: string, bHead: bool): string option =
        let startIdx, endIdx = FindEnclosedGroup(name, '[', ']', bHead)
        let text = getFindText name startIdx endIdx
        match text with
        | "" -> None
        | _ -> Some text


    /// 특수 대괄호 제거 후 순수 이름 추출
    /// [yy]xx[xxx]Name[1,3] => xx[xxx]Name
    /// 앞뒤가 아닌 대괄호는 사용자 이름 뒷단에서 "xx[xxx]Name" 처리
    let GetBracketsRemoveName (name: string) =
        name |> GetLastBracketRemoveName  |> GetHeadBracketRemoveName

    let isStringDigit (str: string) =
        str |> Seq.forall Char.IsDigit
