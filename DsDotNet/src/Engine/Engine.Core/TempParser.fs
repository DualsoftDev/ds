// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Diagnostics
open System.Collections.Generic
open System.Collections.Concurrent
open CoreFlow

[<AutoOpen>]
module TempParser =

    [<AbstractClass>]
    type Call(name:string,  rootFlow:RootFlow, cp:CallPrototype)  =
        inherit CallBase(name)
        member x.RootFlow = rootFlow
        member x.CallPrototype = cp

    type SubCall(name:string,  parenting:SegmentBase, cp:CallPrototype)  =
        member x.Name = name
        member x.CallPrototype = parenting
        member x.SegmentBase = cp

    type Child(subCall:SubCall,  parenting:SegmentBase)  =
        new (ex:ExSegment, parenting:SegmentBase) = 
            let callProto = CallPrototype(ex.Name,parenting.RootFlow)
            Child(SubCall(ex.Name, ex.SegmentBase, callProto), parenting)

        member x.SubCall = subCall
        member x.SegmentBase = SegmentBase
    
    type RootCall(name:string,  rootFlow:RootFlow, cp:CallPrototype)  =
        inherit Call(name, rootFlow, cp)
        member x.Name = name
        member x.RootFlow = rootFlow
        member x.CallPrototype = cp
        
        override x.ToText() = name
