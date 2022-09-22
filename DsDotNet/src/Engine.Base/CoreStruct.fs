// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Diagnostics
open System.Collections.Generic
open System.Collections.Concurrent

[<AutoOpen>]
module CoreStruct =

    [<DebuggerDisplay("{name}")>]
    type DsSystem(name:string)  =
        inherit SysBase(name)
        let dicRootFlow = ConcurrentDictionary<string, RootFlow>()
        let emgSet  = ConcurrentDictionary<string, List<RootFlow>>()
        let startSet  = ConcurrentDictionary<string, List<RootFlow>>()
        let resetSet  = ConcurrentDictionary<string, List<RootFlow>>()
        let autoSet   = ConcurrentDictionary<string, List<RootFlow>>()
        
        member x.RootFlows() = dicRootFlow.Values
        member x.AddFlow(flow:RootFlow) = dicRootFlow.TryAdd(flow.Name, flow);
        member x.GetFlow(name:string)   = dicRootFlow.[name];
        //나의 시스템 Flag
        member val Active = false

        member val BtnEmgSet = ConcurrentDictionary<string, List<RootFlow>>()
        member val BtnStartSet = startSet
        member val BtnResetSet = resetSet
        member val BtnAutoSet = autoSet
         
    and
        [<DebuggerDisplay("{name}")>]
        DsModel(name:string) =
            let systems = HashSet<DsSystem>()
            let cpus    = HashSet<ICpu>()
            let vpss    = HashSet<RootFlow>()
            member x.Cpus = cpus
            /// <summary> 가상 부모 목록.  debugging 용 </summary>
            member x.VPSs = vpss
            member x.Name = name

            //모델에 시스템 등록 및 삭제
            member x.Add(sys:DsSystem) = systems.Add(sys)
            member x.Remove(sys:DsSystem) = systems.Remove(sys)

            /// TotalSystems
            member x.Systems      = systems
            member x.SysActives  = 
                let activeSys = systems |> Seq.filter (fun sys -> sys.Active)
                if((activeSys |> Seq.length) <> 1) then failwith "한개 이상의 Active 시스템 설정이 필요합니다."
            member x.AllFlows      = systems |> Seq.collect(fun sys -> sys.RootFlows()) |> HashSet


            //시스템 인과 추가 방법 모델에서만 가능
            member x.AddEdge(edgeInfo:DsEdge, parent:Segment)    = parent.ChildFlow.AddEdge(edgeInfo) |> ignore
            member x.AddEdge(edgeInfo:DsEdge, rootFlow:RootFlow) = rootFlow.AddEdge(edgeInfo) |> ignore
