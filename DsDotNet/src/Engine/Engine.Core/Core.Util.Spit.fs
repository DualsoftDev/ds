namespace Engine.Core

open System.Runtime.CompilerServices
open System.Linq
open System.Diagnostics
open Engine.Common.FS

[<AutoOpen>]
module SpitModuleHelper =
    type SpitOnlyAlias = { AliasKey:Fqdn; Mnemonic:Fqdn; FlowFqdn:Fqdn }

    type SpitCoreType =
        | SpitDsSystem  of DsSystem
        | SpitExternalSystem of ExternalSystem
        | SpitDevice of Device
        | SpitFlow      of Flow
        | SpitCall      of Call
        | SpitOnlyAlias of SpitOnlyAlias
        | SpitApiItem   of ApiItem4Export
        | SpitVariable  of Variable
        | SpitCommand   of Command
        | SpitObserve   of Observe

        | SpitVertexReal  of Real
        | SpitVertexCall  of VertexCall
        | SpitVertexAlias of VertexAlias
        | SpitVertexOtherFlowRealCall of VertexOtherFlowRealCall

    [<DebuggerDisplay("{NameComponents} = {SpitObj.GetType().Name}")>]
    type SpitResult =
        { SpitObj:SpitCoreType; NameComponents:Fqdn }
        static member Create(core, nameComponents) = {SpitObj = core; NameComponents = nameComponents}
        override x.ToString() = $"Obj={x.SpitObj}, Names={x.NameComponents.Combine()}"

    type SpitResults = SpitResult[]

    //let spitCall (call:Call) : SpitResults =
    //    [| yield SpitResult.Create(SpitCall call, call.NameComponents) |]
    let rec spitDevice (device:Device) : SpitResults =
        [| yield SpitResult.Create(SpitDevice device, device.NameComponents) |]
    and spitExternalSystem (externalSystem:ExternalSystem) : SpitResults =
        [| yield SpitResult.Create(SpitExternalSystem externalSystem, externalSystem.NameComponents) |]

    and spitSegment (segment:Real) : SpitResults =
        [|
            yield SpitResult.Create(SpitVertexReal segment, segment.NameComponents)
            for ch in segment.Graph.Vertices do
                yield! spit(ch)
        |]
    and spitIndirect (indirect:Indirect) : SpitResults =
        let spitCore =
            match indirect with
            | :? VertexCall as v -> SpitVertexCall v
            | :? VertexAlias as v -> SpitVertexAlias v
            | :? VertexOtherFlowRealCall as v -> SpitVertexOtherFlowRealCall v
            | _ -> failwith "ERROR"

        [| yield SpitResult.Create(spitCore, indirect.NameComponents) |]
    and spitFlow (flow:Flow) : SpitResults =
        [|
            let fns = flow.NameComponents
            yield SpitResult.Create(SpitFlow flow, fns)
            for flowVertex in flow.Graph.Vertices do
                yield! spit(flowVertex)

            (*
                 A."+" = { Ap1; Ap2; }    : alias=A."+", mnemonics = [Ap1; Ap2;]
                 Main = { Main2; }
             *)
            for { AliasTarget = targetWrapper; Mnemonincs = mnes } in flow.AliasDefs do
                noop()
                //for m in mnes do
                //    let mnemonicFqdn = [| m |]
                //    let alias = { AliasKey = aliasKey; Mnemonic = mnemonicFqdn; FlowFqdn = flow.NameComponents }
                //    yield SpitResult.Create(SpitOnlyAlias alias, aliasKey)        // key -> alias : [ My.Flow.Ap1, A."+";  My.Flow.Main2, My.Flow.Main; ...]
                //    yield SpitResult.Create(SpitOnlyAlias alias, mnemonicFqdn)    // mne -> alias
        |]
    and spitSystem (system:DsSystem) : SpitResults =
        [|
            yield SpitResult.Create(SpitDsSystem system, system.NameComponents)
            for flow in system.Flows do
                yield! spit(flow)
                for api in system.ApiItems4Export -> SpitResult.Create(SpitApiItem api, api.NameComponents)

            for dev in system.Devices do
                yield! spit(dev)

            for x in system.Variables -> SpitResult.Create(SpitVariable x, [| x.Name |] )
            for x in system.Commands ->  SpitResult.Create(SpitCommand x,  [| x.Name |] )
            for x in system.Observes ->  SpitResult.Create(SpitObserve x,  [| x.Name |] )
        |]
    and spit(obj:obj) : SpitResults =
        match obj with
        | :? DsSystem as s -> spitSystem s
        | :? Flow     as f -> spitFlow f
        | :? Real     as r -> spitSegment r
        //| :? Call     as c -> spitCall c
        | :? Indirect as v -> spitIndirect v
        | :? Device   as d -> spitDevice d
        | :? ExternalSystem as e -> spitExternalSystem e
        | _ -> failwith $"ERROR: Unknown type {obj}"


open SpitModuleHelper
open Engine.Common.FS

[<Extension>]
type SpitModule =
    [<Extension>] static member Spit (system:DsSystem) = spitSystem system
    [<Extension>] static member Spit (flow:Flow)       = spitFlow flow
    [<Extension>] static member Spit (segment:Real)    = spitSegment segment
    //[<Extension>] static member Spit (call:Call)       = spitCall call
    [<Extension>] static member Dump (spits:SpitResult[]) = spits.Select(toString).JoinWith("\r\n")

    [<Extension>]
    static member GetCore (spit:SpitResult):obj =
        match spit.SpitObj with
        | SpitDsSystem  c -> c
        | SpitDevice  c -> c
        | SpitExternalSystem c -> c
        | SpitFlow      c -> c
        | SpitCall      c -> c
        | SpitOnlyAlias c -> c
        | SpitApiItem   c -> c
        | SpitVariable  c -> c
        | SpitCommand   c -> c
        | SpitObserve   c -> c
        | SpitVertexReal      c -> c
        | SpitVertexAlias     c -> c
        | SpitVertexCall c -> c
        | SpitVertexOtherFlowRealCall c -> c



