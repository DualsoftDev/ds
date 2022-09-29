// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Collections.Generic
open System.Linq

[<AutoOpen>]
module CoreModule =
    type Model() =
        member val Systems = createNamedHashSet<DsSystem>()
        //member x.Cpus = x.Systems.Select(fun sys -> sys.Cpu)
        interface IQualifiedNamed with
            member val Name = null //failwith "ERROR"
            member val NameComponents = Array.empty<string>
            member x.QualifiedName = null  //failwith "ERROR"


    and DsSystem private (name:string, cpu:ICpu, model:Model) =
        inherit FqdnObject(name, model)

        //new (name, model) = DsSystem(name, null, model)
        member val Flows = createNamedHashSet<Flow>()
        //member val Api = new Api(this)
        member val Api:Api = null with get, set

        member _.Model = model
        member _.Cpu = cpu

        //시스템 버튼 소속 Flow 정보
        member val EmergencyButtons = ButtonDic()
        member val AutoButtons      = ButtonDic()
        member val StartButtons     = ButtonDic()
        member val ResetButtons     = ButtonDic()
        static member Create(name, cpu, model) =
            let system = DsSystem(name, cpu, model)
            model.Systems.Add(system) |> verify $"Duplicated system name [{name}]"
            system
         
    and Flow private(name:string, system:DsSystem) =
        inherit FqdnObject(name, system)
        member val Graph = Graph<IFlowVertex, InFlowEdge>()     // todo: IFlowVertex -> SegmentBase
        /// alias.target = [| mnemonic1; ... ; mnemonicn; |]
        member val AliasMap = Dictionary<NameComponents, HashSet<string>>(nameComponentsComparer())
        static member Create(name:string, system:DsSystem) =
            let flow = Flow(name, system)
            system.Flows.Add(flow) |> verify $"Duplicated flow name [{name}]"
            flow

    and [<AbstractClass>]
        SegmentBase (name:string, flow:Flow) =
            inherit FqdnObject(name, flow)
            interface IFlowVertex

    // normal segment : leaf, stem(parenting)
    and Segment private (name:string, flow:Flow) =
        inherit SegmentBase(name, flow)
        member val Graph = Graph<IChildVertex, InSegmentEdge>()
        member val Flow = flow
        static member Create(name, flow) =
            let segment = Segment(name, flow)
            flow.Graph.AddVertex(segment) |> verify $"Duplicated segment name [{name}]"
            segment


    and [<AbstractClass>] SegmentEquivalent (name:string, flow:Flow) = inherit SegmentBase(name, flow)

    and SegmentAlias(mnemonic:string, flow:Flow, aliasKey:string[]) =
        inherit SegmentBase(mnemonic, flow)
        member _.AliasKey = aliasKey
        static member Create(name, flow, aliasKey) =
            let alias = SegmentAlias(name, flow, aliasKey)
            flow.Graph.AddVertex(alias) |> verify $"Duplicated segment name [{name}]"
            alias

    /// flow 에서 직접 외부 system 의 api 호출한 경우.  R1 > A.Plus;  에서 A system 의 Plus interface 를 직접 호출한 경우
    and SegmentApiCall(apiItem:ApiItem, flow:Flow) =
        inherit SegmentBase(apiItem.QualifiedName, flow)
        member _.ApiItem = apiItem
        static member Create(apiItem:ApiItem, flow:Flow) =
            let existing = flow.Graph.Vertices |> Seq.tryFind(fun v -> v.Name = apiItem.QualifiedName)
            match existing with
            | None ->
                let api = SegmentApiCall(apiItem, flow)
                flow.Graph.AddVertex(api) |> ignore // |> verify $"Duplicated segment name [{apiItem.QualifiedName}]"
                api
            | Some(api) ->
                assert (api :? SegmentApiCall)
                api :?> SegmentApiCall


    and [<AbstractClass>]
        Child (name:string, apiItem:ApiItem, segment:Segment) =
            inherit FqdnObject(name, segment)
            interface IChildVertex
            member _.Segment = segment
            member _.ApiItem = apiItem

    and ChildApiCall private (apiItem:ApiItem, segment:Segment) =
        inherit Child(apiItem.QualifiedName, apiItem, segment)
        static member Create(apiItem, segment) =
            let child = ChildApiCall(apiItem, segment)
            segment.Graph.AddVertex(child) |> verify $"Duplicated child name [{apiItem.QualifiedName}]"
            child

    and ChildAliased private (mnemonic:string, apiItem:ApiItem, segment:Segment) =
        inherit Child(mnemonic, apiItem, segment)

        static member Create(mnemonic, apiItem, segment) =
            let child = ChildAliased(mnemonic, apiItem, segment)
            segment.Graph.AddVertex(child) |> verify $"Duplicated child name [{mnemonic}]"
            child

    and [<AllowNullLiteral>]
        Api(system:DsSystem) =
            member val Items = createNamedHashSet<ApiItem>()
            member val ResetInfos = ResizeArray<ApiResetInfo>()
            member _.System = system

    and ApiItem private (name:string, system:DsSystem) =
        inherit FqdnObject(name, system)
        interface IFlowVertex
        interface IChildVertex
        
        
        member val TXs = createQualifiedNamedHashSet<Segment>()
        member val RXs = createQualifiedNamedHashSet<Segment>()
        member val Resets = createQualifiedNamedHashSet<Segment>()
        member x.AddTXs(txs:Segment seq) = txs |> Seq.forall(fun tx -> x.TXs.Add(tx))
        member x.AddRXs(rxs:Segment seq) = rxs |> Seq.forall(fun rx -> x.TXs.Add(rx))
        member x.AddResets(resets:Segment seq) = resets |> Seq.forall(fun r -> x.TXs.Add(r))
        member _.System = system

        static member Create(name, system) =
            let cp = ApiItem(name, system)
            if isNull system.Api then
                system.Api <- Api(system)
            system.Api.Items.Add(cp) |> verify $"Duplicated interface prototype name [{name}]"
            cp

        //member val Xywh:Xywh = Xywh(0,0,Some(0),Some(0)) with get,set
        //override x.ToText() = name

    /// API 의 reset 정보:  "+" <||> "-";
    and ApiResetInfo private (system:DsSystem, operand1:string, operator:string, operand2:string) =
        member val Operand1 = operand1  // "+"
        member val Operand2 = operand2  // "<||>"
        member val Operator = operator  // "-"
        static member Create(system, operand1:string, operator:string, operand2:string) =
            let ri = ApiResetInfo(system, operand1, operator, operand2)
            system.Api.ResetInfos.Add(ri) //|> verify $"Duplicated interface prototype name [{name}]"
            ri

    and ButtonDic = Dictionary<string, ResizeArray<Flow>>

