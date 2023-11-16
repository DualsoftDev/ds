// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System
open System.Linq
open System.Globalization
open System.Text.RegularExpressions
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Dual.Common.Core.FS

// Module for text manipulation functions
[<AutoOpen>]
module TextImpl =
    // Helper functions
    let isHangul(ch:char) = Char.GetUnicodeCategory(ch) = UnicodeCategory.OtherLetter
    let isValidStart(ch:char) = ch = '_' || Char.IsLetter(ch) || isHangul(ch)
    let dq = "\""

    let isValidIdentifier (identifier:string) =
        let isValid(ch:char) = isValidStart(ch) || Char.IsDigit(ch)
        match identifier with
        | null | "" -> false
        | "_" -> true
        | _ -> 
            let chars = identifier.ToCharArray()
            isValidStart(chars.[0]) && chars.Skip(1).All(isValid)

    let quote(s:string) = $"{dq}{s}{dq}"

    let quoteOnDemand(s:string) =
        match s with
        | x when isValidIdentifier x -> s
        | _ when s.StartsWith(dq) && s.EndsWith(dq) -> s
        | _ -> quote s

    let deQuoteOnDemand(s:string) =
        match Regex.Match(s, "^\"([^\"]*)\"$") with
        | m when m.Success -> m.Groups.[1].Value
        | _ -> s

    let unwrapString (s:string) (e:string) (str:string) =
        if str.StartsWith(s) && str.EndsWith(e) then str.Substring(s.Length, str.Length - s.Length - e.Length) else str

    let deQuote = unwrapString dq dq

    let combine (separator:string) (nameComponents:string seq) =
        nameComponents 
        |> List.ofSeq 
        |> function
           | [] -> failwithlog "ERROR"
           | [ n ] -> n
           | ns -> ns |> Seq.map quoteOnDemand |> String.concat separator

// Extension methods for string manipulation
[<Extension>]
type TextExt =
    [<Extension>] static member IsValidIdentifier (identifier:string) = isValidIdentifier identifier
    [<Extension>] static member IsValidIdentifier (identifier:char) = isValidIdentifier (identifier.ToString())
    [<Extension>] static member IsQuotationRequired (identifier:string) = not (isValidIdentifier identifier)
    [<Extension>] static member IsQuotationRequired (identifier:char) = not (isValidIdentifier (identifier.ToString()))
    [<Extension>] static member QuoteOnDemand (identifier:string) = quoteOnDemand identifier
    [<Extension>] static member DeQuoteOnDemand (identifier:string) = deQuoteOnDemand identifier
    [<Extension>] static member Combine (nameComponents:string seq, [<Optional; DefaultParameterValue(".")>] separator) = combine separator nameComponents
    [<Extension>] static member CombineQuoteOnDemand (nameComponents:string seq, [<Optional; DefaultParameterValue(".")>] separator) = combine separator (nameComponents.Select(quoteOnDemand))
