// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System
open System.IO
open System.Linq
open System.Diagnostics
open System.Collections.Concurrent
open System.Collections.Generic
open Engine.Core
open Engine.Core.CoreClass
open Engine.Core.CoreFlow
open Engine.Core.CoreStruct

[<AutoOpen>]
module Object =

 ///인과의 노드 종류
    type NodeType =
        | MY            //실제 나의 시스템 1 bit
        | TR            //지시관찰 TX RX 
        | TX            //지시만
        | RX            //관찰만
        with
            member x.IsReal =   match x with
                                |MY  -> true
                                |_ -> false
            member x.IsCall =   match x with
                                |TR |TX |RX -> true
                                |_ -> false
    // 행위 Bound 정의
    type Bound =
        | ThisFlow         //이   MFlow        내부 행위정의
        | OtherFlow        //다른 MFlow     에서 행위 가져옴
        | ExBtn            //버튼(call) 가져옴

    and
        /// 사용자가 모델링을 통해서 만든 segment (SegEditor = User)
        [<DebuggerDisplay("{FullName}")>]
        MSeg(name:string, baseSystem:MSys, bound:Bound, nodeType:NodeType, rootFlow:RootFlow, bDummy:bool) as this =
            inherit SegmentBase(name, ChildFlow(name, rootFlow))
            let mChildFlow = (this :> SegmentBase).ChildFlow
            let mEdges = mChildFlow.Edges |> Seq.cast<MEdge>
            let mChildSegs   = mChildFlow.Nodes |> Seq.cast<MSeg>
            let ownerMFlow = rootFlow.Name
            new (name, baseSystem, rootFlow, nodeType) = MSeg (name, baseSystem,  ThisFlow, nodeType, rootFlow, false)
       
            member x.ChildFlow = mChildFlow 
            member x.NodeType = nodeType 
            member x.BaseSys = baseSystem
            member x.Bound = bound

            member x.SetStatus(status:Status4) = 
                    this.Status4 <- status
                    ChangeStatus(this, status)


            member val ShapeID = 0u with get, set
            member val CountTX = 0 with get, set
            member val CountRX = 0 with get, set

            member x.OwnerMFlow = ownerMFlow
            member x.ToCallText() = let call = sprintf "%s_%s"  (ownerMFlow.TrimStart('\"').TrimEnd('\"')) name
                                    NameUtil.GetValidName(call)

            member x.ToTextInMFlow() = 
                                            if(Bound.ThisFlow = bound) 
                                            then x.SegName
                                            else x.MFlowNSeg

            ///금칙 문자 및 선두숫자가 있으면 "" 로 이름 앞뒤에 배치한다.
            ///Alias 는 무조건 "" 로 이름 앞뒤에 배치
            member x.SegName  = sprintf "%s" x.ValidName//(if(this.IsAlias) then x.ValidName else this.Alias.Value.ValidName)
            member x.MFlowNSeg= sprintf "%s.%s"  ownerMFlow x.ValidName
            member x.FullName = sprintf "%s.%s.%s" baseSystem.Name  ownerMFlow x.ValidName  
            member x.PathName = sprintf "%s(%s)" x.FullName (if(x.Parent.IsSome) then x.Parent.Value.Name else "Root")

            member x.Update(nodeKey, nodeIdValue, nodeCntTX, nodeCntRX) = 
                        this.Key <- nodeKey
                        this.ShapeID <- nodeIdValue
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
            member x.ChildSegs = mChildSegs
            //재귀적으로 자식 Seg를 가져옴
            member x.ChildSegsSubAll =
                   mChildSegs
                   |> Seq.collect(fun e -> e.ChildSegsSubAll)
                   |> Seq.append x.ChildSegs

            member x.NoEdgeSegs      = mChildFlow.Singles  |> Seq.cast<MSeg> |> Seq.sortBy(fun seg -> seg.Name)
            member x.IsDummy = bDummy
            member x.IsChildExist = mChildFlow.Nodes.Any()
            member x.IsChildEmpty = x.IsChildExist|>not
            member x.IsRoot =  x.Parent.IsSome && x.Parent.Value.Bound = ThisFlow
            member x.UIKey:string =  
                                let name = if(x.IsAlias) then x.Alias.Value.Name else x.Name
                                $"{name};{x.Key}"
            member val Key : string = "" with get, set
            member val Parent : MSeg option = None with get, set
            member val S : string option = None    with get, set
            member val E : string option = None    with get, set

            member x.TagStart = if(x.S.IsSome) then x.S.Value else ""
            member x.TagEnd   = if(x.E.IsSome) then x.E.Value else ""

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
            
           

            member x.ToText() = $"{src.SegName}  {causal.ToText()}  {tgt.SegName}"
            member x.ToCheckText(parentName:string) = 
                            let  checkText = match causal with
                                             |SEdge |SPush |  SReset-> "Start"
                                             |REdge |RPush |  Interlock-> "Reset"
                            $"[{parentName}]{src.ToCallText()}  {checkText}  {tgt.ToCallText()}"

            member x.GetSegs() = [src;tgt]
    
    
    and
        /// MFlow : 페이지별 구성
        [<DebuggerDisplay("{Name}")>]
        MFlow(name:string, system:DsSystem, index:int) as this  =
            inherit RootFlow(name, system)
            let mRootFlow = (this :> RootFlow)
            let mEdges       = mRootFlow.Edges |> Seq.cast<MEdge>
            let mChildSegs   = mRootFlow.Nodes |> Seq.cast<MSeg>

            let drawSubs  = ConcurrentHash<MSeg>()
            let dummySeg  = ConcurrentHash<MSeg>()
            let setIL  = ConcurrentHash<HashSet<MSeg>>()
            let safeties  = ConcurrentDictionary<MSeg, MSeg seq>()
            let interlocks  = ConcurrentDictionary<string, MEdge>()
            let aliasSet  = ConcurrentDictionary<string, HashSet<string>>()

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

            member x.Page = index

            member x.MEdges = mEdges |> Seq.sortBy(fun edge -> edge.Source.Name)
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

            member x.UsedMSegs   = x.UsedSegs   |> Seq.cast<MSeg>
            member x.CallSegs() = 
                                        x.UsedMSegs
                                        |> Seq.filter(fun seg -> seg.NodeType.IsCall)
                                        |> Seq.filter(fun seg -> seg.Bound = ThisFlow)
                                        |> Seq.map(fun seg -> if seg.IsAlias then seg.Alias.Value else seg)
                                        |> Seq.cast<MSeg>
                                        |> Seq.sortBy(fun seg -> seg.FullName)
                                        |> Seq.distinctBy(fun seg -> seg.FullName)


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
        MSys(name:string, active:bool, model:Model) as this  =
            inherit DsSystem(name, model)

            let systemFlow = RootFlow(name, this)
            let mutable sysSeg: System.Lazy<MSeg> = null
            let locationSet  = ConcurrentDictionary<string, System.Drawing.Rectangle>()
            let commandSet  = ConcurrentDictionary<string, string>()
            let observeSet  = ConcurrentDictionary<string, string>()
            let variableSet  = ConcurrentDictionary<string, DataType>()
            let addressSet  = ConcurrentDictionary<string, Tuple<string, string>>()
            //let noEdgesSegs = mFlows |> Seq.collect(fun f-> f.Value.NoEdgeSegs)
            let emgSet  =  this.EmergencyButtons 
            let startSet = this.AutoButtons     
            let resetSet = this.StartButtons 
            let autoSet  = this.ResetButtons 
            
            let updateBtn (btnType:BtnType, btnName, btnMFlow) = 
                let name = NameUtil.GetValidName(btnName)
                match btnType with
                |StartBTN ->    if(startSet.ContainsKey(name)) then startSet.[name].Add(btnMFlow) |>ignore else startSet.TryAdd(name, [btnMFlow] |> List) |>ignore
                |ResetBTN ->    if(resetSet.ContainsKey(name)) then resetSet.[name].Add(btnMFlow) |>ignore else resetSet.TryAdd(name, [btnMFlow] |> List) |>ignore
                |AutoBTN ->     if(autoSet.ContainsKey(name))  then autoSet.[name].Add(btnMFlow)  |>ignore else autoSet.TryAdd(name,  [btnMFlow] |> List) |>ignore
                |EmergencyBTN-> if(emgSet.ContainsKey(name))   then emgSet.[name].Add(btnMFlow)   |>ignore else emgSet.TryAdd(name,   [btnMFlow] |> List) |>ignore


            //new (name:string)       = new MSys(name,  false)
            //new (name, active:bool) = new MSys(name,  active)

            member x.Name = name
            member x.SysSeg =
                if isNull sysSeg then
                    sysSeg <- Lazy(fun () -> MSeg("(ENG)Main_"+name, x, systemFlow, MY))
                sysSeg.Value

            member val Debug = false   with get, set
            member val Active = active with get, set
            member val SystemID = -1   with get, set
            member x.OrderPageRootFlows() = 
                this.RootFlows 
                 |> Seq.cast<MFlow>
                 |> Seq.sortBy(fun flow -> flow.Page) 

            member x.SingleNodes   = this.RootFlows
                                        |> Seq.collect(fun flow -> flow.Singles)
                                        |> Seq.cast<MSeg>
            member x.AllNodes  = this.RootFlows
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

            member x.RootMFlow()  = this.RootFlows |> Seq.sortBy(fun flow -> flow.Name)
            member x.BtnSegs()    = this.AllNodes
                                        |> Seq.filter(fun seg -> seg.Bound = ExBtn)
                                        |> Seq.distinctBy(fun seg -> seg.SegName)

    and 
       ImportModel(name:string) as this =
            inherit Model()
            

            member x.Path = name
            member x.Name = Path.GetFileNameWithoutExtension(name) 
            member x.AddEdges(edges, rootFlow:RootFlow) = 

                edges|> Seq.iter(fun (edge: #DsEdge) -> 
                
                         //   edge.Target.Alias <- Some(edge.Source)
                            this.AddEdge(edge, rootFlow)|>ignore)
     
            