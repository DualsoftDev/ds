namespace Engine.Parser.FS

open System.Collections.Generic
open System.Diagnostics

open Engine.Core
open Engine.Common.FS
open type Engine.Parser.dsParser
open Antlr4.Runtime.Tree

[<AutoOpen>]
module ContextInformationModule =
    [<DebuggerDisplay("{FullName}({ContextType.Name})")>]
    type ContextInformation = {
        System   : string option
        Flow     : string option
        Parenting: string option
        Names    : string list
        ContextType:System.Type
    } with
        member x.Tuples = x.System, x.Flow, x.Parenting, x.Names
        member x.NameComponents = [
            if x.System.IsSome then yield x.System.Value
            if x.Flow.IsSome then yield x.Flow.Value
            if x.Parenting.IsSome then yield x.Parenting.Value
            if x.ContextType.IsOneOf(
                  typedefof<SystemContext>
                , typedefof<FlowBlockContext>
                , typedefof<ParentingBlockContext>) then
                    ()
            else
                yield! x.Names
        ]
        member x.FullName = x.NameComponents.ToArray().Combine()

    type ContextInformation with
        static member Create(parserRuleContext, system:string option, flow, parenting, names) =
            {   ContextType = parserRuleContext.GetType();
                System = system; Flow = flow; Parenting = parenting; Names = names }

        static member CreateFullNameComparer() =
            {   new IEqualityComparer<ContextInformation> with
                    member _.Equals(x:ContextInformation, y:ContextInformation) = x.FullName = y.FullName
                    member _.GetHashCode(x:ContextInformation) = x.FullName.GetHashCode() }
        member x.GetRawName() =
            match x.Names.Length with
            | 1 -> x.Names[0]
            | _ -> x.Names.Combine()

