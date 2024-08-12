// Copyright (c) Dualsoft  All Rights Reserved.
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

    /// Need quoteOnDemand use combineQuoteOnDemand
    let combine (separator:string) (nameComponents:string seq) =
        nameComponents
        |> List.ofSeq
        |> function
           | [] -> failwithlog "ERROR"
           | [ n ] -> n
           | ns -> ns |> String.concat separator

    let combineQuoteOnDemand (separator:string) (nameComponents:string seq) =
        combine separator (nameComponents.Select(quoteOnDemand))

    let combineDequoteOnDemand (separator:string) (nameComponents:string seq) =
        combine separator (nameComponents.Select(deQuoteOnDemand)) //|> tee(fun str -> assert(!! str.Contains("\"")))       --- 실제 이름에 dquote 가 포함된 경우는 assert false

    let split (separator: string) (input: string) : string[] =
        if String.IsNullOrEmpty(input) then
            failwith "ERROR: Input string is null or empty"
        input.Split([| separator |], StringSplitOptions.None)

    //let isValidIdentifier (charStr: string) =
    //// Assuming a placeholder function for checking valid identifier characters
    //    Char.IsLetterOrDigit(charStr, 0) || charStr = "_"

    // Function to convert a device name to a valid storage name
    let validStorageName devName =
        let processedName =
            devName
            |> Seq.map (fun c ->
                if Char.IsNumber(c) || isValidIdentifier(c.ToString()) then
                    c.ToString()
                else
                    "_")
            |> String.concat ""

        // Ensure the first character is not a number
        if Char.IsNumber(processedName.[0]) then
            "_" + processedName
        else
            processedName

    ///  "/";"\\ 경로기호 금지, '__' Flow 구분자 금지, '.' Quatation 구분자 금지
    let invalidCharsSystem = [|"/"; "\\"; "__"; "."|]

    let checkFlowName (name: string) =
        if invalidCharsSystem |> Seq.exists (fun f -> name.Contains(f)) then
            let errChars = String.Join("  ", invalidCharsSystem)
            failwith $"이름에 금지된 문자열이 있습니다. \r\n{name} \r\n금지항목 : ( {errChars} )"


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
    [<Extension>] static member CombineQuoteOnDemand (nameComponents:string seq, [<Optional; DefaultParameterValue(".")>] separator) = combineQuoteOnDemand separator nameComponents
    [<Extension>] static member CombineDequoteOnDemand (nameComponents:string seq, [<Optional; DefaultParameterValue(".")>] separator) = combineDequoteOnDemand separator nameComponents
    /// fqdn -> component []
    [<Extension>] static member SplitToFqdn (fqdn:string, [<Optional; DefaultParameterValue(".")>] separator) = split separator fqdn
    [<Extension>] static member SplitToFqdnDeQuoteOnDemand (fqdn:string) = fqdn.DeQuoteOnDemand() |> split "." |> Seq.map(fun f->f.DeQuoteOnDemand())

