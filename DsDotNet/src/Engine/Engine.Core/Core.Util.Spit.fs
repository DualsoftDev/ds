namespace Engine.Core

open System.Runtime.CompilerServices
open System.Linq
open System.Diagnostics

module SpitModuleHelper =
    type SpitOnlyAlias = { AliasKey:NameComponents; Mnemonic:NameComponents }

    type SpitCoreType =
        | SpitModel    of Model
        | SpitDsSystem of DsSystem
        | SpitFlow     of Flow
        | SpitReal     of Real
        | SpitCall     of Call
        | SpitAlias    of Alias
        | SpitOnlyAlias of SpitOnlyAlias
        | SpitApiItem   of ApiItem
        | SpitVariable  of Variable
        | SpitCommand   of Command
        | SpitObserve   of Observe

    [<DebuggerDisplay("Obj={SpitObj}, Names={NameComponents}")>]
    type SpitResult =
        { SpitObj:SpitCoreType; NameComponents:NameComponents }
        static member Create(core, nameComponents) = {SpitObj = core; NameComponents = nameComponents}

    type SpitResults = SpitResult[]

    let rec spitCall (call:Call) : SpitResults =
        [| yield SpitResult.Create(SpitCall call, call.NameComponents) |]
    and spitSegment (segment:Real) : SpitResults =
        [|
            yield SpitResult.Create(SpitReal segment, segment.NameComponents)
            for ch in segment.Graph.Vertices do
                yield! spit(ch)
        |]
    and spitAlias (alias:Alias) : SpitResults =
        [| yield SpitResult.Create(SpitAlias alias, alias.NameComponents) |]
    and spitFlow (flow:Flow) : SpitResults =
        [|
            let fns = flow.NameComponents
            yield SpitResult.Create(SpitFlow flow, fns)
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
                let alias = { AliasKey = aliasKey2; Mnemonic = mnemonicFqdn}
                yield SpitResult.Create(SpitOnlyAlias alias, aliasKey2)       // key -> alias : [ My.Flow.Ap1, A."+";  My.Flow.Main2, My.Flow.Main; ...]
                yield SpitResult.Create(SpitOnlyAlias alias, mnemonicFqdn)    // mne -> alias
        |]
    and spitSystem (system:DsSystem) : SpitResults =
        [|
            yield SpitResult.Create(SpitDsSystem system, system.NameComponents)
            for flow in system.Flows do
                yield! spit(flow)
                for itf in system.ApiItems do
                    yield SpitResult.Create(SpitApiItem itf, itf.NameComponents)
        |]
    and spitModel (model:Model) : SpitResults =
        [|
            yield SpitResult.Create(SpitModel model, [||])
            for sys in model.Systems do
                yield! spit(sys)

            for x in model.Variables do
                yield SpitResult.Create(SpitVariable x, [| x.Name |] )
            for x in model.Commands do
                yield SpitResult.Create(SpitCommand x, [| x.Name |] )
            for x in model.Observes do
                yield SpitResult.Create(SpitObserve x, [| x.Name |] )
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
    //[<Extension>] static member Spit (alias:Alias)     = spitAlias alias
    
    [<Extension>]
    static member GetCore (spit:SpitResult):obj = 
        match spit.SpitObj with
        | SpitModel     c -> c
        | SpitDsSystem  c -> c
        | SpitFlow      c -> c
        | SpitReal      c -> c
        | SpitCall      c -> c
        | SpitAlias     c -> c
        | SpitOnlyAlias c -> c
        | SpitApiItem   c -> c
        | SpitVariable  c -> c
        | SpitCommand   c -> c
        | SpitObserve   c -> c
        


