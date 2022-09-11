// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open System.Collections.Concurrent
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Drawing
open DocumentFormat.OpenXml
open System.IO
open System
open UtilPPT
open UtilMS
open Engine.Common.FS
open Engine.Base



[<AutoOpen>]
module PPTX =
       
    let Objkey(iPage, Id) = $"{iPage}page{Id}"
    let SysName(iPage) = sprintf "page%3d" iPage 

    ///전체 사용된 화살표 반환 (앞뒤연결 필수)
    let Connections(doc:PresentationDocument) = 
                    Office.SildesSkipHide(doc) 
                    |> Seq.map (fun slide ->  slide, slide.Slide.CommonSlideData.ShapeTree.Descendants<ConnectionShape>())
                    |> Seq.map(fun (slide, connects) ->
                        slide, connects |> Seq.map(fun conn -> 
                            let Id = conn.Descendants<NonVisualDrawingProperties>().First().Id;
                            let startNode = conn.Descendants<NonVisualConnectionShapeProperties>().First().Descendants<StartConnection>().FirstOrDefault() 
                            let endNode   = conn.Descendants<NonVisualConnectionShapeProperties>().First().Descendants<EndConnection>().FirstOrDefault() 

                            let connStartId =  if(startNode = null) then 0u else startNode.Id.Value
                            let connEndId =  if(endNode = null) then 0u else endNode.Id.Value

                            conn, Id, connStartId, connEndId))

    ///전체 사용된 도형간 그룹지정 정보
    let Groups(doc:PresentationDocument) = 
                    Office.SildesSkipHide(doc) 
                    |> Seq.filter (fun slide ->  slide.Slide.CommonSlideData.ShapeTree.Descendants<GroupShape>().Any())
                    |> Seq.map (fun slide ->  slide, slide.Slide.CommonSlideData.ShapeTree.Descendants<GroupShape>() |> Seq.toList )
                          
    let rec SubGroup(subG:GroupShape, dicUsedSub:ConcurrentHash<GroupShape>) = 
                            subG.Descendants<Presentation.GroupShape>() 
                            |> Seq.iter(fun childGroup -> 
                                dicUsedSub.TryAdd(childGroup) |>ignore
                                SubGroup(childGroup, dicUsedSub) )

    let IsDummyGroup(subG:GroupShape) = 
                    let haveDummy = 
                        subG.Descendants<Presentation.Shape>() 
                        |> Seq.filter(fun shape -> shape.CheckEllipse())
                        |> Seq.filter(fun shape -> shape.IsDashShape())
                        |> Seq.filter(fun shape -> shape.InnerText.Length = 0)
                        |> Seq.length > 0
                    let haveReal = 
                        subG.Descendants<Presentation.Shape>() 
                        |> Seq.filter(fun shape -> shape.CheckRectangle())
                        |> Seq.filter(fun shape -> shape.IsDashShape()|>not)
                        |> Seq.filter(fun shape -> shape.InnerText.Length > 0)
                        |> Seq.length > 0
                    haveDummy && haveReal|>not
          
    let GetCausal(conn:ConnectionShape, iPage, startName, endName) =  
                    let shapeProperties = conn.Descendants<ShapeProperties>().FirstOrDefault();
                    let outline = shapeProperties.Descendants<Outline>().FirstOrDefault();
                    let tempHead  = if(outline.Descendants<HeadEnd>().FirstOrDefault() = null) 
                                    then LineEndValues.None
                                    else outline.Descendants<HeadEnd>().FirstOrDefault().Type.Value
                    let tempTail  = if(outline.Descendants<TailEnd>().FirstOrDefault() = null) 
                                    then LineEndValues.None
                                    else outline.Descendants<TailEnd>().FirstOrDefault().Type.Value

                    let isChangeHead = (tempHead = LineEndValues.None|>not) && (tempTail = LineEndValues.None)

                    let headShape = if(isChangeHead) then tempTail else tempHead          
                    let tailShape = if(isChangeHead) then tempHead else tempTail          

                    let existHead = headShape = LineEndValues.None|> not
                    let existTail = tailShape = LineEndValues.None|> not

                    let headArrow = headShape = LineEndValues.Triangle || headShape = LineEndValues.Arrow || headShape = LineEndValues.Stealth
                    let tailArrow = tailShape = LineEndValues.Triangle || tailShape = LineEndValues.Arrow || tailShape = LineEndValues.Stealth

                    let dashLine = Office.IsDashLine(conn)
                          
                    let single = outline.CompoundLineType = null || outline.CompoundLineType.Value = CompoundLineValues.Single;
                    let edgeProperties = conn.Descendants<NonVisualConnectionShapeProperties>().FirstOrDefault();
                    let connStart = edgeProperties.Descendants<StartConnection>().FirstOrDefault();
                    let connEnd = edgeProperties.Descendants<EndConnection>().FirstOrDefault();


                    //연결오류 찾아서 예외처리
                    if (connStart = null && connEnd = null) then  conn.ErrorConnect(4, startName, endName, iPage)
                    if (connStart = null) then  conn.ErrorConnect(5, startName, endName, iPage)
                    if (connEnd = null) then  conn.ErrorConnect(6, startName, endName, iPage)
                    if (not existHead && not existTail) then  conn.ErrorConnect(7, startName, endName, iPage)
                    if (existHead && existTail) 
                    then
                        if(not dashLine)
                        then 
                            if(headArrow && tailArrow) then  conn.ErrorConnect(8, startName, endName, iPage)           
                            if((headArrow || tailArrow)|>not)   then  conn.ErrorConnect(9, startName, endName, iPage)  
                            if(not headArrow && not tailArrow)  then  conn.ErrorConnect(10, startName, endName, iPage) 

                    //인과 타입과 <START, END> 역전여부
                    match existHead, existTail, dashLine with
                    |true, true, true  ->    EdgeCausal.Interlock, false
                    |true, true, false  ->  if(not headArrow &&  tailArrow) then EdgeCausal.SReset, false
                                            else EdgeCausal.SReset, true //반대로 뒤집기 필요
                    // dashLine 점선라인, single 한줄라인
                    |_->  match single, tailArrow, dashLine with
                            | true, true, false ->  EdgeCausal.SEdge, isChangeHead
                            | false,true, false ->  EdgeCausal.SPush, isChangeHead
                            | true, false,false ->  EdgeCausal.SSTATE, isChangeHead
                            | true, true, true  ->  EdgeCausal.REdge, isChangeHead
                            | false,true, true  ->  EdgeCausal.RPush, isChangeHead
                            | true, false,true  ->  EdgeCausal.RSTATE, isChangeHead
                            | _ -> conn.ErrorConnect(3, startName, endName, iPage)

    let rec ValidGroup(subG:Presentation.GroupShape, shapeIds:ConcurrentHash<uint32>) =    
        subG.Descendants<Presentation.Shape>() 
        |> Seq.filter(fun shape -> shape.CheckRectangle() || shape.CheckEllipse())
        |> Seq.iter(fun shape -> shapeIds.TryAdd(shape.GetId().Value)|>ignore )
        subG.Descendants<Presentation.GroupShape>() 
        |> Seq.iter(fun childGroup -> 
            ValidGroup(childGroup, shapeIds) |> ignore)
        true

    type pptPage(slidePart:SlidePart, iPage:int , bShow:bool) =
        member x.PageNum = iPage
        member x.SlidePart = slidePart
        member x.IsUsing = bShow
        member x.Title = slidePart.PageTitle()

    type pptNode(shape:Presentation.Shape,iPage:int, dashOutline:bool , sildeSize)  =
        let mutable txCnt = 1
        let mutable rxCnt = 1
        let mutable name = ""
        let mutable bDuumy = false
        do 
            if(shape.InnerText.Contains("[") && shape.InnerText.EndsWith("]"))
            then 
                let matches = System.Text.RegularExpressions.Regex.Matches(shape.InnerText, "(?<=\[).*?(?=\])")
                let data = matches.[matches.Count-1]

                if(data.Value.Split(',').Count() > 1)
                then 
                    txCnt <- data.Value.Split(',').[0] |> Convert.ToInt32
                    rxCnt <- data.Value.Split(',').[1] |> Convert.ToInt32
                    name <- shape.InnerText.Replace($"[{data.Value}]","")
                else 
                    shape.ErrorName(22, iPage)
            else 
                name <- shape.InnerText

            bDuumy <- shape.CheckEllipse() && dashOutline
            
        member x.PageNum = iPage
        member x.Shape = shape
        member x.DashOutline = dashOutline
        member x.IsDummy = bDuumy
        member val NodeCausal = 
                            if(shape.CheckRectangle()) then if(dashOutline) then EX else  MY
                            else 
                            if(shape.CheckEllipse()) 
                            then 
                                if((txCnt = 0 && rxCnt = 0) || txCnt < 0 || rxCnt < 0)
                                then shape.ErrorName(2, iPage)
                                else TR
                            else  shape.ErrorName(1, iPage)

        member val Id =  shape.GetId()
        member val Key =  Objkey(iPage, shape.GetId())
        member val Name =   name
        member val CntTX =  txCnt
        member val CntRX =  rxCnt
        member val Alias :string  option = None with get, set
        member val ExistChildEdge :bool = false with get, set
        member val Rectangle :System.Drawing.Rectangle =   shape.GetPosition(sildeSize)

    and 
        pptEdge(conn:Presentation.ConnectionShape,  iEdge:UInt32Value, iPage:int ,startId:uint32, endId:uint32, nodes:ConcurrentDictionary<string, pptNode>) =
        let mutable reverse = false
        let mutable causal:EdgeCausal = EdgeCausal.SEdge
        let startKey = Objkey(iPage, startId)
        let endKey   = Objkey(iPage, endId) 
        let startNode = nodes.[startKey]
        let endNode   = nodes.[endKey]
        do
            GetCausal(conn, iPage, nodes.[startKey].Name, nodes.[endKey].Name) 
            |> fun(c, r) -> causal <- c;reverse <- r
     
        member x.PageNum = iPage
        member x.ConnectionShape = conn
        member x.Id = iEdge
        member x.StartNode:pptNode = if(reverse) then endNode else startNode
        member x.EndNode:pptNode =   if(reverse) then startNode else endNode
        member x.ParentId = 0 //reserve

    
        member val Name =  conn.EdgeName()
        member val Key =  Objkey(iPage, iEdge)
        member val Text = if(reverse)
                            then $"{endNode.Name}{causal.ToText()}{startNode.Name}";
                            else $"{startNode.Name}{causal.ToText()}{endNode.Name}";
        member val Causal:EdgeCausal = causal
    
    and 
        pptGroup(iPage:int, ids:uint32 seq, nodes:ConcurrentDictionary<string, pptNode>) =
        let mutable parent:pptNode option = None
        let mutable dummy :pptNode option = None
        let childSet =  ConcurrentHash<pptNode>()

        do
            let parents = 
                ids 
                |> Seq.map (fun id -> nodes.[ Objkey(iPage, id) ])
                |> Seq.filter (fun node -> node.NodeCausal = MY)
            if(parents.Count() > 1) 
            then  Office.ErrorPPT(Group, 23, $"부모수:{parents.Count()}", iPage)
            let dummys = 
                ids 
                |> Seq.map (fun id -> nodes.[ Objkey(iPage, id) ])
                |> Seq.filter (fun node -> node.IsDummy)
            if(dummys.Count() > 1) 
            then  Office.ErrorPPT(Group, 24, $"부모수:{dummys.Count()}", iPage)
            if(parents.Count() = 0 && dummys.Count() = 0 ) 
            then  Office.ErrorPPT(Group, 25, $"도형 타입확인", iPage)

            parent <-
                if(parents.Any()|>not) then None
                else Some(parents |> Seq.head)

            
            dummy <-
                if(dummys.Any()|>not) then None
                else Some(dummys |> Seq.head)

            let children = 
                ids 
                |> Seq.map (fun id -> nodes.[Objkey(iPage, id) ])
                |> Seq.filter (fun node ->node.NodeCausal = MY |> not)
                |> Seq.filter (fun node ->(node.IsDummy|>not ||  parent.IsSome))

            if(children.Any() |> not) 
            then  Office.ErrorPPT(Group, 12, $"자식수:0", iPage)

            let nameList = children 
                           |> Seq.map(fun f->f.Name)
                           |> Seq.distinct
            nameList |> Seq.iter(fun name -> 
                           let sameNodes = children |> Seq.filter(fun f -> f.Name = name)
                           if(sameNodes.Count() > 1)
                           then 
                               let mutable cnt = 0
                               sameNodes 
                               |> Seq.iter(fun node -> 
                                   cnt <- cnt+1
                                   node.Alias <-Some(sprintf "%s_Copy%d" node.Name cnt))
                           )

            children |> Seq.iter(fun child -> childSet.TryAdd(child)|>ignore)
        
        member x.RealKey = sprintf "%d;%s"  iPage (parent.Value.Name)
        member x.PageNum = iPage
       
        member x.Parent:pptNode option = parent
        member x.DummyParent:pptNode option = dummy
        member x.Children =  childSet.Values 
        
    type pptDoc(path:string) =
        let doc = Office.Open(path)
        let pages =  ConcurrentDictionary<SlidePart, pptPage>()
        let masterPages =  ConcurrentDictionary<int, DocumentFormat.OpenXml.Presentation.SlideMaster>()
        let nodes =  ConcurrentDictionary<string, pptNode>()
        let parents = ConcurrentDictionary<pptNode, seq<pptNode>>()
        let edges =  ConcurrentHash<pptEdge>()
        do
            let sildesAll = Office.SildesAll(doc) 
            let shapes = Office.PageShapes(doc) 
            let connections = Connections(doc) 
            let groups = Groups(doc) 
            let allGroups =  groups |> Seq.collect(fun (slide, groupSet) -> groupSet) 
            let sildeSize = Office.SildeSize(doc)
            let sildeMasters = Office.SildesMasterAll(doc)

            try
                
                sildeMasters
                |> Seq.iter (fun slideMaster -> 
                    masterPages.TryAdd(masterPages.Count+1, slideMaster) |>ignore )
              
                sildesAll
                |> Seq.iter (fun (slidePart, show, page) -> 
                    pages.TryAdd(slidePart, pptPage(slidePart, page, show)) |>ignore )
              
                shapes 
                |> Seq.iter (fun (shape, page, geometry, isDash) ->
                            let node = pptNode(shape, page,  isDash,  sildeSize)
                            if(node.Name ="" && node.IsDummy|>not) then shape.ErrorName(13, page)
                            nodes.TryAdd(node.Key, node)  |>ignore )
                             
                connections
                |> Seq.iter (fun (slide, conns) -> 
                    conns 
                          |> Seq.iter (fun (conn, Id, startId, endId) ->
                            let iPage = pages.[slide].PageNum
                            if(startId = 0u && endId = 0u) then  conn.ErrorConnect(4, "","", iPage)
                            if(nodes.ContainsKey(Objkey(iPage, startId))|>not) then  conn.ErrorConnect(14, "",$"{(nodes.[Objkey(iPage, endId)]).Name}", iPage) 
                            if(nodes.ContainsKey(Objkey(iPage, endId))|>not)   then  conn.ErrorConnect(14, $"{(nodes.[Objkey(iPage, startId)]).Name}", "", iPage) 
                            if(startId = 0u) then  conn.ErrorConnect(15, "",$"{(nodes.[Objkey(iPage, endId)]).Name}", iPage) 
                            if(endId = 0u) then    conn.ErrorConnect(16, $"{(nodes.[Objkey(iPage, startId)]).Name}", "", iPage) 
                            edges.TryAdd(pptEdge(conn, Id, iPage ,startId, endId,  nodes)) |>ignore 
                        ))
                
                let dicFakeSub = ConcurrentHash<Presentation.GroupShape>()
                allGroups |> Seq.iter (fun group -> SubGroup(group, dicFakeSub))

                let dicParentCheck = ConcurrentDictionary<string, int>()
                let makeRealGroup ( pptGroup:pptGroup ) =
                    if(pptGroup.Parent.IsNone)
                    then Office.ErrorPPT(Group, 18, "", pptGroup.PageNum)
                    else 
                        let parent = pptGroup.Parent.Value;
                        if(dicParentCheck.TryAdd(pptGroup.RealKey, pptGroup.PageNum))
                        then 
                            parents.TryAdd(parent, pptGroup.Children)|>ignore
                        else 
                            Office.ErrorPPT(Group, 17, $"{dicParentCheck.[pptGroup.RealKey]}-{parent.Name}", pptGroup.PageNum) 
                
                let makeDummyGroup ( pptGroup:pptGroup ) =
                        if(pptGroup.DummyParent.IsSome)
                        then 
                            if((pptGroup.Children |> Seq.distinctBy(fun node -> node.Name)).Count() = pptGroup.Children.Count )
                            then
                                parents.TryAdd(pptGroup.DummyParent.Value, pptGroup.Children)|>ignore
                            else
                                let errorChild = pptGroup.Children |> Seq.map(fun node -> node.Name) |> String.concat ", "
                                Office.ErrorPPT(Group, 19, $"{errorChild}", pptGroup.PageNum) 
                            
                          
                
                groups
                |> Seq.iter (fun (slide, groupSet) -> 
                    groupSet
                    |> Seq.iter (fun group ->
                        let shapeIds = ConcurrentHash<uint32>()
                        ValidGroup(group, shapeIds) |> ignore 

                        if(IsDummyGroup(group))
                        then makeDummyGroup(pptGroup(pages.[slide].PageNum, shapeIds.Values, nodes))
                        else 
                            if(dicFakeSub.ContainsKey(group)|>not)
                            then makeRealGroup (pptGroup(pages.[slide].PageNum, shapeIds.Values, nodes))
                    )
                )

             
                doc.Close()
              
            with ex -> doc.Close()
                       MSGError  $"{ex.Message}"
                

        member x.GetPage(pageNum:int) = pages.Values |> Seq.filter(fun p -> p.PageNum = pageNum) |> Seq.head
        member x.VisibleLast() = pages.Values.OrderByDescending(fun p -> p.PageNum) |> Seq.filter(fun p -> p.IsUsing) |> Seq.head
        member val Pages = pages.Values.OrderBy(fun p -> p.PageNum)

        member val Nodes = nodes.Values.OrderBy(fun p -> p.PageNum)
        member val DicNodes = nodes
        member val Edges = edges.Values.OrderBy(fun p -> p.PageNum)
        member val Parents = parents

        member val Name =  Path.GetFileNameWithoutExtension(path)
        member val FullPath =  path

                                           

            