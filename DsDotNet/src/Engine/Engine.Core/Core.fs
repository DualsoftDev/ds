// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Collections.Generic
open System.Runtime.CompilerServices
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
        | RealTarget of Real
        | CallTarget of Call

    and Alias private (mnemonic:string, target:AliasTargetType, parent:ParentWrapper) =
        inherit Vertex(mnemonic, parent)

        static let tryFindAlias (graph:Graph<Vertex, Edge>) (mnemonic:string) =
            let existing = graph.TryFindVertex(mnemonic)
            match existing with
            | Some (:? Alias as a) -> Some a
            | Some v -> failwith "Alias name is already used by other vertex"
            | None -> None

        member x.Target = target

        override x.GetRelativeName(referencePath:NameComponents) =
            match target with
            | RealTarget r -> x.Name
            | CallTarget c -> base.GetRelativeName(referencePath)

        static member Create(mnemonic, target:AliasTargetType, parent:ParentWrapper) =
            let graph = parent.Graph
            let creator() =
                let alias = Alias(mnemonic, target, parent)
                graph.AddVertex(alias) |> verifyM $"Duplicated child name [{mnemonic}]"
                alias

            let existing = tryFindAlias graph mnemonic
            match existing with
            | Some a -> a
            | _ -> creator()



    /// 외부 시스템 호출 객체
    and Call private (apiItem:ApiItem, parent:ParentWrapper) =
        inherit Vertex(apiItem.QualifiedName, parent)
        static let create (graph:Graph<Vertex, Edge>) (apiItem:ApiItem) (parent:ParentWrapper) =
            let existing = graph.TryFindVertex(apiItem.QualifiedName)
            match existing with
            | Some (:? Call as v) -> v
            | Some v ->
                failwith $"Duplicated call name [{apiItem.QualifiedName}]"
            | _ ->
                let call = Call(apiItem, parent)
                graph.AddVertex(call) |> verifyM $"Duplicated call name [{apiItem.QualifiedName}]"
                call

        member _.ApiItem = apiItem
        member val Addresses:Addresses = null with get, set

        static member CreateInFlow(apiItem:ApiItem, flow: Flow) = create flow.Graph apiItem (Flow flow)
        static member CreateInReal(apiItem:ApiItem, real:Real) = create real.Graph apiItem (Real real)

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
        member x.Graph:Graph<Vertex, Edge> =
            match x with
            | Flow f -> f.Graph
            | Real r -> r.Graph


    and ButtonDic = Dictionary<string, ResizeArray<Flow>>

    and Edge private (source:Vertex, target:Vertex, edgeType:EdgeType) =
        inherit EdgeBase<Vertex>(source, target, edgeType)

        static member Create(graph:Graph<_,_>, source, target, edgeType:EdgeType) =
            let edge = Edge(source, target, edgeType)
            graph.AddEdge(edge) |> verifyM $"Duplicated edge [{source.Name}{edgeType.ToText()}{target.Name}]"
            edge
        
        member val EditorInfo = EdgeType.EditorSpare with get, set
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
