// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open Engine.Core
open System.Collections.Concurrent
open Engine.Common.FS

[<AutoOpen>]
///MModel -> CoreModule.Model
///Msys   -> CoreModule.DsSystem
///MFlow  -> CoreModule.Flow
///MSeg   -> CoreModule.Segment
///MSeg(IsChild) -> CoreModule.Child
module ConvertM =
    
    let ToDs(pptModel:MModel) =
        let coreModel = CoreModule.Model()
        //중복 등록 체크용
        let dicChild = ConcurrentDictionary<string, ApiItem>()

        ///mEdges   -> CoreModule.Child
        let convertChildren(mSeg:MSeg, coreSeg:CoreModule.RealInFlow, coreModel:CoreModule.Model) =   

            ///MSeg   -> CoreModule.Child
            let convertChild(mChildSeg:MSeg) = 
                if mChildSeg.IsAlias
                then AliasInReal.Create(mChildSeg.AliasName, dicChild.[mChildSeg.AliasOrg.Value.Name], coreSeg) :> NodeInReal
                else 
                     let api = 
                        if dicChild.ContainsKey(mChildSeg.Name) 
                        then dicChild.[mChildSeg.Name]
                        else 
                            let newApi= ApiItem.Create(mChildSeg.ApiName, coreModel.FindSystem(mChildSeg.BaseSys.Name))
                            dicChild.TryAdd(mChildSeg.Name, newApi) |> ignore
                            newApi

                     CallInReal.CreateOnDemand(api, coreSeg) :> NodeInReal

            let gr = coreSeg.Graph
             ///MEdge   -> CoreModule.Flow
            let convertChildEdge(mEdge:MEdge, coreSeg:CoreModule.RealInFlow) = 
                let s = dicChild.[mEdge.Source.Name].QualifiedName
                let t = dicChild.[mEdge.Target.Name].QualifiedName
                let src = gr.FindVertex(s)
                let tgt = gr.FindVertex(t)
                let edgeType = mEdge.Causal

                let findList = gr.Vertices |> Seq.map(fun v->v.Name) |> String.concat "\n " 
                if(src.IsNull()) then failwithf $"[{findList}]에서 \n{s}를 찾을 수 없습니다."
                if(tgt.IsNull()) then failwithf $"[{findList}]에서 \n{t}를 찾을 수 없습니다."


                InSegmentEdge.Create(coreSeg, src, tgt, edgeType)
            

            mSeg.ChildFlow.Nodes |> Seq.cast<MSeg> 
                                 |> Seq.sortBy(fun seg -> seg.IsAlias)
                                 |> Seq.filter(fun seg -> seg.IsDummy|>not)
                                 |> Seq.iter(fun node -> gr.AddVertex(convertChild (node)) |>ignore)
            mSeg.ChildFlow.Edges |> Seq.cast<MEdge> 
                                 |> Seq.filter(fun edge -> edge.IsDummy|>not)
                                 |> Seq.iter(fun edge -> gr.AddEdge  (convertChildEdge (edge, coreSeg)) |>ignore)
    
        //CoreModule.Flow 에 pptEdge 등록
        let addInFlowEdges(coreFlow:CoreModule.Flow, mFlow:MFlow) =
            ///MSeg   -> CoreModule.Segment
            let convertSeg(mSeg:MSeg, coreFlow:CoreModule.Flow) = 
                if mSeg.IsAlias
                then AliasInFlow.Create(mSeg.ValidName, coreFlow, mSeg.AliasOrg.Value.FullName.Split('.')) :> NodeInFlow
                else 
                     let coreSeg = RealInFlow.Create(mSeg.ValidName, coreFlow)
                     convertChildren (mSeg, coreSeg, coreModel)  |> ignore
                     coreSeg :> NodeInFlow

            ///MEdge   -> CoreModule.Flow
            let convertRootEdge(mEdge:MEdge, coreFlow:CoreModule.Flow) = 

                let s = mEdge.Source.ValidName
                let t = mEdge.Target.ValidName
                let src = coreFlow.Graph.FindVertex(s)
                let tgt = coreFlow.Graph.FindVertex(t)

                let findList = coreFlow.Graph.Vertices |> Seq.map(fun v->v.Name) |> String.concat "\n " 
                if(src.IsNull()) then failwithf $"[{findList}]에서 \n{s}를 찾을 수 없습니다."
                if(tgt.IsNull()) then failwithf $"[{findList}]에서 \n{t}를 찾을 수 없습니다."
                

                if(mEdge.Causal = Interlock)
                then 
                     InFlowEdge.Create(coreFlow, src, tgt, ResetPush ) |> ignore
                     InFlowEdge.Create(coreFlow, tgt, src, ResetPush)
                elif(mEdge.Causal = StartReset)
                then 
                     InFlowEdge.Create(coreFlow, src, tgt, StartEdge) |> ignore
                     InFlowEdge.Create(coreFlow, tgt, src, ResetEdge)
                else 
                     InFlowEdge.Create(coreFlow, src, tgt, mEdge.Causal)

            mFlow.Nodes |> Seq.distinct |> Seq.cast<MSeg> 
                        |> Seq.filter(fun seg -> seg.IsDummy|>not)
                        |> Seq.iter(fun node -> coreFlow.Graph.AddVertex(convertSeg (node, coreFlow)) |>ignore)
            mFlow.Edges |> Seq.cast<MEdge> 
                        |> Seq.filter(fun edge -> edge.IsDummy|>not)
                        |> Seq.iter(fun edge -> coreFlow.Graph.AddEdge(convertRootEdge (edge, coreFlow)) |>ignore)

        //CoreModule.DsSystem 에 pptFlow 등록
        let addFlows(coreSys:DsSystem, mFlows:MFlow seq) =
            mFlows |> Seq.iter(fun mflow ->
                let coreFlow = Flow.Create(mflow.Name, coreSys) 
                addInFlowEdges(coreFlow, mflow)
                )

        let getFlows(mflow:ResizeArray<RootFlow>) =
            mflow 
            |> Seq.cast<MFlow>
            |> Seq.map(fun flow -> coreModel.FindGraphVertex([|flow.System.Name; flow.Name|]))
            |> Seq.cast<Flow>
            |> ResizeArray

        //시스템 변환
        pptModel.Systems
        |> Seq.iter(fun sys -> DsSystem.Create(sys.Name, null, None, coreModel) |>ignore)
        
        //시스템 별 Flow 변환
        pptModel.Systems
        |> Seq.iter(fun sys -> addFlows(coreModel.FindSystem(sys.Name), sys.Flows))

        
        //시스템 별 버튼 반영
        pptModel.Systems
        |> Seq.iter(fun pptSys -> 
            let coreSys = coreModel.FindSystem(pptSys.Name)
            pptSys.EmergencyButtons.ForEach(fun btn -> coreSys.EmergencyButtons.Add(btn.Key, getFlows(btn.Value)))
            pptSys.AutoButtons.ForEach(fun btn -> coreSys.AutoButtons.Add(btn.Key, getFlows(btn.Value)))
            pptSys.StartButtons.ForEach(fun btn -> coreSys.StartButtons.Add(btn.Key, getFlows(btn.Value)))
            pptSys.ResetButtons.ForEach(fun btn -> coreSys.ResetButtons.Add(btn.Key, getFlows(btn.Value)))
                )
          
        coreModel




