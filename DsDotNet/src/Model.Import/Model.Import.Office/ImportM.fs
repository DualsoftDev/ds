// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open System.Collections.Concurrent
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Drawing
open DocumentFormat.OpenXml
open PPTX
open System.Collections.Generic
open System
open Microsoft.FSharp.Collections
open Engine.Common.FS
open Engine.Core
open Engine.Core.Util

[<AutoOpen>]
module ImportM =

    type internal ImportPowerPoint(path:string) =
        let doc = pptDoc(path)
        let dicSeg = ConcurrentDictionary<string, MSeg>()
        let dicEdge = ConcurrentDictionary<MEdge, MSeg>()  //childEdges, parentSeg
        let dicFlow  = ConcurrentDictionary<int, MFlow>()
        let model =  ImportModel(doc.FullPath)
        let mySys= MSys("MY", true)

        let getParent(edge:pptEdge) = 
           Check.SameParent(doc, edge)
           let parents = 
               doc.Parents 
               |> Seq.filter(fun group ->
                   group.Value.Contains(edge.StartNode) 
                   && group.Value.Contains(edge.EndNode))
               |> Seq.map(fun group -> 
                    edge.StartNode.ExistChildEdge <- true
                    edge.EndNode.ExistChildEdge   <- true
                    Some(group.Key), dicSeg.[group.Key.Key]  )
              
        
           if(parents.Any())
               then parents |> Seq.toArray
               else [(None ,mySys.SysSeg)] |> Seq.toArray

        let updateAlias(seg:MSeg, flow:MFlow) = 
            if(seg.Alias.IsSome) 
            then 
                let name = 
                    if(seg.NodeType.IsCall)
                    then seg.ValidName
                    else sprintf "EX.%s.EX" (seg.ToCallText())

                let alias = seg.Alias.Value

                if(flow.AliasSet.Keys.Contains(name))  
                 then flow.AliasSet.[name].Add( alias)|> ignore
                 else let set = HashSet<string>()
                      set.Add(alias)|> ignore
                      flow.AliasSet.TryAdd(name, set ) |> ignore

        let dicSameCheck = ConcurrentDictionary<string, MEdge>()
        let convertEdge(edge:pptEdge) = 
            let sSeg = dicSeg.[edge.StartNode.Key]
            let eSeg = dicSeg.[edge.EndNode.Key]
            updateAlias(sSeg, dicFlow.[edge.PageNum]) 
            updateAlias(eSeg, dicFlow.[edge.PageNum]) 
            getParent(edge) |> Seq.iter(fun (parentNode, parentSeg) ->
                        
                           dicFlow.[edge.PageNum].RemoveSingleNode(sSeg) |> ignore
                           dicFlow.[edge.PageNum].RemoveSingleNode(eSeg) |> ignore
                           let mEdge = MEdge(sSeg, eSeg, edge.Causal)

                           if(mEdge.Causal = Interlock)
                           then dicFlow.[edge.PageNum].AddInterlock(mEdge)

                           match parentNode with
                           |Some(v) -> if(v.PageNum = edge.PageNum) 
                                       then dicFlow.[edge.PageNum].AddSegDrawSub(parentSeg) 
                           |None -> dicFlow.[edge.PageNum].AddEdge(mEdge) |> ignore

                           Check.SameEdgeErr(parentNode, edge, mEdge, dicSameCheck)
                           dicEdge.TryAdd(mEdge, parentSeg) |>ignore
                          )

        
        member internal x.GetImportModel() = 
            try
            
                //모델만들기 및 시스템 등록
                model.Add(mySys) |> ignore
                //model.SetActive(mySys)

                //page 타이틀 중복체크 
                let dicSamePage = ConcurrentDictionary<string, pptPage>()
                let dicSameSeg  = ConcurrentDictionary<string, MSeg>()
                let dicSameFlow = ConcurrentDictionary<int, string>()
                
                doc.Pages |> Seq.filter(fun page -> page.IsUsing)
                          |> Seq.iter(fun page ->  
                                    let mFlowName = 
                                        let title = doc.GetPage(page.PageNum).Title
                                        if(title = "") then sprintf "P%d" page.PageNum else title
              
                                    dicSameFlow.TryAdd(page.PageNum, mFlowName)|>ignore)

                //segment 리스트 만들기
                doc.Nodes 
                |> Seq.iter(fun node -> 
                    Check.ValidMFlowPath(node, dicSameFlow)

                    let realMFlow, realName, bMyMFlow  = 
                        if(node.Name.Contains('.')) 
                        then node.Name.Split('.').[0] , node.Name.Split('.').[1], false
                        else dicSameFlow.[node.PageNum], node.Name, true

                    let btn = node.IsEmgBtn || node.IsStartBtn || node.IsAutoBtn || node.IsResetBtn 
                    let bound = if(btn) then ExBtn
                                else if(bMyMFlow) then ThisFlow else OtherFlow

                    let seg = MSeg(realName, mySys,  bound, node.NodeType, realMFlow, node.IsDummy)

                    seg.Update(node.Key, node.Id.Value, node.Alias, node.CntTX, node.CntRX)
                    dicSeg.TryAdd(node.Key, seg) |> ignore
                    if(node.IsDummy |> not) then  Check.SameNode(seg, node, dicSameSeg)   )
              
                //MFlow 리스트 만들기
                dicSameFlow
                |> Seq.iter(fun flow -> 
                                let pageNum  = flow.Key
                                let name  = flow.Value
                                if(mySys.Name = name) then Office.ErrorPPT(ErrorCase.Name, 31, $"시스템이름 : {mySys.Name}", pageNum, $"페이지이름 : {name}")
                                let mFlow  = MFlow(name, pageNum)
                                dicFlow.TryAdd(pageNum, mFlow) |> ignore

                                if(mySys.AddFlow(mFlow)|>not) 
                                then 
                                    Office.ErrorPPT(Page, 21, $"{name},  Same Page->{(mySys.GetFlow(name):?>MFlow).Page}",  pageNum)
                                )

                //Safety & EMG & Start & Auto 리스트 만들기
                doc.Nodes 
                |> Seq.filter(fun node -> node.IsDummy|>not)
                |> Seq.iter(fun node -> 
                                let mFlowName =  dicSameFlow.[node.PageNum]
                                let dic = dicSeg.Values.Select(fun seg -> seg.FullName, seg) |> dict
                               
                                //Start, Reset, Auto, Emg 버튼
                                if(node.IsStartBtn) then mySys.TryAddStartBTN(node.Name, dicFlow.[node.PageNum])
                                if(node.IsResetBtn) then mySys.TryAddResetBTN(node.Name, dicFlow.[node.PageNum])
                                if(node.IsAutoBtn)  then mySys.TryAddAutoBTN(node.Name,  dicFlow.[node.PageNum])
                                if(node.IsEmgBtn)   then mySys.TryAddEmergBTN(node.Name, dicFlow.[node.PageNum])
                                
                                //Safety
                                let safeSeg = 
                                    node.Safeties   //세이프티 입력 미등록 이름오류 체크
                                    |> Seq.map(fun safe ->  sprintf "%s.%s.%s" mySys.Name mFlowName safe)
                                    |> Seq.iter(fun safeFullName -> if(dic.ContainsKey safeFullName|>not) 
                                                                    then Office.ErrorName(node.Shape, 28, node.PageNum))


                                    node.Safeties   
                                    |> Seq.map(fun safe ->  sprintf "%s.%s.%s" mySys.Name mFlowName safe)
                                    |> Seq.map(fun safeFullName ->  dic.[safeFullName])

                                if(safeSeg.Any())
                                    then dicFlow.[node.PageNum].AddSafety(dicSeg.[node.Key], safeSeg)
                                )
                                
                //Dummy child 처리
                doc.Parents
                |> Seq.filter(fun group -> group.Key.IsDummy)
                |> Seq.map(fun group -> group.Key, group.Value)
                |> Seq.iter(fun (parent, children) -> 
                    let pSeg = dicSeg.[parent.Key]
                    children 
                    |> Seq.iter(fun child ->  
                                let cSeg = dicSeg.[child.Key]
                                pSeg.ChildFlow.AddSingleNode(cSeg) |> ignore
                                dicFlow.[parent.PageNum].RemoveSingleNode(cSeg) |> ignore
                               // child.ExistChildEdge <- true
                                )
                )
            
            
                //edge 리스트 만들기 (pptEdge를 변환하여 dicEdge에 등록)
                doc.Edges 
                |> Seq.iter(fun edge -> 
                                let sSeg = dicSeg.[edge.StartNode.Key]
                                let eSeg = dicSeg.[edge.EndNode.Key]
                                if(sSeg.IsDummy || eSeg.IsDummy)
                                then 
                                    let srcs = if(sSeg.NoEdgeSegs.Any()) then sSeg.NoEdgeSegs |> Seq.toList else [sSeg]
                                    let tgts = if(eSeg.NoEdgeSegs.Any()) then eSeg.NoEdgeSegs |> Seq.toList else [eSeg]

                                    srcs
                                    |> Seq.iter(fun src ->
                                            tgts
                                            |> Seq.iter(fun tgt -> 
                                                let edge = pptEdge(edge.ConnectionShape, edge.Id, edge.PageNum, src.ShapeID, tgt.ShapeID , doc.DicNodes)
                                                convertEdge(edge)
                                                ))
                                
                                else 
                                    convertEdge(edge)
                                )

                //NoEdge child 처리
                doc.Parents
                |> Seq.filter(fun group -> group.Key.IsDummy |>not)
                |> Seq.map(fun group -> group.Key, group.Value)
                |> Seq.iter(fun (parent, children) ->
                            let pSeg = dicSeg.[parent.Key]
                            children 
                            |> Seq.filter(fun child -> child.ExistChildEdge|>not) //엣지 할당 못받은 자식만
                            |> Seq.filter(fun child -> child.IsDummy|>not) 
                            |> Seq.iter(fun child -> 
                                                updateAlias(dicSeg.[child.Key], dicFlow.[parent.PageNum]) 
                                                
                                                //행위 부모 할당후 
                                                pSeg.ChildFlow.AddSingleNode(dicSeg.[child.Key]) |> ignore
                                                //MFlow 상에서 삭제
                                                dicFlow.[parent.PageNum].RemoveSingleNode(dicSeg.[child.Key]) |> ignore 
                                                dicFlow.[parent.PageNum].AddSegDrawSub(pSeg) 
                                                    )
                )

                //시스템에 인과모델 등록
                dicEdge
                |> Seq.iter(fun edge -> model.AddEdge(edge.Key, edge.Value))
            
                //Call 위치정보 업데이트 (마지막 페이지만 정보 반영)
            
                doc.Nodes 
                |> Seq.filter(fun node -> node.PageNum = doc.VisibleLast().PageNum)
                |> Seq.filter(fun node -> node.Name = ""|>not)
                |> Seq.filter(fun node -> dicSeg.[node.Key].Bound = ThisFlow)
                |> Seq.filter(fun node -> node.NodeType = TX || node.NodeType = TR || node.NodeType = RX )
                |> Seq.iter(fun node -> mySys.LocationSet.TryAdd(dicSeg.[node.Key].FullName, node.Rectangle) |> ignore)
            
                MSGInfo($"전체 장표   count [{doc.Pages.Count()}]")
                MSGInfo($"전체 도형   count [{doc.Nodes.Count()}]")
                MSGInfo($"전체 연결   count [{doc.Edges.Count()}]")
                MSGInfo($"전체 부모   count [{doc.Parents.Keys.Count}]")
                model

            
            with ex ->  MSGError  $"{ex.Message}"
                        model
                        
           
                    

    let FromPPTX(path:string) =
        let ppt = ImportPowerPoint(path)
        DoWork(20);
        let model = ppt.GetImportModel()
        DoWork(50);
        model
        


