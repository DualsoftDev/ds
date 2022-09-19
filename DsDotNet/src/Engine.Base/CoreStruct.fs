// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Diagnostics
open System.Collections.Generic

[<AutoOpen>]
module CoreStruct =

    [<DebuggerDisplay("{name}")>]
    type DsSystem(name:string, model:DsModel)  =
        inherit SysBase(name)
        let rootFlows = HashSet<RootFlow>()
        
        member x.Model = model
        member x.RootFlows = rootFlows
        member val Active = false


    and
        [<DebuggerDisplay("{name}")>]
        DsModel(name:string) =
            let systems = HashSet<DsSystem>()
            let cpus = HashSet<DsCpu>()
            let vpss = HashSet<RootFlow>()
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

            ///사용자 모델링 기본형 : parentSeg는 모델링시에 엣지의 부모를 할당받음
            //member x.AddEdge(edgeInfo:DsEdge, parent:Segment) = x.AddEdges([edgeInfo], parent)
            //member x.AddEdges(edgeInfos:DsEdge seq, parent:Segment) =
            //    edgeInfos |> Seq.iter (fun e -> x.EdgeAdd(e, Some parent))

            //member private x.EdgeAdd(edge:DsEdge, pSeg:Segment option) =
            //    //시스템 등록 Check 및 사용된 UsedSegs System Add
            //    edge.Nodes |> Seq.cast<Segment>
            //    |> Seq.iter(fun seg-> 
            //        let mySystem = seg.SysBase:?> DsSystem
            //        if not (x.Systems.Contains(mySystem)) 
            //        then failwith $"model({x.Name})에 해당 {seg.Name}의 System 등록 필요. model.add(system) 필요합니다."
            //        else ())
            //            //if pSeg.IsNone then seg.SetParent(seg.BaseSys.SysSeg))
                        

            //    let newParent = if pSeg.IsSome 
            //                    then pSeg.Value 
            //                    else edge.Target.SysBase.SystemSeg :?> Segment

            //    newParent.ChildFlow.AddEdge(edge) |> ignore

            


            
