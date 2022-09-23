// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Diagnostics
open System.Collections.Generic
open System.Collections.Concurrent
open CoreFlow
open CoreClass

[<AutoOpen>]
module CoreStruct =

    [<DebuggerDisplay("{name}")>]
    type DsSystem(name:string, model:Model)  =
        inherit SysBase(name)
        let dicRootFlow = ConcurrentDictionary<string, RootFlow>()
      
        override x.ToText() = name
        member x.Model = model
        member x.RootFlows = dicRootFlow.Values
        member x.AddFlow(flow:RootFlow) = dicRootFlow.TryAdd(flow.Name, flow);
        member x.GetFlow(name:string)   = dicRootFlow.[name];
        //나의 시스템 Flag
        member val Active = false
        
        //시스템 버튼 소속 Flow 정보
        member val EmergencyButtons = ConcurrentDictionary<string, List<RootFlow>>()
        member val AutoButtons      = ConcurrentDictionary<string, List<RootFlow>>()
        member val StartButtons     = ConcurrentDictionary<string, List<RootFlow>>()
        member val ResetButtons     = ConcurrentDictionary<string, List<RootFlow>>()
         
    and
        [<DebuggerDisplay("{name}")>]
        Model() =
            let systems = HashSet<DsSystem>()
            let cpus    = HashSet<ICpu>()
            let vpss    = HashSet<RootFlow>()
            member x.Cpus = cpus
            /// <summary> 가상 부모 목록.  debugging 용 </summary>
            member x.VPSs = vpss

            //모델에 시스템 등록 및 삭제
            member x.Add(sys:DsSystem) = systems.Add(sys)
            member x.Remove(sys:DsSystem) = systems.Remove(sys)

            /// TotalSystems
            member x.Systems      = systems
            member x.SysActives  = 
                let activeSys = systems |> Seq.filter (fun sys -> sys.Active)
                if((activeSys |> Seq.length) <> 1) then failwith "한개 이상의 Active 시스템 설정이 필요합니다."
            member x.AllFlows      = systems |> Seq.collect(fun sys -> sys.RootFlows) |> HashSet


            //시스템 인과 추가 방법 모델에서만 가능
            member x.AddEdge(edgeInfo:DsEdge, parent:SegmentBase)    = parent.ChildFlow.AddEdge(edgeInfo) |> ignore
            member x.AddEdge(edgeInfo:DsEdge, rootFlow:RootFlow) = rootFlow.AddEdge(edgeInfo) |> ignore
