// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System
open System.IO
open System.Linq
open System.Diagnostics
open System.Collections.Concurrent
open System.Collections.Generic
open Engine.Core

[<AutoOpen>]
module Object =

    // 행위 Bound 정의
    type Bound =
        | ThisFlow         //나의 System 의 이 MFlow        내부 행위정의
        | OtherFlow        //나의 System 의 다른 MFlow     에서 행위 가져옴
        | ExSeg            //외부 System 의 에서 행위(real) 가져옴
        | ExBtn            //외부 System 의 에서 버튼(call) 가져옴

    and
        /// 사용자가 모델링을 통해서 만든 segment (SegEditor = User)
        [<DebuggerDisplay("{FullName}")>]
        MSeg(name:string, baseSystem:MSys, bound:Bound, nodeType:NodeType, ownerMFlow:string, bDummy:bool) as this =
            inherit Segment(name, ChildFlow(name), RootFlow(ownerMFlow))
            let mEdges = (this :> Segment).ChildFlow.Edges
            let mChildFlow = (this :> Segment).ChildFlow

            new (name, baseSystem, nodeType) = MSeg (name, baseSystem,  ThisFlow, nodeType, "", false)
       
            member x.NodeType = nodeType 
            member x.BaseSys = baseSystem
            member x.Bound = bound
            member x.RemoveMEdge(edge:MEdge) =  mChildFlow.RemoveEdge(edge) |> ignore
            member x.AddMEdge(edge:MEdge) =
                    mChildFlow.AddEdge(edge) |> ignore
                    let src = edge.Source
                    let tgt = edge.Target
                    if(this = src) then failwith $"parent [{this.SegName}] = SourceVertex [{src.SegName}]"
                    if(this = tgt) then failwith $"parent [{this.SegName}] = TargetVertex [{tgt.SegName}]"

            member val Alias :string  option = None with get, set
            member val ShapeID = 0u with get, set
            member val CountTX = 0 with get, set
            member val CountRX = 0 with get, set

            member x.OwnerMFlow = ownerMFlow
            member x.ToCallText() = let call = sprintf "%s_%s"  (ownerMFlow.TrimStart('\"').TrimEnd('\"')) name
                                    NameUtil.GetValidName(call)

            member x.ToTextInMFlow() =  match nodeType with
                                         |EX -> if(this.Alias.IsSome) 
                                                then this.Alias.Value 
                                                else sprintf "EX.%s.EX" (x.ToCallText())
                                         |_  -> if(Bound.ThisFlow = bound) 
                                                then x.SegName
                                                else x.MFlowNSeg

            ///금칙 문자 및 선두숫자가 있으면 "" 로 이름 앞뒤에 배치한다.
            ///Alias 는 무조건 "" 로 이름 앞뒤에 배치
            member x.SegName  = sprintf "%s" (if(this.Alias.IsSome) then this.Alias.Value else x.ValidName)
            member x.MFlowNSeg= sprintf "%s.%s"  ownerMFlow x.ValidName
            member x.FullName = sprintf "%s.%s.%s" baseSystem.Name  ownerMFlow x.ValidName  
            member x.PathName = sprintf "%s(%s)" x.FullName (if(x.Parent.IsSome) then x.Parent.Value.Name else "Root")

            member x.Update(nodeKey, nodeIdValue, nodeAlias, nodeCntTX, nodeCntRX) = 
                        this.Key <- nodeKey
                        this.ShapeID <- nodeIdValue
                        this.Alias <- nodeAlias
                        this.CountTX <- nodeCntTX
                        this.CountRX <- nodeCntRX

            member x.MaxCnt  = 
                        if(this.CountTX >= this.CountRX) 
                        then this.CountTX else this.CountRX

            member x.PrintfTRX (curr:int, ?bAddress:bool) =
                let nameTR =  if(bAddress.IsSome && bAddress.Value) then "IO" else "TR"
                let nameTX =  if(bAddress.IsSome && bAddress.Value) then "O" else "TX"
                let nameRX =  if(bAddress.IsSome && bAddress.Value) then "I" else "RX"
                
                if(this.CountTX >= curr && this.CountRX >= curr) 
                then  TR, if(this.CountTX = 1) then nameTR else sprintf "%s%d" nameTR curr
                else if(this.CountRX < curr) 
                then  TX, if(this.CountTX = 1) then nameTX else sprintf "%s%d" nameTX curr
                else  RX, if(this.CountRX = 1) then nameRX else sprintf "%s%d" nameRX curr
  
            member x.MEdges = mEdges |> Seq.sortBy(fun edge ->edge.ToText()) |> Seq.cast<MEdge>
            member x.ChildSegs =
                mChildFlow.Nodes
                |> Seq.cast<MSeg>
                |> Seq.distinct

            //재귀적으로 자식 Seg를 가져옴 (다른시스템은 Root까지)
            member x.ChildSegsSubAll =
                   x.ChildSegs
                   |> Seq.collect(fun e -> e.ChildSegsSubAll)
                   |> Seq.append x.ChildSegs
                   |> Seq.append x.NoEdgeSegs
        
            member x.IsDummy = bDummy
            member x.IsChildExist = x.ChildSegsSubAll.Any()
            member x.IsChildEmpty = x.IsChildExist|>not
            member x.IsRoot =  x.Parent.IsSome && x.Parent.Value.Bound = ThisFlow
            member x.UIKey:string =  $"{x.Name};{x.Key}"
            member val Key : string = "" with get, set
            member val Parent : MSeg option = None with get, set
            member val S : string option = None    with get, set
            member val R : string option = None    with get, set
            member val E : string option = None    with get, set

            member x.NoEdgeBaseSegs      = mChildFlow.Singles  |> Seq.cast<MSeg> |> Seq.sortBy(fun seg -> seg.Name)
            member x.AddSegNoEdge(seg)   = mChildFlow.AddSingleNode(seg)         |> ignore
            member x.RemoveSegNoEdge(seg)= mChildFlow.RemoveSingleNode(seg)      |> ignore

            member x.TextStart = if(x.S.IsSome) then x.S.Value else ""
            member x.TextReset = if(x.R.IsSome) then x.R.Value else ""
            member x.TextEnd   = if(x.E.IsSome) then x.E.Value else ""

            member x.AddChildNSetParent(edge:MEdge) =  
                edge.Source.Parent <- Some(x)  
                edge.Target.Parent <- Some(x)  
           
                x.AddMEdge(edge)

            member x.NoEdgeSegs = x.NoEdgeBaseSegs |> Seq.cast<MSeg> |> Seq.sortBy(fun s -> s.SegName)

     

    and
        /// Modeled Edge : 사용자가 작성한 모델 상의 segment 간의 연결 edge (Wire)
        [<DebuggerDisplay("{Source.FullName}{Causal.ToText()}{Target.FullName}")>]
        MEdge(src:MSeg, tgt:MSeg, causal:EdgeCausal) =
            inherit DsEdge(src, tgt, causal)
            member x.Source = src
            member x.Target = tgt
            member x.IsSameSys = src.BaseSys = tgt.BaseSys
            member x.SrcSystem = src.BaseSys
            member x.TgtSystem = tgt.BaseSys
            member x.Nodes = [src;tgt]
            member x.Causal = causal
            
            member x.ToCheckText() =    match causal with
                                        |SEdge |SPush |  SReset-> "Start"
                                        |REdge |RPush |  Interlock-> "Reset"

            member x.ToText() = $"{src.SegName}  {causal.ToText()}  {tgt.SegName}"
            member x.ToCheckText(parentName:string) = 
                            let srcName = if(src.Alias.IsSome) then src.Alias.Value else src.ToCallText()
                            let tgtName = if(tgt.Alias.IsSome) then tgt.Alias.Value else tgt.ToCallText()
                            $"[{parentName}]{srcName}  {x.ToCheckText()}  {tgtName}"

            member x.GetSegs() = [src;tgt]
    
    
    and
        /// MFlow : 페이지별 구성
        [<DebuggerDisplay("{Name}")>]
        MFlow(name:string, index:int)  =
            inherit RootFlow(name)
            let drawSubs  = ConcurrentHash<MSeg>()
            let dummySeg  = ConcurrentHash<MSeg>()
            let safeties  = ConcurrentDictionary<MSeg, MSeg seq>()
            let edges  = ConcurrentHash<MEdge>()
            let interlocks  = ConcurrentDictionary<string, MEdge>()
            let setIL  = ConcurrentHash<HashSet<MSeg>>()
            let aliasSet  = ConcurrentDictionary<string, HashSet<string>>()
            let noEdgeBaseSegs  = ConcurrentDictionary<MSeg, MSeg>()

            let rec getLink(start:MSeg, find:HashSet<MSeg>, full:HashSet<MSeg>) =
                let update (edge:MEdge) =
                    find.Add(edge.Source) |>ignore   
                    find.Add(edge.Target) |>ignore
                    full.Remove(edge.Source) |> ignore
                    full.Remove(edge.Target) |> ignore

                interlocks.Values.GetSrcSame(start) 
                |> Seq.iter(fun edge -> 
                                if(find.Contains(edge.Source)|>not || find.Contains(edge.Target)|>not)
                                then update (edge);getLink (edge.Target, find, full))
                                        
                interlocks.Values.GetTgtSame(start) 
                |> Seq.iter(fun edge -> 
                                if(find.Contains(edge.Source)|>not || find.Contains(edge.Target)|>not)
                                then update (edge);getLink (edge.Source, find, full))
            member x.NoEdgeBaseSegs = noEdgeBaseSegs.Values |> Seq.sortBy(fun seg -> seg.Name)
            member x.AddSegNoEdge(seg) = noEdgeBaseSegs.TryAdd(seg, seg) |> ignore 
            member x.RemoveSegNoEdge(seg) = noEdgeBaseSegs.TryRemove(seg) |> ignore 

            member x.Page = index

            member x.Edges = edges.Values |> Seq.sortBy(fun edge -> edge.Source.Name)
            member x.AddEdge(edge) = edges.TryAdd(edge) |> ignore 
            member x.AddSafety(seg, segSafetys:MSeg seq) = safeties.TryAdd(seg, segSafetys) |> ignore 
            member x.Safeties = safeties
            member x.AliasSet   = aliasSet

            member x.Interlockedges = 
                        let FullNodesIL = interlocks.Values
                                            |> Seq.collect(fun seg -> [seg.Source;seg.Target])
                                            |> Seq.filter(fun seg -> seg.Alias.IsNone)
                                            |> HashSet
                        interlocks.Values
                                        |> Seq.collect(fun seg -> [seg.Source;seg.Target])
                                        |> Seq.filter(fun seg -> FullNodesIL.Contains(seg))
                                        |> Seq.iter(fun seg -> 
                                                    let findSet = HashSet<MSeg>()
                                                    getLink (seg, findSet, FullNodesIL)
                                                    if(findSet.Any()) then setIL.TryAdd(findSet) |> ignore
                                            )
                        setIL.Values

            member x.AddInterlock(edge:MEdge) = 
                    if(interlocks.TryAdd(edge.ToText() , edge) )
                    then ()
                    else ()

            member x.DrawSubs = drawSubs.Values |> Seq.sortBy(fun seg -> seg.Name)
            member x.AddSegDrawSub(seg) = drawSubs.TryAdd(seg) |> ignore 

            member x.DummySeg = dummySeg.Values 
            member x.AddDummySeg(seg) = dummySeg.TryAdd(seg) |> ignore 
            member x.NoEdgeSegs =  x.NoEdgeBaseSegs  |> Seq.cast<MSeg>

            //member x.UsedSegs =
            //    x.AllSegments |> Seq.cast<MSeg>

                //let rootUsedSegs  = 
                //    edges 
                //    |> Seq.collect(fun edge -> edge.Key.GetSegs())
                //    |> Seq.append x.NoEdgeSegs

                //rootUsedSegs 
                //|> Seq.collect (fun node -> node.ChildSegsSubAll)
                //|> Seq.append rootUsedSegs
                //|> Seq.distinctBy(fun seg -> seg.PathName)
                    
            member x.UsedSegs   = x.UsedSegments   |> Seq.cast<MSeg>
            member x.CallSegs() = x.UsedSegs
                                        |> Seq.filter(fun seg -> seg.NodeType.IsCall)
                                        |> Seq.filter(fun seg -> seg.Bound = ThisFlow)
                                        |> Seq.distinctBy(fun seg -> seg.FullName)

            member x.ExRealSegs() = x.UsedSegs
                                        |> Seq.filter(fun seg -> seg.NodeType.IsReal)
                                        |> Seq.filter(fun seg -> seg.Bound = ExSeg)
                                        |> Seq.distinctBy(fun seg -> seg.FullName)


            member x.CallNExRealSegs() =  x.CallSegs() |> Seq.append (x.ExRealSegs())
            member x.CallWithoutInterLock()  = 
                let dicInterLockName =  
                    x.Interlockedges 
                    |> Seq.collect(fun segs ->segs |> Seq.map(fun seg -> seg.FullName))
                x.CallSegs()
                |> Seq.filter(fun seg -> 
                    dicInterLockName.Contains(seg.FullName)|>not)
              
    and
        [<DebuggerDisplay("{Name}")>]
        /// System 내부 Seg의 내외부 Seg간 시작/리셋 연결 정보 구조
        MSys(name:string, active:bool) as this  =
            inherit DsSystem(name)

            let mutable sysSeg: System.Lazy<MSeg> = null
            let locationSet  = ConcurrentDictionary<string, System.Drawing.Rectangle>()
            let commandSet  = ConcurrentDictionary<string, string>()
            let observeSet  = ConcurrentDictionary<string, string>()
            let variableSet  = ConcurrentDictionary<string, DataType>()
            let addressSet  = ConcurrentDictionary<string, Tuple<string, string, string>>()
            //let noEdgesSegs = mFlows |> Seq.collect(fun f-> f.Value.NoEdgeSegs)
            let emgSet  = ConcurrentDictionary<string, List<MFlow>>()
            let startSet  = ConcurrentDictionary<string, List<MFlow>>()
            let resetSet  = ConcurrentDictionary<string, List<MFlow>>()
            let autoSet   = ConcurrentDictionary<string, List<MFlow>>()
            
            let updateBtn (btnType:BtnType, btnName, btnMFlow) = 
                let name = NameUtil.GetValidName(btnName)
                match btnType with
                |StartBTN ->    if(startSet.ContainsKey(name)) then startSet.[name].Add(btnMFlow) |>ignore else startSet.TryAdd(name, [btnMFlow] |> List) |>ignore
                |ResetBTN ->    if(resetSet.ContainsKey(name)) then resetSet.[name].Add(btnMFlow) |>ignore else resetSet.TryAdd(name, [btnMFlow] |> List) |>ignore
                |AutoBTN ->     if(autoSet.ContainsKey(name))  then autoSet.[name].Add(btnMFlow)  |>ignore else autoSet.TryAdd(name,  [btnMFlow] |> List) |>ignore
                |EmergencyBTN-> if(emgSet.ContainsKey(name))   then emgSet.[name].Add(btnMFlow)   |>ignore else emgSet.TryAdd(name,   [btnMFlow] |> List) |>ignore


            new (name:string)       = new MSys(name,  false)
            new (name, active:bool) = new MSys(name,  active)

            member x.Name = name
            member x.SysSeg =
                if isNull sysSeg then
                    sysSeg <- Lazy(fun () -> MSeg("(ENG)Main_"+name, x, NodeType.MY))
                sysSeg.Value

            member val Debug = false   with get, set
            member val Active = active with get, set
            member val SystemID = -1   with get, set

            //member x.AddFlowPage(flow:RootFlow, page:int) = 
            //    //test ahn
            //    //mFlows.TryAdd(page, flow :?> MFlow) |> ignore
            //    x.AddFlow(flow);

            //member x.GetFlow(page:int)      = mFlows.[page]
            //member x.MFlows      = mFlows
            member x.SingleNodes   = this.RootFlows() 
                                        |> Seq.collect(fun flow -> flow.Singles)
                                        |> Seq.cast<MSeg>
            member x.AllNodes  = this.RootFlows() 
                                        |> Seq.collect(fun flow -> flow.Nodes)
                                        |> Seq.cast<MSeg>
            member x.LocationSet   = locationSet
            member x.CommandSet   = commandSet
            member x.ObserveSet   = observeSet
            member x.VariableSet   = variableSet
            member x.AddressSet   = addressSet
            member x.EmgSet   = emgSet
            member x.StartSet   = startSet
            member x.ResetSet   = resetSet
            member x.AutoSet   = autoSet

            member x.TryAddStartBTN(name, mFlow) = updateBtn(StartBTN, name, mFlow)
            member x.TryAddResetBTN(name, mFlow) = updateBtn(ResetBTN, name, mFlow)
            member x.TryAddAutoBTN(name,  mFlow) = updateBtn(AutoBTN, name, mFlow)
            member x.TryAddEmergBTN(name, mFlow) = updateBtn(EmergencyBTN, name, mFlow)

            member x.GetBtnSet(btnType:BtnType) = 
                match btnType with
                |StartBTN ->     startSet
                |ResetBTN ->     resetSet
                |AutoBTN ->      autoSet
                |EmergencyBTN -> emgSet

            member x.AssignAddress(typeString, nameString, valueString) = 
                match BtnToType(typeString) with
                |StartBTN ->     startSet
                |ResetBTN ->     resetSet
                |AutoBTN ->      autoSet
                |EmergencyBTN -> emgSet

            member x.Segs() =   let segs = 
                                        x.SysSeg.ChildSegsSubAll 
                                        |> Seq.append (x.SingleNodes)

                                segs
                                |> Seq.collect(fun seg -> seg.ChildSegsSubAll) |> Seq.append segs
                                |> Seq.sortBy(fun seg -> seg.Name)
                                |> Seq.distinct
            

            member x.RootEdges()    = x.SysSeg.MEdges 

            member x.NotRootSegs() = 
                x.SysSeg.MEdges
                    |> Seq.collect(fun seg -> [seg.Source;seg.Target])
                    |> Seq.cast<MSeg>
                    |> Seq.append x.SingleNodes
                    |> Seq.collect(fun seg -> seg.ChildSegs)
                    |> Seq.distinct

            member x.RootSegs() =
                x.SysSeg.ChildSegs
                    |> Seq.append x.SingleNodes
                    |> Seq.distinct

            member x.RootMFlow()  = this.RootFlows() |> Seq.sortBy(fun flow -> flow.Name)
            member x.BtnSegs()    = this.AllNodes
                                        |> Seq.filter(fun seg -> seg.Bound = ExBtn)
                                        |> Seq.distinctBy(fun seg -> seg.SegName)

    and 
       ImportModel(name:string) as this =
            inherit DsModel(name)
            

            member x.Path = name
            member x.Name = Path.GetFileNameWithoutExtension(name) 
            member x.AddEdges(edges, rootFlow:RootFlow) = 
                edges|> Seq.iter(fun edge ->  this.AddEdge(edge, rootFlow)|>ignore)
     
            