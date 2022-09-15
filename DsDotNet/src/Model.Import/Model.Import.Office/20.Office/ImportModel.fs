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
open Engine.Base
open Engine.Parser

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

        let updateAlias(node:pptNode, flow:Flo) = 
            if(node.Alias.IsSome) 
            then 
                let name = Util.GetValidName(node.Name) 
                let alias = $"\"{node.Alias.Value}\"";  //별칭은 무조건 "" 감싸기

                if(flow.AliasSet.Keys.Contains(name))  
                 then flow.AliasSet.[name].Add( alias)|> ignore
                 else let set = HashSet<string>()
                      set.Add(alias)|> ignore
                      flow.AliasSet.TryAdd(name, set ) |> ignore

        let dicSameCheck = ConcurrentDictionary<string, MEdge>()
        let convertEdge(edge:pptEdge) = 
            let sSeg = dicSeg.[edge.StartNode.Key]
            let eSeg = dicSeg.[edge.EndNode.Key]
            updateAlias(edge.StartNode, mySys.Flows.[edge.PageNum]) 
            updateAlias(edge.EndNode, mySys.Flows.[edge.PageNum]) 
            getParent(edge) |> Seq.iter(fun (parentNode, parentSeg) ->
                        
                           mySys.Flows.[edge.PageNum].RemoveSegNoEdge(sSeg) 
                           mySys.Flows.[edge.PageNum].RemoveSegNoEdge(eSeg) 
                           let mEdge = MEdge(sSeg, eSeg, edge.Causal)

                           if(mEdge.Causal = Interlock)
                           then mySys.Flows.[edge.PageNum].AddInterlock(mEdge)

                           match parentNode with
                           |Some(v) -> if(v.PageNum = edge.PageNum) 
                                       then mySys.Flows.[edge.PageNum].AddSegDrawSub(parentSeg) 
                           |None -> mySys.Flows.[edge.PageNum].AddEdge(mEdge)

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
              
                                    dicFloName.TryAdd(page.PageNum, Util.GetValidName(flowName))|>ignore)

                //segment 리스트 만들기
                doc.Nodes 
                |> Seq.iter(fun node -> 
                    Check.ValidFloPath(node, dicFloName)
                    let realFlo, realName  = 
                        if(node.Name.Contains('.')) 
                        then node.Name.Split('.').[0], node.Name
                        else dicFloName.[node.PageNum], node.Name
                    let btn = node.IsEmgBtn ||node.IsStartBtn ||node.IsAutoBtn 
                    let seg = Seg(realName, mySys, Editor.User, Bound.Normal, node.NodeCausal, realFlo, node.IsDummy, btn)
                    seg.Update(node.Key, node.Id.Value, node.Alias, node.CntTX, node.CntRX )
                    dicSeg.TryAdd(node.Key, seg) |> ignore
                    
                    Check.SameNode(seg, node, dicSameSeg)   )
              
                //Flow 리스트 만들기
                doc.Nodes 
                |> Seq.filter(fun node -> node.IsDummy|>not)
                |> Seq.iter(fun node -> 
                                let name  = dicFloName.[node.PageNum]
                                let flow  = Flo(name, node.PageNum, mySys)
                               
                                mySys.Flows.TryAdd(node.PageNum, flow)|>ignore
                                mySys.Flows.[node.PageNum].AddSegNoEdge(dicSeg.[node.Key])
                                )

                //Safety & EMG & Start & Auto 리스트 만들기
                doc.Nodes 
                |> Seq.filter(fun node -> node.IsDummy|>not)
                |> Seq.iter(fun node -> 
                                let flowName =  mySys.Flows.[node.PageNum].Name
                                let dic = dicSeg.Values.Select(fun seg -> seg.FullName, seg) |> dict
                                //EMG
                                if(node.IsEmgBtn) 
                                then 
                                    if(mySys.EmgSet.ContainsKey(node.Name))
                                    then mySys.EmgSet.[node.Name].Add(mySys.Flows.[node.PageNum]) |>ignore
                                    else mySys.EmgSet.TryAdd(node.Name, [mySys.Flows.[node.PageNum]] |> List) |>ignore
                                //Start
                                if(node.IsStartBtn) 
                                then 
                                    if(mySys.StartSet.ContainsKey(node.Name))
                                    then mySys.StartSet.[node.Name].Add(mySys.Flows.[node.PageNum]) |>ignore
                                    else mySys.StartSet.TryAdd(node.Name, [mySys.Flows.[node.PageNum]] |> List) |>ignore
                                //Auto
                                if(node.IsAutoBtn) 
                                then 
                                    if(mySys.AutoSet.ContainsKey(node.Name))
                                    then mySys.AutoSet.[node.Name].Add(mySys.Flows.[node.PageNum]) |>ignore
                                    else mySys.AutoSet.TryAdd(node.Name, [mySys.Flows.[node.PageNum]] |> List) |>ignore
                                //Safety
                                let safeSeg = 
                                    node.Safeties   //세이프티 입력 미등록 이름오류 체크
                                    |> Seq.map(fun safe ->  sprintf "%s.%s.%s" mySys.Name flowName safe)
                                    |> Seq.iter(fun safeFullName -> if(dic.ContainsKey safeFullName|>not) 
                                                                    then Office.ErrorName(node.Shape, 28, node.PageNum))


                                    node.Safeties   
                                    |> Seq.map(fun safe ->  sprintf "%s.%s.%s" mySys.Name flowName safe)
                                    |> Seq.map(fun safeFullName ->  dic.[safeFullName])

                                if(safeSeg.Any())
                                    then mySys.Flows.[node.PageNum].AddSafety(dicSeg.[node.Key], safeSeg)
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
                                pSeg.AddSegNoEdge(cSeg)
                                mySys.Flows.[parent.PageNum].RemoveSegNoEdge(cSeg) 
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
                                                updateAlias(child, mySys.Flows.[parent.PageNum]) 
                                                
                                                //행위 부모 할당후 
                                                pSeg.AddSegNoEdge(dicSeg.[child.Key])
                                                //Flo 상에서 삭제
                                                mySys.Flows.[parent.PageNum].RemoveSegNoEdge(dicSeg.[child.Key]) 
                                                mySys.Flows.[parent.PageNum].AddSegDrawSub(pSeg) 
                                                    )
                )

                //시스템에 인과모델 등록
                dicEdge
                |> Seq.iter(fun edge -> model.AddEdge(edge.Key, edge.Value))
            
                //Call 위치정보 업데이트 (마지막 페이지만 정보 반영)
            
                doc.Nodes 
                |> Seq.filter(fun node -> node.PageNum = doc.VisibleLast().PageNum)
                |> Seq.filter(fun node -> node.Name = ""|>not)
                |> Seq.filter(fun node -> node.NodeCausal = TX || node.NodeCausal = TR || node.NodeCausal = RX || node.NodeCausal = EX )
                |> Seq.iter(fun node -> mySys.LocationSet.TryAdd(dicSeg.[node.Key].ToLayOutPath(), node.Rectangle) |> ignore)
            
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
        let model = ppt.GetDsModel()
        DoWork(50);
        model
        


