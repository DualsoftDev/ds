// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Collections.Generic
open System.Diagnostics
open Engine.Common.FS

[<AutoOpen>]
module CoreModule =
    ///Top level structure
    type Model() =
        member val Systems = createNamedHashSet<DsSystem>()
        interface IQualifiedNamed with
            member val Name = null 
            member val NameComponents = Array.empty<string>
            member x.QualifiedName = null  

    and DsSystem private (name:string, host:string, cpu:ICpu option, model:Model) =
        inherit FqdnObject(name, model)

        member val Flows    = createNamedHashSet<Flow>()
        member val ApiItems = createNamedHashSet<ApiItem>()
        member val ApiResetInfos = HashSet<ApiResetInfo>() with get, set

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

    and Flow private (name:string, system:DsSystem) =
        inherit VertexBase(name, system, system, ParentType.NoneParent)
        member val Graph = Graph<VertexBase, InFlowEdge>()     
        member val AliasMap = Dictionary<NameComponents, HashSet<string>>(nameComponentsComparer())
        member x.System = system
        static member Create(name:string, system:DsSystem) =
            let flow = Flow(name, system)
            system.Flows.Add(flow) |> verifyM $"Duplicated flow name [{name}]"
            flow

    /// leaf or stem(parenting)
    and [<AbstractClass>]
        VertexBase (name:string, nodeParent:FqdnObject, system:DsSystem,  parentType:ParentType) =
        inherit FqdnObject(name, nodeParent)
        interface INamedVertex
        member _.System = system
        ///부모의 위치 정보
        member _.ParentType = parentType 

    /// Segment (DS Basic Unit)
    and [<DebuggerDisplay("{QualifiedName}")>]
        Real private (name:string, flow:Flow) =
        inherit VertexBase(name, flow, flow.System, ParentType.Flow)
        member val Graph = Graph<VertexBase, InRealEdge>()
        member val Flow = flow
        member val SafetyConditions = createQualifiedNamedHashSet<Real>() 
        static member Create(name:string, flow) =
            if (name.Contains(".") (*&& not <| (name.StartsWith("\"") && name.EndsWith("\""))*)) then
                logWarn $"Suspicious segment name [{name}]. Check it."

            let segment = Real(name, flow)
            flow.Graph.AddVertex(segment) |> verifyM $"Duplicated segment name [{name}]"
            segment

    /// 외부 시스템 호출 객체
    and Call private (apiItem:ApiItem, vertexParent:VertexBase, parentType:ParentType) =
        inherit VertexBase(apiItem.QualifiedName, vertexParent, vertexParent.System, parentType)
        member _.ApiItem = apiItem
        static member CreateInFlow(apiItem:ApiItem, flow:Flow) = Call.Create(apiItem, flow)
        static member CreateInReal(apiItem:ApiItem, real:Real) = Call.Create(apiItem, real)
        static member private Create(apiItem:ApiItem, vertexParent:VertexBase) = 
                let call = 
                    match vertexParent with
                    | :? Flow     as flow ->   Call(apiItem, vertexParent, ParentType.Flow)
                    | :? Real     as real ->   Call(apiItem, vertexParent, ParentType.Real)
                    | _ -> failwith $"ERROR: able type is Flow or Real!! \n{vertexParent.Name}({vertexParent.GetType().Name})"

                match vertexParent with
                | :? Flow     as flow ->  flow.Graph.AddVertex(call) |> verifyM $"Duplicated call name [{apiItem.QualifiedName}]"
                | :? Real     as real ->  real.Graph.AddVertex(call) |> verifyM $"Duplicated call name [{apiItem.QualifiedName}]"
                | _ -> failwith $"ERROR"
                call
      
    and Alias private (mnemonic:string, vertexParent:FqdnObject, system:DsSystem, aliasKey:string[], parentType:ParentType) =
        inherit VertexBase(mnemonic, vertexParent, system, parentType)

        static member CreateInFlow(name, aliasKey, flow:Flow) =
            let alias = Alias(name, flow, flow.System, aliasKey, ParentType.Flow)
            flow.Graph.AddVertex(alias) |> verifyM $"Duplicated segment name [{name}]"
            alias
        static member CreateInReal(mnemonic, apiItem:ApiItem, segment:Real) =
            let child = Alias(mnemonic, segment, segment.System,  apiItem.NameComponents, ParentType.Real)
            segment.Graph.AddVertex(child) |> verifyM $"Duplicated child name [{mnemonic}]"
            child
    
        member _.AliasKey = aliasKey  

    and ApiItem private (name:string, system:DsSystem) =
        inherit FqdnObject(name, system)
        interface INamedVertex
        
        member val TXs = createQualifiedNamedHashSet<Real>()
        member val RXs = createQualifiedNamedHashSet<Real>()
        member x.AddTXs(txs:Real seq) = txs |> Seq.forall(fun tx -> x.TXs.Add(tx))
        member x.AddRXs(rxs:Real seq) = rxs |> Seq.forall(fun rx -> x.RXs.Add(rx))
        member _.System = system
        member val Xywh:Xywh = null with get, set
        member val Addresses:Addresses = null with get, set

        static member Create(name, system) =
            let cp = ApiItem(name, system)
            system.ApiItems.Add(cp) |> verifyM $"Duplicated interface prototype name [{name}]"
            cp

        //member val Xywh:Xywh = Xywh(0,0,Some(0),Some(0)) with get,set
        //override x.ToText() = name

    /// API 의 reset 정보:  "+" <||> "-";
    and ApiResetInfo private (system:DsSystem, operand1:string, operator:string, operand2:string) =
        member val Operand1 = operand1  // "+"
        member val Operand2 = operand2  // "-"
        member val Operator = operator  // "<||>"
        member x.Text = sprintf "%s %s %s" operand1 operator operand2  //"+" <||> "-"
        static member Create(system, operand1, operator, operand2) =
            let ri = ApiResetInfo(system, operand1, operator, operand2)
            system.ApiResetInfos.Add(ri) |> verifyM $"Duplicated interface ResetInfo [{ri.Text}]"
            ri

    and ButtonDic = Dictionary<string, ResizeArray<Flow>>

    and InFlowEdge private (source:VertexBase, target:VertexBase, edgeType:EdgeType) =
        inherit EdgeBase<VertexBase>(source, target, edgeType)
        static member Create(flow:Flow, source, target, edgeType:EdgeType) =
            let edge = InFlowEdge(source, target, edgeType)
            flow.Graph.AddEdge(edge) |> verifyM $"Duplicated edge [{source.Name}{edgeType.ToText()}{target.Name}]"
            edge
        override x.ToString() = $"{x.Source.QualifiedName} {x.EdgeType.ToText()} {x.Target.QualifiedName}"

    and InRealEdge private (source:VertexBase, target:VertexBase, edgeType:EdgeType) =
        inherit EdgeBase<VertexBase>(source, target, edgeType)
        static member Create(segment:Real, source, target, edgeType:EdgeType) =
            let edge = InRealEdge(source, target, edgeType)
            let gr:Graph<_, _> = segment.Graph
            segment.Graph.AddEdge(edge) |> verifyM $"Duplicated edge [{source.Name}{edgeType}{target.Name}]"
            edge
        override x.ToString() = $"{x.Source.QualifiedName} {x.EdgeType.ToText()} {x.Target.QualifiedName}"