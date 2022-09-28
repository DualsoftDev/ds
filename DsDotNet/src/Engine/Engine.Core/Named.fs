// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Runtime.CompilerServices
open System
open System.Linq
open System.Globalization

[<AutoOpen>]
module TextUtil = 
    let internal isValidIdentifier (identifier:string) = 
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
    let private dq = "\""
    let private quote(s:string) = $"{dq}{s}{dq}"
    let internal quoteOnDemand(s:string) = if isValidIdentifier s then s else $"\"{s}\""
    let internal deQuoteOnDemand(s:string) = if s.StartsWith(dq) && s.EndsWith(dq) then s.Substring(1, s.Length - 2) else s
 
    type NameComponents = string[]

    // 이름이 필요한 객체
    [<AbstractClass>]
    type Named(name) =
        let mutable name = name
        interface INamed with
            member _.Name with get () = name //and set (v) = name <- v

        member val Name : string = name with get, set
        abstract ToText : unit -> string
        default x.ToText() = name
     

[<Extension>]
type NameUtil =
    /// DS 문법에서 사용하는 identifier (Segment, flow, call 등의 이름)가 적법한지 검사.
    /// 적법하지 않으면 double quote 로 감싸주어야 한다.
    [<Extension>] static member IsValidIdentifier (identifier:string) = isValidIdentifier identifier
    [<Extension>] static member IsQuotationRequired (identifier:string) = isValidIdentifier(identifier) |> not
    [<Extension>] static member QuoteOnDemand (identifier:string) = quoteOnDemand identifier                       
    [<Extension>] static member DeQuoteOnDemand (identifier:string) = deQuoteOnDemand identifier                       
    [<Extension>] static member Combine (nameComponents:string seq) = nameComponents |> Seq.map quoteOnDemand |> String.concat "."

    [<Extension>]
    static member FindWithName (namedObjects:#INamed seq, name:string) =
        namedObjects.FirstOrDefault(fun obj -> obj.Name = name)
    [<Extension>]
    static member FindWithNameComponents (namedObjects:#IQualifiedNamed seq, nameComponents:NameComponents) =
        namedObjects.FirstOrDefault(fun obj -> Enumerable.SequenceEqual(obj.NameComponents, nameComponents))
    
        