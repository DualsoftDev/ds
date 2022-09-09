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

[<AutoOpen>]
module ImportModel =

    type internal ImportPowerPoint(path:string) =
        let doc = pptDoc(path)
        let dicSeg = ConcurrentDictionary<string, Seg>()
        let dicEdge = ConcurrentDictionary<MEdge, Seg>()  //childEdges, parentSeg
        let model =  DsModel(doc.FullPath)
        let mySys= DsSystem("MY", true)

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

        let updateAlias(node:pptNode) = 
            if(node.Alias.IsSome) 
            then if(mySys.AliasSet.Keys.Contains(node.Name))
                 then mySys.AliasSet.[node.Name].Add( node.Alias.Value )|> ignore
                 else let set = HashSet<string>()
                      set.Add(node.Alias.Value)|> ignore
                      mySys.AliasSet.TryAdd(node.Name, set ) |> ignore

        let dicSameCheck = ConcurrentDictionary<string, MEdge>()
        let convertEdge(edge:pptEdge) = 
            let sSeg = dicSeg.[edge.StartNode.Key]
            let eSeg = dicSeg.[edge.EndNode.Key]
            updateAlias(edge.StartNode) 
            updateAlias(edge.EndNode) 
            getParent(edge) |> Seq.iter(fun (parentNode, parentSeg) ->
                        
                           mySys.Flos.[edge.PageNum].RemoveSegNoEdge(sSeg) 
                           mySys.Flos.[edge.PageNum].RemoveSegNoEdge(eSeg) 
                           let mEdge = MEdge(sSeg, eSeg, edge.Causal)

                           if(mEdge.Causal = Interlock)
                           then mySys.Flos.[edge.PageNum].AddInterlock(mEdge)

                           match parentNode with
                           |Some(v) -> if(v.PageNum = edge.PageNum) 
                                       then mySys.Flos.[edge.PageNum].AddSegDrawSub(parentSeg) 
                           |None -> mySys.Flos.[edge.PageNum].AddEdge(mEdge)

                           Check.SameEdgeErr(parentNode, edge, mEdge, dicSameCheck)
                           dicEdge.TryAdd(mEdge, parentSeg) |>ignore
                          )

        
        member internal x.GetDsModel() = 
            try
            
                //모델만들기 및 시스템 등록
                model.Add(mySys) |> ignore
                model.SetActive(mySys)

                //page 타이틀 중복체크 
                let dicSamePage = ConcurrentDictionary<string, pptPage>()
                let dicSameSeg  = ConcurrentDictionary<string, Seg>()
                let dicFloName  = ConcurrentDictionary<int, string>()
                
                doc.Pages |> Seq.iter(fun page ->  Check.SamePage(page, dicSamePage))
                doc.Pages |> Seq.iter(fun page ->  
                                    let flowName = 
                                        let title = doc.GetPage(page.PageNum).Title
                                        if(title = "") then sprintf "P%d" page.PageNum else title
              
                                    dicFloName.TryAdd(page.PageNum, flowName)|>ignore)

                //segment 리스트 만들기
                doc.Nodes 
                |> Seq.iter(fun node -> 
                    Check.ValidFloPath(node, dicFloName)
                    let realFlo, realName  = 
                        if(node.Name.Contains('.')) 
                        then node.Name.Split('.').[0], node.Name
                        else dicFloName.[node.PageNum], node.Name

                    let seg = Seg(realName, mySys, node.NodeCausal,  realFlo)
                    seg.Update(node.Key, node.Id.Value, node.Alias, node.CntTX, node.CntRX )
                    dicSeg.TryAdd(node.Key, seg) |> ignore
                    
                    Check.SameNode(seg, node, dicSameSeg)   )
                 
              
                //flow 리스트 만들기
                doc.Nodes 
                |> Seq.filter(fun node -> node.NodeCausal = DUMMY|>not)
                |> Seq.iter(fun node -> 
                                let name  = dicFloName.[node.PageNum]
                                let flow  = Flo(name, node.PageNum, mySys)
                               
                                mySys.Flos.TryAdd(node.PageNum, flow)|>ignore
                                mySys.Flos.[node.PageNum].AddSegNoEdge(dicSeg.[node.Key]))
                                
                //Dummy child 처리
                doc.Parents
                |> Seq.filter(fun group -> group.Key.NodeCausal = DUMMY)
                |> Seq.map(fun group -> group.Key, group.Value)
                |> Seq.iter(fun (parent, children) -> 
                    let pSeg = dicSeg.[parent.Key]
                    children 
                    |> Seq.iter(fun child ->  
                                let cSeg = dicSeg.[child.Key]
                                pSeg.AddSegNoEdge(cSeg)
                                mySys.Flos.[parent.PageNum].RemoveSegNoEdge(cSeg) 
                               // child.ExistChildEdge <- true
                                )
                )
            
            
                //edge 리스트 만들기 (pptEdge를 변환하여 dicEdge에 등록)
                doc.Edges 
                |> Seq.iter(fun edge -> 
                                let sSeg = dicSeg.[edge.StartNode.Key]
                                let eSeg = dicSeg.[edge.EndNode.Key]
                                if(sSeg.NodeCausal = DUMMY || eSeg.NodeCausal = DUMMY)
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
                |> Seq.filter(fun group -> group.Key.NodeCausal = DUMMY |>not)
                |> Seq.map(fun group -> group.Key, group.Value)
                |> Seq.iter(fun (parent, children) ->
                            let pSeg = dicSeg.[parent.Key]
                            children 
                            |> Seq.filter(fun child -> child.ExistChildEdge|>not) //엣지 할당 못받은 자식만
                            |> Seq.filter(fun child -> child.NodeCausal = DUMMY|>not) 
                            |> Seq.iter(fun child -> 
                                                //행위 부모 할당후 
                                                pSeg.AddSegNoEdge(dicSeg.[child.Key])
                                                //Flo 상에서 삭제
                                                mySys.Flos.[parent.PageNum].RemoveSegNoEdge(dicSeg.[child.Key]) 
                                                mySys.Flos.[parent.PageNum].AddSegDrawSub(pSeg) 
                                                    )
                )

                //시스템에 인과모델 등록
                dicEdge
                |> Seq.iter(fun edge -> model.AddEdge(edge.Key, edge.Value))
            
                //Call 위치정보 업데이트 (마지막 페이지만 정보 반영)
            
                doc.Nodes 
                |> Seq.filter(fun node -> node.PageNum = doc.VisibleLast().PageNum)
                |> Seq.filter(fun node -> node.Name = ""|>not)
                |> Seq.filter(fun node -> node.NodeCausal.IsLocation)
                |> Seq.iter(fun node -> mySys.LocationSet.TryAdd(dicSeg.[node.Key].ToLayOutPath(), node.Rectangle) |> ignore)
            
                Event.MSGInfo($"전체 장표   count [{doc.Pages.Count()}]")
                Event.MSGInfo($"전체 도형   count [{doc.Nodes.Count()}]")
                Event.MSGInfo($"전체 연결   count [{doc.Edges.Count()}]")
                Event.MSGInfo($"전체 부모   count [{doc.Parents.Keys.Count}]")
                model

            
            with ex ->  Event.MSGError  $"{ex.Message}"
                        model
                        
           
                    

    let FromPPTX(path:string) =
        let ppt = ImportPowerPoint(path)
        Event.DoWork(20);
        let model = ppt.GetDsModel()
        Event.DoWork(50);
        model
        


