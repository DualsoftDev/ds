// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Diagnostics
open System.Collections.Generic
open System.Collections.Concurrent
open CoreFlow

[<AutoOpen>]
module CoreGraph =

    [<DebuggerDisplay("{name}")>]
    type DsGraph(name:string)  =
        inherit SysBase(name)
        let dicRootFlow = ConcurrentDictionary<string, RootFlow>()

        override x.ToText() = name
        member x.RootFlows() = dicRootFlow.Values
        member x.AddFlow(flow:RootFlow) = dicRootFlow.TryAdd(flow.Name, flow);
        member x.GetFlow(name:string)   = dicRootFlow.[name];
        //나의 시스템 Flag
        member val Active = false
        