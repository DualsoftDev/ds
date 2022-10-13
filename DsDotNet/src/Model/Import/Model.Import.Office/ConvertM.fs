// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open Engine.Core
open System.Collections.Concurrent

[<AutoOpen>]
///MModel -> CoreModule.Model
///Msys   -> CoreModule.DsSystem
///MFlow  -> CoreModule.Flow
///MSeg   -> CoreModule.Segment
module ConvertM =

    ///mEdges   -> CoreModule.Child
    let private convertChildren(mSeg:MSeg, coreSeg:CoreModule.Segment, coreModel:CoreModule.Model) =   
        //중복 등록 체크용
        let dicChild = ConcurrentDictionary<string, ApiItem>()

        ///MSeg   -> CoreModule.Child
        let convertChild(mChildSeg:MSeg) = 
            if mChildSeg.IsAlias
            then ChildAliased.Create(mChildSeg.Name, dicChild.[mChildSeg.Alias.Value.FullName], coreSeg) :> Child
            else 
                 let orgApi = ApiItem.Create(mChildSeg.Name, coreModel.FindSystem(mChildSeg.BaseSys.Name))
                 dicChild.TryAdd(mChildSeg.FullName, orgApi) |> ignore

                 ChildApiCall.Create(orgApi, coreSeg) :> Child

         ///MEdge   -> CoreModule.Flow
        let convertChildEdge(mEdge:MEdge, coreSeg:CoreModule.Segment) = 
            let src = coreSeg.Graph.FindVertex(mEdge.Source.Name)
            let tgt = coreSeg.Graph.FindVertex(mEdge.Target.Name)
            let edgeType = EdgeHelper.GetEdgeType(mEdge.Causal)

            let findList = coreSeg.Graph.Vertices |> Seq.map(fun v->v.Name) |> String.concat "\n " 
            try if(src.Name = null) then ()    with ex -> failwithf $"{findList}에서 \n{mEdge.Source.Name}를 찾을 수 없습니다."
            try if(tgt.Name = null) then ()    with ex -> failwithf $"{findList}에서 \n{mEdge.Target.Name}를 찾을 수 없습니다."


            InSegmentEdge.Create(coreSeg, src, tgt, edgeType)
            

        mSeg.ChildFlow.Nodes |> Seq.cast<MSeg> 
                             |> Seq.sortBy(fun seg -> seg.IsAlias)
                             |> Seq.iter(fun node -> coreSeg.Graph.AddVertex(convertChild (node)) |>ignore)
        mSeg.ChildFlow.Edges |> Seq.cast<MEdge> 
                             |> Seq.iter(fun edge -> coreSeg.Graph.AddEdge  (convertChildEdge (edge, coreSeg)) |>ignore)
    
    let ToDs(pptModel:MModel) =
        let coreModel = CoreModule.Model()

        //CoreModule.Flow 에 pptEdge 등록
        let addInFlowEdges(coreFlow:CoreModule.Flow, mFlow:MFlow) =
            ///MSeg   -> CoreModule.Segment
            let convertSeg(mSeg:MSeg, coreFlow:CoreModule.Flow) = 
                if mSeg.IsAlias
                then SegmentAlias.Create(mSeg.Name, coreFlow, mSeg.Alias.Value.FullName.Split('.')) :> SegmentBase
                else 
                     let coreSeg = Segment.Create(mSeg.Name, coreFlow)
                     convertChildren (mSeg, coreSeg, coreModel)  |> ignore
                     coreSeg :> SegmentBase

            ///MEdge   -> CoreModule.Flow
            let convertRootEdge(mEdge:MEdge, coreFlow:CoreModule.Flow) = 

                let src = coreFlow.Graph.FindVertex(mEdge.Source.Name)
                let tgt = coreFlow.Graph.FindVertex(mEdge.Target.Name)
                let edgeType = EdgeHelper.GetEdgeType(mEdge.Causal)

                InFlowEdge.Create(coreFlow, src, tgt, edgeType)

            mFlow.Nodes |> Seq.distinct |> Seq.cast<MSeg> 
                        |> Seq.iter(fun node -> coreFlow.Graph.AddVertex(convertSeg (node, coreFlow)) |>ignore)
            mFlow.Edges |> Seq.cast<MEdge> 
                        |> Seq.iter(fun edge -> coreFlow.Graph.AddEdge(convertRootEdge (edge, coreFlow)) |>ignore)

        //CoreModule.DsSystem 에 pptFlow 등록
        let addFlows(coreSys:DsSystem, mFlows:MFlow seq) =
            mFlows |> Seq.iter(fun mflow ->
                let coreFlow = Flow.Create(mflow.Name, coreSys) 
                addInFlowEdges(coreFlow, mflow)
                )
        
        //시스템 변환
        pptModel.Systems
        |> Seq.iter(fun sys ->
            DsSystem.Create(sys.Name, None, coreModel) |>ignore
            )

        //시스템 별 Flow 변환
        pptModel.Systems
        |> Seq.iter(fun sys ->
            addFlows(coreModel.FindSystem(sys.Name), sys.Flows)
            )

        coreModel




