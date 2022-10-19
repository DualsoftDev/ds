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
    let rec spitCall (child:Vertex) : SpitResults =
        [|
            yield SpitResult(child, child.NameComponents)
        |]
    and spitSegment (segment:Real) : SpitResults =
        [|
            yield SpitResult(segment, segment.NameComponents)
            for ch in segment.Graph.Vertices do
                yield! spit(ch)
        |]
    and spitAlias (segmentAlias:Alias) : SpitResults =
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
                for itf in system.ApiItems do
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
        | :? Real     as r -> spitSegment r
        | :? Call     as c -> spitCall c
        | :? Alias    as a -> spitAlias a
        | _ -> failwith $"ERROR: Unknown type {obj}"
    ()

open SpitModuleHelper

[<Extension>]
type SpitModule =
    [<Extension>] static member Spit (model:Model)     = spitModel model
    [<Extension>] static member Spit (system:DsSystem) = spitSystem system
    [<Extension>] static member Spit (flow:Flow)       = spitFlow flow
    [<Extension>] static member Spit (segment:Real)    = spitSegment segment
    [<Extension>] static member Spit (call:Call)       = spitCall call
    [<Extension>] static member Spit (alias:Alias)     = spitAlias alias

