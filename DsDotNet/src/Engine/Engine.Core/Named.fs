// Copyright (c) Dual Inc.  All Rights Reserved.
namespace rec Engine.Core

open System.Runtime.CompilerServices
open System
open System.Diagnostics
open System.Linq
open System.Globalization
open System.Collections.Generic
open System.Runtime.InteropServices
open Engine.Common.FS


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

    let internal combine (separator:string) (nameComponents:string seq) =
        let nameComponents = nameComponents |> List.ofSeq
        match nameComponents with
        | [] -> failwith "ERROR"
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
        | p::q::[] -> combine "." fqdn'
        | _ -> failwith "ERROR"

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

    let getRelativeName(referencePath:Fqdn) (fqdn:Fqdn) = getRelativeNames referencePath fqdn |> Seq.map(quoteOnDemand) |> (combine ".")

    type FqdnObject(name:string, parent:IQualifiedNamed) =
        inherit Named(name)
        interface IQualifiedNamed with
            member x.NameComponents = [| yield! parent.NameComponents; x.Name |]
            member x.QualifiedName = combine "." x.NameComponents
        member x.Name with get() = (x :> INamed).Name and set(v) = (x :> INamed).Name <- v
        member x.NameComponents = (x :> IQualifiedNamed).NameComponents
        member x.QualifiedName = (x :> IQualifiedNamed).QualifiedName
        abstract member GetRelativeName: Fqdn -> string
        default x.GetRelativeName(referencePath:Fqdn) =
            getRelativeName referencePath x.NameComponents



[<Extension>]
type NameUtil =
    /// DS 문법에서 사용하는 identifier (Segment, flow, call 등의 이름)가 적법한지 검사.
    /// 적법하지 않으면 double quote 로 감싸주어야 한다.
    [<Extension>] static member IsValidIdentifier (identifier:string) = isValidIdentifier identifier
    [<Extension>] static member IsQuotationRequired (identifier:string) = isValidIdentifier(identifier) |> not
    [<Extension>] static member QuoteOnDemand (identifier:string) = quoteOnDemand identifier
    [<Extension>] static member DeQuoteOnDemand (identifier:string) = deQuoteOnDemand identifier
    [<Extension>] static member Combine (nameComponents:string seq, [<Optional; DefaultParameterValue(".")>]separator) = combine separator nameComponents
    [<Extension>] static member CreateNameComparer() = nameComparer()
    [<Extension>] static member CreateNameComponentsComparer() = nameComponentsComparer()
    [<Extension>] static member GetRelativeName(fqdn:Fqdn, referencePath:Fqdn) = getRelativeName referencePath fqdn


    [<Extension>]
    static member FindWithName (namedObjects:#INamed seq, name:string) =
        namedObjects.FirstOrDefault(fun obj -> obj.Name = name)
    [<Extension>]
    static member FindWithNameComponents (namedObjects:#IQualifiedNamed seq, nameComponents:Fqdn) =
        namedObjects.FirstOrDefault(fun obj -> obj.NameComponents = nameComponents)

