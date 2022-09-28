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


    type Model() =
        member val Systems = createNamedHashSet<DsSystem>()
        //member x.Cpus = x.Systems.Select(fun sys -> sys.Cpu)

    and DsSystem private (name:string, cpu:ICpu, model:Model) =
        inherit Named(name)

        //new (name, model) = DsSystem(name, null, model)
        member val Flows = createNamedHashSet<Flow>()
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
        inherit Graph<IFlowVertex, InFlowEdge>()
        member val CallPrototypes = createNamedHashSet<CallPrototype>()
        /// alias.target = [| mnemonic1; ... ; mnemonicn; |]
        member val AliasMap = Dictionary<NameComponents, HashSet<string>>(nameComponentsComparer())

        interface INamed with
            member val Name = name  // with get, set
        interface IQualifiedNamed with
            member val NameComponents = [|system.Name; name|]
            member x.QualifiedName = x.NameComponents.Combine()
        member x.Name with get() = (x :> INamed).Name
        member x.NameComponents = (x :> IQualifiedNamed).NameComponents
        member x.QualifiedName = (x :> IQualifiedNamed).QualifiedName
        static member Create(name:string, system:DsSystem) =
            let flow = Flow(name, system)
            system.Flows.Add(flow) |> verify $"Duplicated flow name [{name}]"
            flow

    and Segment private (name:string, flow:Flow) =
        inherit Graph<IChildVertex, InSegmentEdge>()
        interface IFlowVertex
        member val Flow = flow

        interface INamed with
            member val Name = name  // with get, set
        interface IQualifiedNamed with
            member val NameComponents = [| yield! flow.NameComponents; name|]
            member x.QualifiedName = x.NameComponents.Combine()
        member x.Name with get() = (x :> INamed).Name
        member x.NameComponents = (x :> IQualifiedNamed).NameComponents
        member x.QualifiedName = (x :> IQualifiedNamed).QualifiedName
        static member Create(name, flow) =
            let segment = Segment(name, flow)
            flow.AddVertex(segment) |> verify $"Duplicated segment name [{name}]"
            segment


    and SegmentAlias private (name:string, segment:Segment) =
        member _.ReferenceSegment = segment

        interface IFlowVertex

        interface INamed with
            member val Name = name  // with get, set
        interface IQualifiedNamed with
            member val NameComponents = [| yield! segment.Flow.NameComponents; name|]
            member x.QualifiedName = x.NameComponents.Combine()
        member x.Name with get() = (x :> INamed).Name
        member x.NameComponents = (x :> IQualifiedNamed).NameComponents
        member x.QualifiedName = (x :> IQualifiedNamed).QualifiedName
        static member Create(name, segment) =
            let alias = SegmentAlias(name, segment)
            segment.Flow.AddVertex(alias) |> verify $"Duplicated segment name [{name}]"
            alias

    and Child private (name:string, callPrototype:CallPrototype, segment:Segment) =
        inherit Named(name)
        interface IChildVertex
        member _.Segment = segment
        member _.CallPrototype = callPrototype

        interface INamed with
            member val Name = name  // with get, set
        interface IQualifiedNamed with
            member val NameComponents = [| yield! segment.Flow.NameComponents; name|]
            member x.QualifiedName = x.NameComponents.Combine()
        member x.Name with get() = (x :> INamed).Name
        member x.NameComponents = (x :> IQualifiedNamed).NameComponents
        member x.QualifiedName = (x :> IQualifiedNamed).QualifiedName
        static member Create(name, callPrototype:CallPrototype, segment) =
            let child = Child(name, callPrototype, segment)
            callPrototype.Users.Add(child) |> verify $"Duplicated call prototype usage on same child [{name}]"
            segment.AddVertex(child) |> verify $"Duplicated child name [{name}]"
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

    and CallPrototype private (name:string, flow:Flow) =
        inherit Named(name)
        
        member val TXs = createQualifiedNamedHashSet<Segment>()
        member val RXs = createQualifiedNamedHashSet<Segment>()
        member val Resets = createQualifiedNamedHashSet<Segment>()
        member x.AddTXs(txs:Segment seq) = txs |> Seq.forall(fun tx -> x.TXs.Add(tx))
        member x.AddRXs(rxs:Segment seq) = rxs |> Seq.forall(fun rx -> x.TXs.Add(rx))
        member x.AddResets(resets:Segment seq) = resets |> Seq.forall(fun r -> x.TXs.Add(r))
        /// this CallPrototype 을 사용하는(user) Child 목록
        member val Users:HashSet<Child> = HashSet<Child>()

        static member Create(name, flow) =
            let cp = CallPrototype(name, flow)
            flow.CallPrototypes.Add(cp) |> verify $"Duplicated call prototype name [{name}]"
            cp

        //member val Xywh:Xywh = Xywh(0,0,Some(0),Some(0)) with get,set
        //override x.ToText() = name

    and ButtonDic = Dictionary<string, ResizeArray<Flow>>

