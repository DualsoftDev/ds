// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Runtime.CompilerServices
open System
open System.Linq
open System.Globalization
open System.Collections.Generic

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
    let internal combine (separator:string) (nameComponents:string seq) = nameComponents |> Seq.map quoteOnDemand |> String.concat separator
    type NameComponents = string[]

    // 이름이 필요한 객체
    [<AbstractClass>]
    type Named(name) =
        let mutable name = name
        interface INamed with
            //member _.Name with get () = name //and set (v) = name <- v
            member x.Name with get () = x.Name

        member val Name : string = name with get, set
        abstract ToText : unit -> string
        default x.ToText() = name
     
    let isStringArrayEqaul (ns1:string seq, ns2:string seq) = Enumerable.SequenceEqual(ns1, ns2)

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
        new IEqualityComparer<NameComponents> with
            member _.Equals(x:NameComponents, y:NameComponents) = isStringArrayEqaul(x, y)
            member _.GetHashCode(x:NameComponents) = x.Average(fun s -> s.GetHashCode()) |> int
    }

    type FqdnObject(name:string, parent:IQualifiedNamed) =
        inherit Named(name)
        interface IQualifiedNamed with
            member val NameComponents = [| yield! parent.NameComponents; name |]
            member x.QualifiedName = combine "." x.NameComponents
        member x.Name with get() = (x :> INamed).Name
        member x.NameComponents = (x :> IQualifiedNamed).NameComponents
        member x.QualifiedName = (x :> IQualifiedNamed).QualifiedName



[<Extension>]
type NameUtil =
    /// DS 문법에서 사용하는 identifier (Segment, flow, call 등의 이름)가 적법한지 검사.
    /// 적법하지 않으면 double quote 로 감싸주어야 한다.
    [<Extension>] static member IsValidIdentifier (identifier:string) = isValidIdentifier identifier
    [<Extension>] static member IsQuotationRequired (identifier:string) = isValidIdentifier(identifier) |> not
    [<Extension>] static member QuoteOnDemand (identifier:string) = quoteOnDemand identifier                       
    [<Extension>] static member DeQuoteOnDemand (identifier:string) = deQuoteOnDemand identifier                       
    [<Extension>] static member Combine (nameComponents:string seq) = combine "." nameComponents 
    [<Extension>] static member Combine (nameComponents:string seq, separator) = combine separator nameComponents 
    [<Extension>] static member IsStringArrayEqaul (ns1:string seq, ns2:string seq) = isStringArrayEqaul(ns1, ns2)
    [<Extension>] static member CreateNameComparer() = nameComparer()
    [<Extension>] static member CreateNameComponentsComparer() = nameComponentsComparer()
    

    [<Extension>]
    static member FindWithName (namedObjects:#INamed seq, name:string) =
        namedObjects.FirstOrDefault(fun obj -> obj.Name = name)
    [<Extension>]
    static member FindWithNameComponents (namedObjects:#IQualifiedNamed seq, nameComponents:NameComponents) =
        namedObjects.FirstOrDefault(fun obj -> Enumerable.SequenceEqual(obj.NameComponents, nameComponents))
    
        