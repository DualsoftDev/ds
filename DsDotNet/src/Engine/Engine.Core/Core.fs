// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Diagnostics
open Engine.Common.FS

[<AutoOpen>]
module CoreModule =
    ///Top level structure
    type Model() =
        member val Systems = createNamedHashSet<DsSystem>()
        member val Variables = ResizeArray<Variable>()
        member val Commands = ResizeArray<Command>()
        member val Observes = ResizeArray<Observe>()

        interface IQualifiedNamed with
            member val Name = null 
            member val NameComponents = Array.empty<string>
            member x.QualifiedName = null  

    and DsSystem private (name:string, host:string, model:Model) =
        inherit FqdnObject(name, model)

        member val Flows    = createNamedHashSet<Flow>()
        member val ApiItems = createNamedHashSet<ApiItem>()
        member val ApiResetInfos = HashSet<ApiResetInfo>() with get, set
        ///시스템 전체시작 버튼누름시 수행되야하는 Real목록
        member val StartPoints = createQualifiedNamedHashSet<Real>() 

        member _.Model = model
        member _.Host = host

        ///시스템 버튼 소속 Flow 정보
        member val EmergencyButtons = ButtonDic()
        member val AutoButtons      = ButtonDic()
        member val StartButtons     = ButtonDic()
        member val ResetButtons     = ButtonDic()

        /// API name -> Address map.  A.+ = (%Q1234.2343, %I1234.2343)
        member val ApiAddressMap    = Dictionary<string[], Addresses>(nameComponentsComparer())


        ///시스템 핸들링 대상여부   true : mySystem / false : exSystem
        member val Active = false with get, set 

        static member Create(name, host, model) =
            let system = DsSystem(name, host, model)
            model.Systems.Add(system) |> verifyM $"Duplicated system name [{name}]"
            system

    and Flow private (name:string, system:DsSystem) =
        inherit FqdnObject(name, system)
        member val Graph = Graph<Vertex, Edge>()     
        member val AliasMap = Dictionary<NameComponents, HashSet<string>>(nameComponentsComparer())
        member x.System = system
        static member Create(name:string, system:DsSystem) =
            let flow = Flow(name, system)
            system.Flows.Add(flow) |> verifyM $"Duplicated flow name [{name}]"
            flow




    /// leaf or stem(parenting)
    and [<AbstractClass>]
        Vertex (name:string, parent:ParentWrapper) =
        inherit FqdnObject(name, parent.Core)
        interface INamedVertex
        member _.Parent = parent 

    /// Segment (DS Basic Unit)
    and [<DebuggerDisplay("{QualifiedName}")>]
        Real private (name:string, flow:Flow) =
        inherit Vertex(name, Flow flow)
        member val Graph = Graph<Vertex, Edge>()
        member val Flow = flow

        member val SafetyConditions = createQualifiedNamedHashSet<Real>() 
        static member Create(name:string, flow) =
            if (name.Contains(".") (*&& not <| (name.StartsWith("\"") && name.EndsWith("\""))*)) then
                logWarn $"Suspicious segment name [{name}]. Check it."

            let segment = Real(name, flow)
            flow.Graph.AddVertex(segment) |> verifyM $"Duplicated segment name [{name}]"
            segment

    and AliasTargetType =
        | NullTarget
        | RealTarget of Real
        | CallTarget of Call

    and Alias private (mnemonic:string, parent:ParentWrapper, aliasKey:string[], isOtherFlowCall:bool) =
        inherit Vertex(mnemonic, parent)
        
        member _.IsOtherFlowCall = isOtherFlowCall
        member _.AliasKey = aliasKey
        member val Target = NullTarget with get, set
        member x.SetTarget(call) = assert(x.Target = NullTarget); x.Target <- CallTarget call
        member x.SetTarget(real) = assert(x.Target = NullTarget); x.Target <- RealTarget real
        
        override x.GetRelativeName(referencePath:NameComponents) =
            if isOtherFlowCall then
                aliasKey[1..].Combine()
            else
                base.GetRelativeName(referencePath)
        
        static member CreateInFlow(name, aliasKey, flow:Flow, [<Optional; DefaultParameterValue(false)>] isOtherFlowCall) =
            let alias = Alias(name, Flow flow, aliasKey, isOtherFlowCall)
            flow.Graph.AddVertex(alias) |> verifyM $"Duplicated segment name [{name}]"
            alias
        static member CreateInReal(mnemonic, apiItem:ApiItem, parent:Real) =
            let child = Alias(mnemonic, Real parent, apiItem.NameComponents, false)
            parent.Graph.AddVertex(child) |> verifyM $"Duplicated child name [{mnemonic}]"
            child
        static member CreateInReal(mnemonic, call:Call, parent:Real) =
            let api:ApiItem = call.ApiItem
            let child = Alias(mnemonic, Real parent, api.NameComponents, false)
            child.SetTarget(call)
            parent.Graph.AddVertex(child) |> verifyM $"Duplicated child name [{mnemonic}]"
            child
            
    

    /// 외부 시스템 호출 객체
    and Call private (apiItem:ApiItem, parent:ParentWrapper) =
        inherit Vertex(apiItem.QualifiedName, parent)
        member _.ApiItem = apiItem
        member val Addresses:Addresses = null with get, set

        static member CreateInFlow(apiItem:ApiItem, flow:Flow) =
            let call = Call(apiItem, Flow flow)
            flow.Graph.AddVertex(call) |> verifyM $"Duplicated call name [{apiItem.QualifiedName}]"
            call

        static member CreateInReal(apiItem:ApiItem, real:Real) =
            let call = Call(apiItem, Real real)
            real.Graph.AddVertex(call) |> verifyM $"Duplicated call name [{apiItem.QualifiedName}]"
            call

        /// Graph 에 포함되지 않는 core.  Alias 에 숨은 core
        static member CreateNowhere(apiItem:ApiItem, parent:ParentWrapper) = Call(apiItem, parent)

      
    and ApiItem private (name:string, system:DsSystem) =
        inherit FqdnObject(name, system)
        interface INamedVertex
        
        member val TXs = createQualifiedNamedHashSet<Real>()
        member val RXs = createQualifiedNamedHashSet<Real>()
        member x.AddTXs(txs:Real seq) = txs |> Seq.forall(fun tx -> x.TXs.Add(tx))
        member x.AddRXs(rxs:Real seq) = rxs |> Seq.forall(fun rx -> x.RXs.Add(rx))
        member _.System = system
        member val Xywh:Xywh = null with get, set

        static member Create(name, system) =
            let cp = ApiItem(name, system)
            system.ApiItems.Add(cp) |> verifyM $"Duplicated interface prototype name [{name}]"
            cp

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


    ///Vertex의 부모의 타입을 구분한다.
    and ParentWrapper = 
        | Flow of Flow //Real/Call/Alias 의 부모
        | Real of Real //Call/Alias      의 부모
        member x.Core =
            match x with
            | Flow f -> f :> FqdnObject
            | Real r -> r
        member x.System =
            match x with
            | Flow f -> f.System
            | Real r -> r.Flow.System

    and ButtonDic = Dictionary<string, ResizeArray<Flow>>

    and Edge private (source:Vertex, target:Vertex, edgeType:EdgeType) =
        inherit EdgeBase<Vertex>(source, target, edgeType)
        static member Create(graph:Graph<_,_>, source, target, edgeType:EdgeType) =
            let edge = Edge(source, target, edgeType)
            graph.AddEdge(edge) |> verifyM $"Duplicated edge [{source.Name}{edgeType.ToText()}{target.Name}]"
            edge
        override x.ToString() = $"{x.Source.QualifiedName} {x.EdgeType.ToText()} {x.Target.QualifiedName}"

[<Extension>]
type CoreExt =
    [<Extension>] static member GetSystem(call:Call) = call.Parent.System
    [<Extension>]
    static member AddButton(sys:DsSystem, btnType:BtnType, btnName: string, flow:Flow) = 
        let dicButton =
            match btnType with
            | StartBTN       -> sys.StartButtons
            | ResetBTN       -> sys.ResetButtons
            | EmergencyBTN   -> sys.EmergencyButtons
            | AutoBTN        -> sys.AutoButtons

        if dicButton.ContainsKey btnName then
            dicButton.[btnName].Add(flow)
        else
            dicButton.Add(btnName, ResizeArray[|flow|] )
