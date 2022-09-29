namespace Engine.Core

open System.Runtime.CompilerServices
open System.Linq
open System.Diagnostics

module SpitModuleHelper =
    type SpitObjAlias(key:NameComponents, mnemonic:NameComponents) =
        member x.Key = key
        member x.Mnemonic = mnemonic

    [<DebuggerDisplay("Obj={Obj}, Names={NameComponents}")>]
    type SpitResult(obj:obj, nameComponents:NameComponents) =
        member val Obj = obj
        member val NameComponents = nameComponents

    type SpitResults = SpitResult[]
    let rec spitChild (child:Child) : SpitResults =
        [|
            yield SpitResult(child, child.NameComponents)
        |]
    and spitSegment (segment:Segment) : SpitResults =
        [|
            yield SpitResult(segment, segment.NameComponents)
            for ch in segment.Graph.Vertices do
                yield! spit(ch)
        |]
    and spitSegmentAlias (segmentAlias:SegmentAlias) : SpitResults =
        [|
            yield SpitResult(segmentAlias, segmentAlias.NameComponents)
        |]
    and spitFlow (flow:Flow) : SpitResults =
        [|
            let fns = flow.NameComponents
            yield SpitResult(flow, fns)
            for flowVertex in flow.Graph.Vertices do
                yield! spit(flowVertex)

            // A."+" = { Ap1; Ap2; }    : alias=A."+", mnemonics = [Ap1; Ap2;]
            // Main = { Main2; }
            for KeyValue(aliasKey, mnemonics) in flow.AliasMap do
            for m in mnemonics do
                let aliasKey2 =
                    match aliasKey.Length with
                    | 2 -> aliasKey            // A."+"
                    | 1 -> fns.Append(aliasKey[0]).ToArray()   // My.Flow + Main
                    | _ -> failwith "ERROR"

                let mnemonicFqdn = [| yield! fns; m |]
                let alias = SpitObjAlias(aliasKey2, mnemonicFqdn)
                yield SpitResult(alias, aliasKey2)       // key -> alias : [ My.Flow.Ap1, A."+";  My.Flow.Main2, My.Flow.Main; ...]
                yield SpitResult(alias, mnemonicFqdn)    // mne -> alias
        |]
    and spitSystem (system:DsSystem) : SpitResults =
        [|
            yield SpitResult(system, system.NameComponents)
            for flow in system.Flows do
                yield! spit(flow)
            if system.Api <> null then
                for itf in system.Api.Items do
                    yield SpitResult(itf, itf.NameComponents)
        |]
    and spitModel (model:Model) : SpitResults =
        [|
            yield SpitResult(model, [||])
            for sys in model.Systems do
                yield! spit(sys)
        |]
    and spit(obj:obj) : SpitResults =
        match obj with
        | :? Model    as m -> spitModel m
        | :? DsSystem as s -> spitSystem s
        | :? Flow     as f -> spitFlow f
        | :? Segment  as s -> spitSegment s
        | :? Child    as c -> spitChild c
        | :? SegmentAlias  as s -> spitSegmentAlias s
    ()

open SpitModuleHelper

[<Extension>]
type SpitModule =
    [<Extension>] static member Spit (model:Model)     = spitModel model
    [<Extension>] static member Spit (system:DsSystem) = spitSystem system
    [<Extension>] static member Spit (flow:Flow)       = spitFlow flow
    [<Extension>] static member Spit (segment:Segment) = spitSegment segment


[<AutoOpen>]
module internal EdgeModule =
    let getEdgeType(operator:string) =
        match operator with
        | ">"   -> EdgeType.Default
        | ">>"  -> EdgeType.Strong
        | "|>"  -> EdgeType.Reset
        | "||>" -> EdgeType.Reset  ||| EdgeType.Strong

        | "<"   -> EdgeType.Reversed
        | "<<"  -> EdgeType.Reversed ||| EdgeType.Strong
        | "<|"  -> EdgeType.Reversed ||| EdgeType.Reset
        | "<||" -> EdgeType.Reversed ||| EdgeType.Reset  ||| EdgeType.Strong

        | "<|>" -> EdgeType.Reset  ||| EdgeType.Bidirectional
        | "<||>" -> EdgeType.Reset  ||| EdgeType.Strong ||| EdgeType.Bidirectional

        | _ -> failwith "ERROR"

    let createFlowEdges(flow:Flow, source:SegmentBase, target:SegmentBase, operator:string) =
        let eType = getEdgeType(operator)
        [|
            if eType.HasFlag(EdgeType.Bidirectional) then
                let single = eType &&& (~~~ EdgeType.Bidirectional)
                yield InFlowEdge.Create(flow, source, target, single)
                yield InFlowEdge.Create(flow, target, source, single ||| EdgeType.Reversed)
            else
                yield InFlowEdge.Create(flow, source, target, eType)
        |]

    let createChildEdges(segment:Segment, source:Child, target:Child, operator:string) =
        let eType = getEdgeType(operator)
        [|
            if eType.HasFlag(EdgeType.Bidirectional) then
                let single = eType &&& (~~~ EdgeType.Bidirectional)
                yield InSegmentEdge.Create(segment, source, target, single)
                yield InSegmentEdge.Create(segment, target, source, single ||| EdgeType.Reversed)
            else
                yield InSegmentEdge.Create(segment, source, target, eType)
        |]

[<Extension>]
type EdgeHelper =
    [<Extension>] static member CreateEdges(flow:Flow, source:SegmentBase, target:SegmentBase, operator:string) =
                    createFlowEdges(flow, source, target, operator)
    [<Extension>] static member CreateEdges(segment:Segment, source:Child, target:Child, operator:string) =
                    createChildEdges(segment, source, target, operator)
