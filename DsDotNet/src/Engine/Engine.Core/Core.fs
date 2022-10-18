// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Collections.Generic
open System.Diagnostics
open Engine.Common.FS

[<AutoOpen>]
module CoreModule =
    type ICall =
        abstract Addresses:Addresses
    type Model() =
        member val Systems = createNamedHashSet<DsSystem>()
        interface IQualifiedNamed with
            member val Name = null //failwith "ERROR"
            member val NameComponents = Array.empty<string>
            member x.QualifiedName = null  //failwith "ERROR"


    and DsSystem private (name:string, host:string, cpu:ICpu option, model:Model) =
        inherit FqdnObject(name, model)

        //new (name, model) = DsSystem(name, null, model)
        member val Flows = createNamedHashSet<Flow>()
        member val Api:Api = null with get, set

        member _.Model = model
        member _.Cpu = cpu
        member _.Host = host

        //시스템 버튼 소속 Flow 정보
        member val EmergencyButtons = ButtonDic()
        member val AutoButtons      = ButtonDic()
        member val StartButtons     = ButtonDic()
        member val ResetButtons     = ButtonDic()
        static member Create(name, host, cpu, model) =
            let system = DsSystem(name, host, cpu, model)
            model.Systems.Add(system) |> verifyM $"Duplicated system name [{name}]"
            system

    and Flow private(name:string, system:DsSystem) =
        inherit FqdnObject(name, system)
        member val Graph = Graph<NodeInFlow, InFlowEdge>()     // todo: IFlowVertex -> SegmentBase
        /// alias.target = [| mnemonic1; ... ; mnemonicn; |]
        member val AliasMap = Dictionary<NameComponents, HashSet<string>>(nameComponentsComparer())
        member x.System = system
        static member Create(name:string, system:DsSystem) =
            let flow = Flow(name, system)
            system.Flows.Add(flow) |> verifyM $"Duplicated flow name [{name}]"
            flow

    and InFlowEdge private (source:NodeInFlow, target:NodeInFlow, edgeType:EdgeType) =
        inherit EdgeBase<NodeInFlow>(source, target, edgeType)
        static member Create(flow:Flow, source, target, edgeType:EdgeType) =
            let edge = InFlowEdge(source, target, edgeType)
            flow.Graph.AddEdge(edge) |> verifyM $"Duplicated edge [{source.Name}{edgeType.ToText()}{target.Name}]"
            edge
        override x.ToString() = $"{x.Source.QualifiedName} {x.EdgeType.ToText()} {x.Target.QualifiedName}"

    and InSegmentEdge private (source:NodeInReal, target:NodeInReal, edgeType:EdgeType) =
        inherit EdgeBase<NodeInReal>(source, target, edgeType)
        static member Create(segment:RealInFlow, source, target, edgeType:EdgeType) =
            let edge = InSegmentEdge(source, target, edgeType)
            let gr:Graph<_, _> = segment.Graph
            segment.Graph.AddEdge(edge) |> verifyM $"Duplicated edge [{source.Name}{edgeType}{target.Name}]"
            edge
        override x.ToString() = $"{x.Source.QualifiedName} {x.EdgeType.ToText()} {x.Target.QualifiedName}"

    and [<AbstractClass>]
        NodeInFlow (name:string, flow:Flow) =
        inherit FqdnObject(name, flow)
        interface IFlowVertex

    /// normal segment : leaf, stem(parenting)
    /// RealInReal //Real안에 Real은 불가능한 객체
    and [<DebuggerDisplay("{QualifiedName}")>]
        RealInFlow private (name:string, flow:Flow) =
        inherit NodeInFlow(name, flow)
        member val Graph = Graph<NodeInReal, InSegmentEdge>()
        member val Flow = flow
        member val SafetyConditions = createQualifiedNamedHashSet<RealInFlow>()
        member val Addresses:Addresses = null with get, set
        static member Create(name:string, flow) =
            if (name.Contains(".") (*&& not <| (name.StartsWith("\"") && name.EndsWith("\""))*)) then
                logWarn $"Suspicious segment name [{name}]. Check it."

            let segment = RealInFlow(name, flow)
            flow.Graph.AddVertex(segment) |> verifyM $"Duplicated segment name [{name}]"
            segment

    and AliasInFlow(mnemonic:string, flow:Flow, aliasKey:string[]) =
        inherit NodeInFlow(mnemonic, flow)
        member _.AliasKey = aliasKey
        static member Create(name, flow, aliasKey) =
            let alias = AliasInFlow(name, flow, aliasKey)
            flow.Graph.AddVertex(alias) |> verifyM $"Duplicated segment name [{name}]"
            alias

    /// flow 에서 직접 외부 system 의 api 호출한 경우.  R1 > A.Plus;  에서 A system 의 Plus interface 를 직접 호출한 경우
    and CallInFlow(apiItem:ApiItem, flow:Flow) =
        inherit NodeInFlow(apiItem.QualifiedName, flow)

        //interface ICall with
        //    member x.Addresses = x.Addresses
        member _.ApiItem = apiItem
        static member Create(apiItem:ApiItem, flow:Flow) =
            let existing = flow.Graph.Vertices |> Seq.tryFind(fun v -> v.Name = apiItem.QualifiedName)
            match existing with
            | None ->
                let api = CallInFlow(apiItem, flow)
                flow.Graph.AddVertex(api) |> ignore // |> verify $"Duplicated segment name [{apiItem.QualifiedName}]"
                api
            | Some(api) ->
                assert (api :? CallInFlow)
                api :?> CallInFlow


    and [<AbstractClass>]
        NodeInReal (name:string, apiItem:ApiItem, segment:RealInFlow) =
        inherit FqdnObject(name, segment)
        interface IChildVertex
        member _.Segment = segment
        member _.ApiItem = apiItem

    and CallInReal private (apiItem:ApiItem, segment:RealInFlow) =
        inherit NodeInReal(apiItem.QualifiedName, apiItem, segment)
        //interface ICall with
        //    member x.Addresses = x.Addresses
        static member CreateOnDemand(apiItem:ApiItem, segment:RealInFlow) =
            let gr = segment.Graph
            let existing = gr.FindVertex(apiItem.QualifiedName)
            if existing.IsNonNull() then
                existing :?> CallInReal
            else
                let child = CallInReal(apiItem, segment)
                gr.AddVertex(child) |> verifyM $"Duplicated child name [{apiItem.QualifiedName}]"
                child

    and AliasInReal private (mnemonic:string, apiItem:ApiItem, segment:RealInFlow) =
        inherit NodeInReal(mnemonic, apiItem, segment)

        static member Create(mnemonic, apiItem, segment) =
            let child = AliasInReal(mnemonic, apiItem, segment)
            segment.Graph.AddVertex(child) |> verifyM $"Duplicated child name [{mnemonic}]"
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

        member val TXs = createQualifiedNamedHashSet<RealInFlow>()
        member val RXs = createQualifiedNamedHashSet<RealInFlow>()
        member val Resets = createQualifiedNamedHashSet<RealInFlow>()
        member x.AddTXs(txs:RealInFlow seq) = txs |> Seq.forall(fun tx -> x.TXs.Add(tx))
        member x.AddRXs(rxs:RealInFlow seq) = rxs |> Seq.forall(fun rx -> x.RXs.Add(rx))
        member x.AddResets(resets:RealInFlow seq) = resets |> Seq.forall(fun r -> x.Resets.Add(r))
        member _.System = system
        member val Xywh:Xywh = null with get, set

        static member Create(name, system) =
            let cp = ApiItem(name, system)
            if isNull system.Api then
                system.Api <- Api(system)
            system.Api.Items.Add(cp) |> verifyM $"Duplicated interface prototype name [{name}]"
            cp

        //member val Xywh:Xywh = Xywh(0,0,Some(0),Some(0)) with get,set
        //override x.ToText() = name

    /// API 의 reset 정보:  "+" <||> "-";
    and ApiResetInfo private (system:DsSystem, operand1:string, operator:string, operand2:string) =
        member val Operand1 = operand1  // "+"
        member val Operand2 = operand2  // "-"
        member val Operator = operator  // "<||>"
        static member Create(system, operand1:string, operator:string, operand2:string) =
            let ri = ApiResetInfo(system, operand1, operator, operand2)
            system.Api.ResetInfos.Add(ri) //|> verify $"Duplicated interface prototype name [{name}]"
            ri

    and ButtonDic = Dictionary<string, ResizeArray<Flow>>

