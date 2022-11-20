namespace Engine.Parser.FS

open System.Collections.Generic
open System.Diagnostics

open Engine.Core
open Engine.Common.FS
open type Engine.Parser.dsParser
open Antlr4.Runtime
open type DsParser

[<AutoOpen>]
module ContextInformationModule =
    [<DebuggerDisplay("{FullName}({ContextType.Name})")>]
    type NamedContextInformation = {
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

    type NamedContextInformation with
        static member Create(parserRuleContext:ParserRuleContext, system:string option, flow, parenting, names) =
            {   ContextType = parserRuleContext.GetType();
                System = system; Flow = flow; Parenting = parenting; Names = names }

        static member CreateFullNameComparer() =
            {   new IEqualityComparer<NamedContextInformation> with
                    member _.Equals(x:NamedContextInformation, y:NamedContextInformation) = x.FullName = y.FullName
                    member _.GetHashCode(x:NamedContextInformation) = x.FullName.GetHashCode() }

        // 단일 이름인 경우, Combine() 을 수행하면 특수 기호 포함된 경우, quotation 부호가 강제로 붙어서 향후의 처리에 문제가 되어서 따로 처리
        member x.GetRawName() = getRawName x.Names false
            //match x.Names.Length with
            //| 1 -> x.Names[0]
            //| _ -> x.Names.Combine()
    let getContextInformation(parserRuleContext:ParserRuleContext) =      // collectUpwardContextInformation
        let ctx = parserRuleContext
        let system  = LoadedSystemName.OrElse(tryGetSystemName ctx)
        let flow      = tryFindFirstAncestor<FlowBlockContext>(ctx, true).Bind(tryFindIdentifier1FromContext)
        let parenting = tryFindFirstAncestor<ParentingBlockContext>(ctx, true).Bind(tryFindIdentifier1FromContext)
        let ns        = collectNameComponents(ctx).ToFSharpList()
        NamedContextInformation.Create(ctx, system, flow, parenting, ns)

    type ObjectContextInformation = {
        System   : DsSystem
        Flow     : Flow option
        Parenting: Real option
        NamedContextInformation: NamedContextInformation
    } with
        static member Create(system:DsSystem, parserRuleContext) =
            let ci = getContextInformation parserRuleContext
            assert(system.Name = ci.System.Value)
            let flow = ci.Flow.Bind(fun fn -> system.TryFindFlow(fn))
            let parenting =
                option {
                    let! flow = flow
                    let! parentingName = ci.Parenting
                    return! flow.Graph.TryFindVertex<Real>(parentingName)
                }
            {   System = system; Flow = flow; Parenting = parenting; NamedContextInformation = ci }

[<AutoOpen>]
module DsParserHelperModule =

    let choiceParentWrapper (ci:NamedContextInformation) (flow:Flow option) (parenting:Real option) =
        match ci.Parenting with
        | Some prnt -> Real parenting.Value
        | None -> Flow flow.Value
    let tryFindParentWrapper (system:DsSystem) (ci:NamedContextInformation) =
        option {
            let! flowName = ci.Flow
            match ci.Tuples with
            | Some sys, Some flow, Some parenting, _ ->
                let! real = tryFindReal system flow parenting
                return Real real
            | Some sys, Some flow, None, _ ->
                let! f = tryFindFlow system flowName
                return Flow f
            | _ -> failwith "ERROR"
        }

    let tryFindToken (system:DsSystem) (ctx:CausalTokenContext):Vertex option =
        let ci = getContextInformation ctx
        option {
            let! parentWrapper = tryFindParentWrapper system ci
            let graph = parentWrapper.GetGraph()
            match ci.Names with
            | ofn::ofrn::[] ->      // of(r)n: other flow (real) name
                return! graph.TryFindVertex(ci.Names.Combine())
            | callOrAlias::[] ->
                return! graph.TryFindVertex(callOrAlias)
            | _ ->
                failwith "ERROR"
        }

