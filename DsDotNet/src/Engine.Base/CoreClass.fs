// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Diagnostics
open System.Collections.Generic
open Engine.Core.CoreFlow
open Engine.Core
open System

[<AutoOpen>]
module CoreClass =

    /// Real segment 
    [<DebuggerDisplay("{ToText()}")>]
    type SegmentBase(name:string, childFlow:ChildFlow)  =
        inherit SegBase(name,  childFlow)
        let mutable status4 = Status4.Homing
            
        override x.ToText() = childFlow.QualifiedName
        member x.Name = name
        member val Status4 = status4 with get, set
       
        member x.ChildFlow = childFlow
        member x.RootFlow  = childFlow.ContainerFlow
        //parser 전용 추후  상속받을 예정??
        member val InstanceMap  = Dictionary<string, obj>();
        member val SafetyConditions  =  List<SegmentBase>()  with get,set
        member val Addresses:System.Tuple<string, string, string>  = Tuple.Create("","","")  with get,set
        member x.ContainerFlow = childFlow.ContainerFlow
        member x.NameComponents = childFlow.ContainerFlow.NameComponents |> Seq.append [name]
        member x.QualifiedName  = x.NameComponents.Combine()
        
        static member Create(name, rootFlow) = 
                    let childFlow = ChildFlow(name, rootFlow)
                    let seg = SegmentBase(name, childFlow)
                    rootFlow.AddChildVertex(seg)
                    seg




    //parser 전용 추후  상속받을 예정??
    type ExSegment(name:string, segmentBase:SegmentBase)  =
        member x.Name = name
        member x.SegmentBase = segmentBase


    /// Call segment 
    [<DebuggerDisplay("{ToText()}")>]
    type CallSeg(name:string, rootFlow:RootFlow) =
        inherit CallBase(name)
        let mutable status4 = Status4.Homing

        override x.ToText() = name
            
        member x.Name = name
        member x.RootFlow = rootFlow
        member val Status4 = status4 with get, set

