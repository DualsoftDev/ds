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
        let getApi(mSeg:MSeg) = 
                if dicChild.ContainsKey(mSeg.Name) 
                then dicChild.[mSeg.Name]
                else 
                    let newApi= ApiItem.Create(mSeg.ApiName, coreModel.FindSystem(mSeg.BaseSys.Name))
                    dicChild.TryAdd(mSeg.Name, newApi) |> ignore
                    newApi

        let convertChildren(mSeg:MSeg, coreSeg:CoreModule.Real, coreModel:CoreModule.Model) =   

            ///MSeg   -> CoreModule.Call or CoreModule.Alias
            let convertChild(mChildSeg:MSeg) = 
                if mChildSeg.IsAlias
                then Alias.CreateInReal(mChildSeg.ApiName, dicChild.[mChildSeg.AliasOrg.Value.Name], coreSeg) :> Vertex
                else 
                     let api = getApi(mChildSeg)
                     Call.CreateInReal(api, coreSeg) :> Vertex

            let gr = coreSeg.Graph
             ///MEdge   -> CoreModule.Flow
            let convertChildEdge(mEdge:MEdge, coreSeg:CoreModule.Real) = 
                let s = dicChild.[mEdge.Source.Name].QualifiedName
                let t = dicChild.[mEdge.Target.Name].QualifiedName
                let src = gr.FindVertex(s)
                let tgt = gr.FindVertex(t)
                let edgeType = mEdge.Causal

                let findList = gr.Vertices |> Seq.map(fun v->v.Name) |> String.concat "\n " 
                if(src.IsNull()) then failwithf $"[{findList}]에서 \n{s}를 찾을 수 없습니다."
                if(tgt.IsNull()) then failwithf $"[{findList}]에서 \n{t}를 찾을 수 없습니다."


                Edge.Create(coreSeg.Graph, src, tgt, edgeType)
            

            mSeg.ChildFlow.Nodes |> Seq.cast<MSeg> 
                                 |> Seq.sortBy(fun seg -> seg.IsAlias)
                                 |> Seq.filter(fun seg -> seg.NodeType.IsReal || seg.NodeType.IsCall)
                                 |> Seq.iter(fun node -> gr.AddVertex(convertChild (node)) |>ignore)
            mSeg.ChildFlow.Edges |> Seq.cast<MEdge> 
                                 |> Seq.filter(fun edge -> edge.IsDummy|>not)
                                 |> Seq.iter(fun edge -> gr.AddEdge  (convertChildEdge (edge, coreSeg)) |>ignore)
       
           
        //CoreModule.DsSystem 에 pptFlow 등록
        let addFlows(coreSys:DsSystem, mFlows:MFlow seq) =
             ///MSeg   -> CoreModule.Segment
            let convertSeg(mSeg:MSeg, coreFlow:CoreModule.Flow) = 
                    if mSeg.IsAlias
                    then Alias.CreateInFlow(mSeg.ValidName, mSeg.AliasOrg.Value.FullName.Split('.'), coreFlow) :> Vertex
                    else 
                        if mSeg.NodeType.IsReal
                            then let coreSeg = Real.Create(mSeg.ValidName, coreFlow)
                                 convertChildren (mSeg, coreSeg, coreModel)  |> ignore
                                 coreSeg :> Vertex
                            else Call.CreateInFlow(getApi(mSeg), coreFlow)

            ///MEdge   -> CoreModule.Flow
            let convertRootEdge(mEdge:MEdge, coreFlow:CoreModule.Flow) = 

                let s = mEdge.Source.ValidName
                let t = mEdge.Target.ValidName
                let graph = coreFlow.Graph
                let src = graph.FindVertex(s)
                let tgt = graph.FindVertex(t)

                let findList = graph.Vertices |> Seq.map(fun v->v.Name) |> String.concat "\n " 
                if(src.IsNull()) then failwithf $"[{findList}]에서 \n{s}를 찾을 수 없습니다."
                if(tgt.IsNull()) then failwithf $"[{findList}]에서 \n{t}를 찾을 수 없습니다."
                
                if(mEdge.Causal = Interlock)
                then 
                        Edge.Create(graph, src, tgt, ResetPush ) |> ignore
                        Edge.Create(graph, tgt, src, ResetPush)
                elif(mEdge.Causal = StartReset)
                then 
                        Edge.Create(graph, src, tgt, StartEdge) |> ignore
                        Edge.Create(graph, tgt, src, ResetEdge)
                else 
                        Edge.Create(graph, src, tgt, mEdge.Causal)
        

            mFlows |> Seq.iter(fun mflow ->
                let coreFlow = Flow.Create(mflow.Name, coreSys) 
                mflow.Nodes |> Seq.cast<MSeg> 
                            |> Seq.distinctBy(fun seg -> seg.FullName) 
                            |> Seq.filter(fun seg -> seg.NodeType.IsRealorCall)
                            |> Seq.iter(fun node  -> coreFlow.Graph.AddVertex(convertSeg (node, coreFlow)) |>ignore)
                        
                mflow.Edges |> Seq.cast<MEdge> 
                            |> Seq.filter(fun edge -> edge.IsDummy|>not)
                            |> Seq.filter(fun edge -> edge.Source.NodeType.IsRealorCall && edge.Target.NodeType.IsRealorCall)
                            |> Seq.iter(fun edge -> coreFlow.Graph.AddEdge(convertRootEdge (edge, coreFlow)) |>ignore)
  
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
        |> Seq.iter(fun sys -> 
                let coreSys = coreModel.FindSystem(sys.Name)
                if sys.Active then coreSys.Active <- true
                addFlows(coreSys, sys.Flows))

        
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




