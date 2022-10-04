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
    

    //중복 등록 체크용
    let dicChild = ConcurrentDictionary<string, string>()

    ///mEdges   -> CoreModule.Child
    let private convertChildren(mSeg:Segment, coreSeg:CoreModule.Segment, coreSys:CoreModule.DsSystem) =   
        let getNode(seg:MSeg) = coreSeg.Graph.FindVertex($"{seg.BaseSys.Name}.{seg.ValidName}")
        ///MSeg   -> CoreModule.Child
        let convertChild(mSeg:MSeg) = 
            if mSeg.IsAlias
            then ChildAliased.Create(mSeg.Name, ApiItem.Create(mSeg.Name, coreSys), coreSeg) :> Child
            else 
                 let coreCall = ChildApiCall.Create(ApiItem.Create(mSeg.Name, coreSys), coreSeg)
                 coreCall :> Child

         ///MEdge   -> CoreModule.Flow
        let convertChildEdge(mEdge:MEdge, coreSeg:CoreModule.Segment) = 

            let src = getNode(mEdge.Source)
            let tgt = getNode(mEdge.Target)
            let edgeType = EdgeHelper.GetEdgeType(mEdge.Causal)

            InSegmentEdge.Create(coreSeg, src, tgt, edgeType)
            

        mSeg.ChildFlow.Nodes |> Seq.cast<MSeg> |> Seq.filter(fun seg -> dicChild.TryAdd(seg.FullName, seg.FullName))
                             |> Seq.iter(fun node -> coreSeg.Graph.AddVertex(convertChild (node)) |>ignore)
        mSeg.ChildFlow.Edges |> Seq.cast<MEdge> 
                             |> Seq.iter(fun edge -> coreSeg.Graph.AddEdge  (convertChildEdge (edge, coreSeg)) |>ignore)
    
    let ToDs(pptModel:MModel) =
        let coreModel = CoreModule.Model()
        dicChild.Clear();

        //CoreModule.Flow 에 pptEdge 등록
        let addInFlowEdges(coreFlow:CoreModule.Flow, mFlow:MFlow) =
            ///MSeg   -> CoreModule.Segment
            let convertSeg(mSeg:MSeg, coreFlow:CoreModule.Flow) = 
                if mSeg.IsAlias
                then SegmentAlias.Create(mSeg.Name, coreFlow, mSeg.Alias.Value.FullName.Split('.')) :> SegmentBase
                else 
                     let coreSeg = Segment.Create(mSeg.Name, coreFlow)
                     convertChildren (mSeg, coreSeg, coreFlow.System)  |> ignore
                     coreSeg :> SegmentBase

            ///MEdge   -> CoreModule.Flow
            let convertRootEdge(mEdge:MEdge, coreFlow:CoreModule.Flow) = 
                let getNode(seg:MSeg) = coreFlow.Graph.FindVertex(seg.Name)

                let src = getNode(mEdge.Source)
                let tgt = getNode(mEdge.Target)
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
        
        pptModel.Systems
        |> Seq.iter(fun sys ->
            let coreSys = DsSystem.Create(sys.Name, None, coreModel)
            addFlows(coreSys, sys.Flows)  )


        coreModel




