// Copyright (c) Dual Inc.  All Rights Reserved.
namespace rec Engine.Core

open System.Runtime.CompilerServices
open System
open System.Diagnostics
open System.Linq
open System.Globalization
open System.Collections.Generic
open System.Runtime.InteropServices
open Dual.Common.Core.FS
open System.Text.RegularExpressions
open System.ComponentModel


[<AutoOpen>]
module TextUtil =
    let isHangul(ch:char) = Char.GetUnicodeCategory(ch) = UnicodeCategory.OtherLetter;
    let isValidStart(ch:char) = ch = '_' || Char.IsLetter(ch) || isHangul(ch);
    let (|IsHangul|) x = if isHangul x then Some x else None
    let (|IsValidStart|) x = if isValidStart x then Some x else None
    let internal isValidIdentifier (identifier:string) =
        let isValid(ch:char) = isValidStart(ch) || Char.IsDigit(ch);
        if (identifier = null || identifier = "") then
            ArgumentNullException() |> raise

        if (identifier = "_") then
            true
        else
            let chars = identifier.ToCharArray()
            let first = chars[0]
            isValidStart(first) && chars.Skip(1).All(isValid)
    let (|ValidIdentifier|) x = if isValidIdentifier x then Some x else None
    let private dq = "\""
    let internal quote(s:string) = $"{dq}{s}{dq}"
    let internal quoteOnDemand(s:string) =
        let pattern = @$".*\.\{dq}.*\{dq}(\.*)?"
        match s with
        | ValidIdentifier x when x.IsSome -> s

        | ( RegexMatches "^\"(.*)\"$"   // "#specidal#"
          | RegexMatches pattern ) -> s // xxx."yyy"

        | _ -> quote s

    let internal deQuoteOnDemand(s:string) =
        match s with
        | RegexPattern "^\"([^\"]*)\"$" [inner] -> inner
        | _ -> s

    let unwrapString (s:string) (e:string) (str:string) =
        if str.StartsWith(s) && str.EndsWith(e) then
            let s, e = s.Length, str.Length - (s.Length + e.Length)
            str[s..e]
        else
            str
    /// doublequote(") 로 시작하고, doublequote 로 끝나면, 앞뒤의 double quote 제거한 string 을 반환.  그렇지 않으면 원본 string 반환
    let deQuote = unwrapString dq dq

    let internal combine (separator:string) (nameComponents:string seq) =
        let nameComponents = nameComponents |> List.ofSeq
        match nameComponents with
        | [] -> failwithlog "ERROR"
        | n::[] -> n
        | ns -> ns |> Seq.map quoteOnDemand |> String.concat separator

    /// Fully Qualified Domain Name: string[] for "A.B.C"
    type Fqdn = string[]
    let (|Fqdn|) xs = Array.ofSeq xs

    // 이름이 필요한 객체
    [<AbstractClass>]
    [<DebuggerDisplay("{Name}")>]
    type Named(name) =
        interface INamed with
            member x.Name with get () = x.Name and set(v) = x.Name <- v

        member val Name : string = name with get, set
        abstract ToText : unit -> string
        default x.ToText() = x.Name

    let internal nameComparer<'T when 'T:> INamed>() = {
        new IEqualityComparer<'T> with
            member _.Equals(x:'T, y:'T) = x.Name = y.Name
            member _.GetHashCode(x) = x.Name.GetHashCode()
    }

    let internal createNamedHashSet<'T when 'T:> INamed>() =
        new HashSet<'T>(Seq.empty<'T>, nameComparer<'T>())

    let internal qualifiedNameComparer<'T when 'T:> IQualifiedNamed>() = {
        new IEqualityComparer<'T> with
            member _.Equals(x:'T, y:'T) = x.QualifiedName = y.QualifiedName
            member _.GetHashCode(x) = x.QualifiedName.GetHashCode()
    }

    let internal createQualifiedNamedHashSet<'T when 'T:> IQualifiedNamed>() =
        new HashSet<'T>(Seq.empty<'T>, qualifiedNameComparer<'T>())


    let internal nameComponentsComparer() = {
        new IEqualityComparer<Fqdn> with
            member _.Equals(x:Fqdn, y:Fqdn) = x = y
            member _.GetHashCode(x:Fqdn) = x.Average(fun s -> s.GetHashCode()) |> int
    }

    // 단일 이름인 경우, Combine() 을 수행하면 특수 기호 포함된 경우, quotation 부호가 강제로 붙어서 향후의 처리에 문제가 되어서 따로 처리
    let getRawName (fqdn:string seq) (quoteOnSingle:bool) =
        let fqdn' = fqdn |> List.ofSeq
        match fqdn' with
        | n::[] when quoteOnSingle -> quoteOnDemand n
        | n::[] -> n
        | _p::_q::[] -> combine "." fqdn'
        | _ -> failwithlog "ERROR"

    let getRelativeNames(referencePath:Fqdn) (fqdn:Fqdn) =
        let rec countSameStartElements (FList(xs)) (FList(ys)) =
            let rec helper xs ys =
                match xs, ys with
                | x::xx, y::yy when x = y -> 1 + (helper xx yy)
                | _ -> 0
            helper xs ys
        let numSameStart = countSameStartElements referencePath fqdn
        let relativeNameComponents = fqdn.Skip(numSameStart).ToFSharpList()
        assert(relativeNameComponents.NonNullAny())
        relativeNameComponents

    // '_' 는 dsText에서 입력없음 의미 address 입력시 '_' 는 "" 입력없음으로 변환
    let replaceSkipAddress(addr:string) = if addr = "_" then "" else addr
    let getRelativeName(referencePath:Fqdn) (fqdn:Fqdn) = getRelativeNames referencePath fqdn |> Seq.map(quoteOnDemand) |> (combine ".")

    type FqdnObject(name:string, parent:IQualifiedNamed) =
        inherit Named(name)
        interface IQualifiedNamed with
            member x.NameComponents = [| yield! parent.NameComponents; x.Name |]
            member x.QualifiedName = combine "." x.NameComponents
        member x.Name with get() = (x :> INamed).Name and set(v) = (x :> INamed).Name <- v
        [<Browsable(false)>]
        member x.NameComponents = (x :> IQualifiedNamed).NameComponents
        [<Browsable(false)>]
        member x.QualifiedName = (x :> IQualifiedNamed).QualifiedName
        abstract member GetRelativeName: Fqdn -> string
        default x.GetRelativeName(referencePath:Fqdn) =
            getRelativeName referencePath x.NameComponents
        [<Browsable(false)>]
        member val TagManager = getNull<ITagManager>() with get, set


[<Extension>]
type NameUtil =
    /// DS 문법에서 사용하는 identifier (Segment, flow, call 등의 이름)가 적법한지 검사.
    /// 적법하지 않으면 double quote 로 감싸주어야 한다.
    [<Extension>] static member IsValidIdentifier (identifier:string) = isValidIdentifier identifier
    [<Extension>] static member IsValidIdentifier (identifier:char) = isValidIdentifier (identifier.ToString())
    [<Extension>] static member IsQuotationRequired (identifier:string) = isValidIdentifier(identifier) |> not
    [<Extension>] static member IsQuotationRequired (identifier:char) = isValidIdentifier(identifier.ToString()) |> not
    [<Extension>] static member QuoteOnDemand (identifier:string) = quoteOnDemand identifier
    [<Extension>] static member DeQuoteOnDemand (identifier:string) = deQuoteOnDemand identifier
    [<Extension>] static member Combine (nameComponents:string seq, [<Optional; DefaultParameterValue(".")>]separator) = combine separator nameComponents
    [<Extension>] static member CreateNameComparer() = nameComparer()
    [<Extension>] static member CreateNameComponentsComparer() = nameComponentsComparer()
    [<Extension>] static member GetRelativeName(fqdn:Fqdn, referencePath:Fqdn) = getRelativeName referencePath fqdn

