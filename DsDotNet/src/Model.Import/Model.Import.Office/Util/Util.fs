// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System
open System.Linq
open System.Diagnostics
open System.Collections.Concurrent
open System.Runtime.CompilerServices
open Engine.Parser

[<AutoOpen>]
module Util =

    type E =
        /// relay 변화를 Trace.WriteLine   
        [<Extension>] static member ConsolLogAction(text:string) = 
                        Trace.WriteLine ($"{DateTime.Now.Second}.{DateTime.Now.Millisecond} {text}") 

  
    /// ConcurrentDictionary 를 이용한 hash
    type ConcurrentHash<'T>() =
        inherit ConcurrentDictionary<'T, 'T>()
        member x.TryAdd(item:'T) = x.TryAdd(item, item)

    let GetValidName(name:string) = 
        if (String.IsNullOrEmpty(name)) 
            then ""
        else 
            if(ParserExtension.IsValidIdentifier(name))  then name else $"\"{name}\"" 
        
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

    //특수 대괄호 제거후 순수 이름 추출 
    //[yy]xx[xxx]Name[1,3] => xx[xxx]Name  
    //앞뒤가 아닌 대괄호는 사용자 이름 뒷단에서 "xx[xxx]Name" 처리
    let GetBracketsReplaceName(name:string) = 
        let patternHead   = "^\[[^]]*]" // 첫 대괄호 제거
        let replaceName = System.Text.RegularExpressions.Regex.Replace(name, patternHead, "")
        let patternTail   = "\[[^]]*]$" // 끝 대괄호 제거
        let replaceName = System.Text.RegularExpressions.Regex.Replace(replaceName, patternTail, "")
        replaceName
