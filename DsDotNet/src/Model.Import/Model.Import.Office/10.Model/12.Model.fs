// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office
open Util
open System.IO


[<AutoOpen>]
module Model =
    /// 모델 생성시, ActiveSystem은 반드시 시스템 중 하나 지정되어야 한다.(AddEdge시 Active 기준 부모 탐색)
    type DsModel(name:string) =
        
        let systems =  ConcurrentHash<DsSys>()

        member x.Path = name
        member x.Name = Path.GetFileNameWithoutExtension(name) 
     
        //모델에 시스템 등록 및 삭제
        member x.Add(sys:DsSys) = systems.TryAdd(sys)
        member x.AddRange(newSystems:DsSys seq) = 
            newSystems |> Seq.iter (fun sys -> x.Add(sys) |> ignore)
        member x.Remove(sys:DsSys) = systems.TryRemove(sys)

        /// TotalSystems
        member x.TotalSystems      = systems.Values
        /// The No ActiveSystem
        member x.PassiveSystems    = systems.Values  |> Seq.filter (fun sys -> not sys.Active)
        /// The ActiveSystem
        member x.SetActive(active) = systems.Values  |> Seq.iter (fun sys ->  sys.Active <- (sys = active) )
                                    
        member x.ActiveSys  = 
            let activeSys = systems.Values |> Seq.filter (fun sys -> sys.Active)
            if((activeSys |> Seq.length) <> 1) then failwith "The number of ActiveSystem must be 'ONE'."
            activeSys |> Seq.head
        ///사용자 모델링 기본형 : parentSeg는 모델링시에 엣지의 부모를 할당받음
        member x.AddEdge(edgeInfo:MEdge, parent:Seg) = x.AddEdges([edgeInfo], parent)
        member x.AddEdges(edgeInfos:MEdge seq, parent:Seg) =
            edgeInfos |> Seq.iter (fun e -> x.EdgeAdd(e, Some parent))

        member private x.EdgeAdd(mEdge:MEdge, pSeg:Seg option) =
            //시스템 등록 Check 및 사용된 UsedSegs System Add
            mEdge.Nodes |> Seq.cast<Seg>
            |> Seq.iter(fun seg-> 
                if not (x.TotalSystems.Contains(seg.BaseSys)) 
                then failwith $"model({x.Name})에 해당 {seg.SegName}의 System 등록 필요. model.add(system) 필요합니다."
                else
                    if pSeg.IsNone then seg.Parent <- Some(seg.BaseSys.SysSeg)
                    )

            let newParent = if pSeg.IsSome then pSeg.Value else x.ActiveSys.SysSeg

            newParent.AddChildNSetParent(mEdge)

