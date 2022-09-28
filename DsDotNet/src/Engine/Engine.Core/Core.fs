// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Collections.Generic
open System.Linq

[<AutoOpen>]
module CoreModule =
    let private createNamedHashSet<'T when 'T:> INamed>() =
        new HashSet<'T>(Seq.empty<'T>, nameComparer<'T>())

    let private qualifiedNameComparer<'T when 'T:> IQualifiedNamed>() = {
        new IEqualityComparer<'T> with
            member _.Equals(x:'T, y:'T) = x.QualifiedName = y.QualifiedName
            member _.GetHashCode(x) = x.QualifiedName.GetHashCode()
    }

    let private createQualifiedNamedHashSet<'T when 'T:> IQualifiedNamed>() =
        new HashSet<'T>(Seq.empty<'T>, qualifiedNameComparer<'T>())        

    let private nameComponentsComparer() = {
        new IEqualityComparer<NameComponents> with
            member _.Equals(x:NameComponents, y:NameComponents) = Enumerable.SequenceEqual(x, y)
            member _.GetHashCode(x:NameComponents) = x.Average(fun s -> s.GetHashCode()) |> int
    }

    type FqdnObject(name:string, parent:IQualifiedNamed) =
        inherit Named(name)
        interface IQualifiedNamed with
            member val NameComponents = [| yield! parent.NameComponents; name |]
            member x.QualifiedName = x.NameComponents.Combine()
        member x.Name with get() = (x :> INamed).Name
        member x.NameComponents = (x :> IQualifiedNamed).NameComponents
        member x.QualifiedName = (x :> IQualifiedNamed).QualifiedName


    type Model() =
        member val Systems = createNamedHashSet<DsSystem>()
        //member x.Cpus = x.Systems.Select(fun sys -> sys.Cpu)
        interface IQualifiedNamed with
            member val Name = null //failwith "ERROR"
            member val NameComponents = Array.empty<string>
            member x.QualifiedName = null  //failwith "ERROR"


    and DsSystem private (name:string, cpu:ICpu, model:Model) as this =
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
        member val Graph = Graph<IFlowVertex, InFlowEdge>()
        /// alias.target = [| mnemonic1; ... ; mnemonicn; |]
        member val AliasMap = Dictionary<NameComponents, HashSet<string>>(nameComponentsComparer())
        static member Create(name:string, system:DsSystem) =
            let flow = Flow(name, system)
            system.Flows.Add(flow) |> verify $"Duplicated flow name [{name}]"
            flow

    and Segment private (name:string, flow:Flow) =
        inherit FqdnObject(name, flow)
        interface IFlowVertex
        member val Graph = Graph<IChildVertex, InSegmentEdge>()
        member val Flow = flow
        static member Create(name, flow) =
            let segment = Segment(name, flow)
            flow.Graph.AddVertex(segment) |> verify $"Duplicated segment name [{name}]"
            segment


    and SegmentAlias private (name:string, segment:Segment) =
        inherit FqdnObject(name, segment.Flow)
        interface IFlowVertex
        member _.ReferenceSegment = segment
        static member Create(name, segment) =
            let alias = SegmentAlias(name, segment)
            segment.Flow.Graph.AddVertex(alias) |> verify $"Duplicated segment name [{name}]"
            alias

    and Child private (name:string, callPrototype:ApiItem, segment:Segment) =
        inherit FqdnObject(name, segment)
        interface IChildVertex
        member _.Segment = segment
        member _.CallPrototype = callPrototype

        static member Create(name, interfacePrototype:ApiItem, segment) =
            let child = Child(name, interfacePrototype, segment)
            segment.Graph.AddVertex(child) |> verify $"Duplicated child name [{name}]"
            child

    //and ChildAlias private (name:string, child:Child) =
    //    inherit Named(name)
    //    member _.ReferenceChild = child
    //    interface IChildVertex

    //    interface INamed with
    //        member val Name = name  // with get, set
    //    interface IQualifiedNamed with
    //        member val NameComponents = [| yield! child.Segment.NameComponents; name|]
    //        member x.QualifiedName = x.NameComponents.Combine()
    //    member x.Name with get() = (x :> INamed).Name
    //    member x.NameComponents = (x :> IQualifiedNamed).NameComponents
    //    member x.QualifiedName = (x :> IQualifiedNamed).QualifiedName
    //    static member Create(name, child) =
    //        let alias = ChildAlias(name, child)
    //        child.Segment.AddVertex(alias) |> verify $"Duplicated child name [{name}]"
    //        alias

    and 
        [<AllowNullLiteral>]
        Api(system:DsSystem) =
            member val Items = createNamedHashSet<ApiItem>()
            member val ResetInfos = ResizeArray<ApiResetInfo>()
            member _.System = system

    and ApiItem private (name:string, system:DsSystem) =
        inherit FqdnObject(name, system)
        
        member val TXs = createQualifiedNamedHashSet<Segment>()
        member val RXs = createQualifiedNamedHashSet<Segment>()
        member val Resets = createQualifiedNamedHashSet<Segment>()
        member x.AddTXs(txs:Segment seq) = txs |> Seq.forall(fun tx -> x.TXs.Add(tx))
        member x.AddRXs(rxs:Segment seq) = rxs |> Seq.forall(fun rx -> x.TXs.Add(rx))
        member x.AddResets(resets:Segment seq) = resets |> Seq.forall(fun r -> x.TXs.Add(r))
        member _.System = system

        static member Create(name, system) =
            let cp = ApiItem(name, system)
            system.Api.Items.Add(cp) |> verify $"Duplicated interface prototype name [{name}]"
            cp

        //member val Xywh:Xywh = Xywh(0,0,Some(0),Some(0)) with get,set
        //override x.ToText() = name

    and ApiResetInfo private (system:DsSystem, operand1:string, operator:string, operand2:string) =
        member val Operand1 = operand1
        member val Operand2 = operand2
        member val Operator = operator
        static member Create(system, operand1:string, operator:string, operand2:string) =
            let ri = ApiResetInfo(system, operand1, operator, operand2)
            system.Api.ResetInfos.Add(ri) //|> verify $"Duplicated interface prototype name [{name}]"
            ri

    and ButtonDic = Dictionary<string, ResizeArray<Flow>>

