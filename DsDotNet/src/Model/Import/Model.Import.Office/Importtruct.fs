// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Diagnostics
open System.Collections.Generic
open System.Collections.Concurrent
open Engine.Core

[<AutoOpen>]
module CoreStruct =

    [<DebuggerDisplay("{name}")>]
    type MSystem(name:string, model:ModelBase) =
        inherit SysBase(name)
        let dicRootFlow = ConcurrentDictionary<string, RootFlow>()

        member x.Model = model

        override x.Add(flow:IFlow)  = 
                    let flow = flow :?> RootFlow
                    dicRootFlow.TryAdd(flow.Name, flow)

        member x.RootFlows = dicRootFlow.Values
        member x.GetFlow(name:string)  
                    = dicRootFlow.[name];
    

        //시스템 버튼 소속 Flow 정보
        member val EmergencyButtons = ConcurrentDictionary<string, ResizeArray<RootFlow>>()
        member val AutoButtons      = ConcurrentDictionary<string, ResizeArray<RootFlow>>()
        member val StartButtons     = ConcurrentDictionary<string, ResizeArray<RootFlow>>()
        member val ResetButtons     = ConcurrentDictionary<string, ResizeArray<RootFlow>>()
         
    and
        [<DebuggerDisplay("{name}")>]
        ModelBase() =
            class end
      