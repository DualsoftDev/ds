// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Linq
open System.Collections.Generic
open System.ComponentModel
open System.Runtime.CompilerServices
open System.Diagnostics
open System.Runtime.InteropServices
open Dual.Common.Core.FS

[<AutoOpen>]
module FqdnImpl =

    /// Type definitions and Active Patterns
    type Fqdn = string[]
    let (|Fqdn|) xs = Array.ofSeq xs

    /// Helper Functions
    let internal replaceSkipAddress(addr:string) =
        if addr = "_" then "" else addr

    let internal getRawName (fqdn:string seq) (quoteOnSingle:bool) =
        let fqdnList = fqdn |> List.ofSeq
        match fqdnList with
        | [n] when quoteOnSingle -> quoteOnDemand n
        | [n] -> n
        | [_p; _q] -> combine "." fqdnList
        | _ -> failwith "ERROR"

    let internal getRelativeNames(referencePath:Fqdn) (fqdn:Fqdn) =
        let rec countSameStartElements xs ys =
            match xs, ys with
            | x::xx, y::yy when x = y -> 1 + countSameStartElements xx yy
            | _ -> 0
        let numSameStart = countSameStartElements (List.ofArray referencePath) (List.ofArray fqdn)
        fqdn.Skip(numSameStart).ToList()


    let internal getRelativeName(referencePath:Fqdn) (fqdn:Fqdn) =
        let relativeNames = getRelativeNames referencePath fqdn
    
        // Placeholder for quoteOnDemand. If it's defined elsewhere, you can remove this.
        let quoteOnDemand s = s // Placeholder: just returns the input string

        relativeNames |> Seq.map quoteOnDemand |> String.concat "."


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
    /// Named and FqdnObject Classes
    [<AbstractClass; DebuggerDisplay("{Name}")>]
    type Named(name) =
        interface INamed with
            member x.Name with get () = x.Name and set(v) = x.Name <- v

        member val Name : string = name with get, set
        abstract ToText : unit -> string
        default x.ToText() = x.Name

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
type FqdnExt =
    [<Extension>] static member Combine (nameComponents:string seq, [<Optional; DefaultParameterValue(".")>]separator) = combine separator nameComponents
    [<Extension>] static member CreateNameComparer() = nameComparer()
    [<Extension>] static member CreateNameComponentsComparer() = nameComponentsComparer()
    [<Extension>] static member GetRelativeName(fqdn:Fqdn, referencePath:Fqdn) = getRelativeName referencePath fqdn