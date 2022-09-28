// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Runtime.CompilerServices
open System
open System.Linq
open System.Globalization

[<AutoOpen>]
module TextUtil = 
    let private isValidIdentifier (identifier:string) = 
        let isHangul(ch:char) = Char.GetUnicodeCategory(ch) = UnicodeCategory.OtherLetter;
        let isValidStart(ch:char) = ch = '_' || Char.IsLetter(ch) || isHangul(ch);
        let isValid(ch:char) = isValidStart(ch) || Char.IsDigit(ch);
        if (identifier = null || identifier = "") then
            ArgumentNullException() |> raise

        if (identifier = "_") then
            true
        else
            let chars = identifier.ToCharArray()
            let first = chars[0]
            isValidStart(first) && chars.Skip(1).All(isValid)
    let private quote(s:string) = $"\"{s}\""
    let private quoteOnDemand(s:string) = if isValidIdentifier s then s else $"\"{s}\""
 
    [<Extension>]
    type NameUtil =
        /// DS 문법에서 사용하는 identifier (Segment, flow, call 등의 이름)가 적법한지 검사.
        /// 적법하지 않으면 double quote 로 감싸주어야 한다.
        [<Extension>] static member IsValidIdentifier (identifier:string) = isValidIdentifier identifier
        [<Extension>] static member IsQuotationRequired (identifier:string) = isValidIdentifier(identifier) |> not
        [<Extension>] static member QuoteOnDemand (identifier:string) = quoteOnDemand identifier                       
        [<Extension>] static member Combine (nameComponents:string seq) = nameComponents |> Seq.map quoteOnDemand |> String.concat "."
    
    // 이름이 필요한 객체
    [<AbstractClass>]
    type Named(name) =
        let mutable name = name
        interface INamed with
            member _.Name with get () = name //and set (v) = name <- v

        member val Name : string = name with get, set
        abstract ToText : unit -> string
        default x.ToText() = name
     
        