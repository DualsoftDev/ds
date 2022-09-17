// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System
open System.Linq
open System.Diagnostics
open System.Collections.Concurrent
open System.Collections.Generic
open DocumentFormat.OpenXml
open Engine.Core
open Engine.Core.DsType
open Engine.Util

[<AutoOpen>]
module Object =

    // 행위 Bound 정의
    type Bound =
        | ThisFlow         //나의 System 의 이 Flow        내부 행위정의
        | OtherFlow        //나의 System 의 다른 Flow     에서 행위 가져옴
        | ExSeg            //외부 System 의 에서 행위(real) 가져옴
        | ExBtn            //외부 System 의 에서 버튼(call) 가져옴

    //Seg 편집자
    type Editor =
        | User      //사용자 
        | Engine    //Dualsoft Engine 자동 생성
    
    /// Start/Reset/End relay tuple
    and
        /// 사용자가 모델링을 통해서 만든 segment (SegEditor = User)
        [<DebuggerDisplay("{FullName}")>]
        Seg(name:string, baseSystem:DsSys, editor:Editor, bound:Bound, nodeCausal:NodeCausal, ownerFlow:string, bDummy:bool) as this =
            inherit SegBase(name,  baseSystem)
            /// modeled edges
            let mutable status4 = Status4.Homing
            let mEdges  = ConcurrentHash<MEdge>()

            new (name, baseSystem, nodeCausal) = Seg (name, baseSystem, Editor.User,   ThisFlow, nodeCausal, "", false)
            new (name, baseSystem)             = Seg (name, baseSystem, Editor.Engine, ThisFlow, MY        , "", false)

            member x.NodeCausal = nodeCausal
            member x.Status4 = status4 
            member x.SetStatus(s:Status4) = status4 <- s
                                            ChangeStatus(this, s)

            member x.BaseSys = baseSystem
            member x.Editor = editor
            member x.Bound = bound
            member x.RemoveMEdge(edge:MEdge) = mEdges.TryRemove(edge) |> ignore
            member x.AddMEdge(edge:MEdge) =
                    mEdges.TryAdd(edge) |> ignore
                    let src = edge.Source
                    let tgt = edge.Target
                    if(this = src) then failwith $"parent [{this.SegName}] = SourceVertex [{src.SegName}]"
                    if(this = tgt) then failwith $"parent [{this.SegName}] = TargetVertex [{tgt.SegName}]"

            member val Alias :string  option = None with get, set
            member val ShapeID = 0u with get, set
            member val CountTX = 0 with get, set
            member val CountRX = 0 with get, set
            member x.OwnerFlow = ownerFlow
            member x.ToCallText() = let call = sprintf "%s_%s"  (ownerFlow.TrimStart('\"').TrimEnd('\"')) name
                                    Util.GetValidName(call)

            member x.ToTextInFlow() =  match nodeCausal with
                                         |EX -> sprintf "EX.%s.EX" (x.ToCallText())
                                         |_  -> if(ThisFlow = bound) 
                                                then x.SegName
                                                else x.FlowNSeg

            ///금칙 문자 및 선두숫자가 있으면 "" 로 이름 앞뒤에 배치한다.
            ///Alias 는 무조건 "" 로 이름 앞뒤에 배치
            member x.SegName  = sprintf "%s" (if(this.Alias.IsSome) then this.Alias.Value else Util.GetValidName(name))
            member x.FlowNSeg = sprintf "%s.%s"  ownerFlow (Util.GetValidName(name))
            member x.FullName = sprintf "%s.%s.%s" baseSystem.Name  ownerFlow (Util.GetValidName(name))  
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
  
            member x.MEdges = mEdges.Values  |> Seq.sortBy(fun edge ->edge.ToText())
            member x.ChildSegs =
                mEdges.Values
                |> Seq.collect(fun e -> e.Nodes)
                |> Seq.cast<Seg>
                |> Seq.distinct

            //재귀적으로 자식 Seg를 가져옴 (다른시스템은 Root까지)
            member x.ChildSegsSubAll =
                   x.ChildSegs
                   |> Seq.collect(fun e -> e.ChildSegsSubAll)
                   |> Seq.append x.ChildSegs
                   |> Seq.append x.NoEdgeSegs
        
            member x.IsDummy = bDummy
            member x.IsChildExist = mEdges.Any()
            member x.IsChildEmpty = mEdges.IsEmpty
            member x.IsRoot =  x.Parent.IsSome && x.Parent.Value.Bound = ThisFlow
            member x.UIKey:string =  $"{x.Name};{x.Key}"
            member val Key : string = "" with get, set
            member val Parent : Seg option = None with get, set
            member val S : string option = None  with get, set
            member val R : string option = None  with get, set
            member val E : string option = None  with get, set

            member x.TextStart = if(x.S.IsSome) then x.S.Value else ""
            member x.TextReset = if(x.R.IsSome) then x.R.Value else ""
            member x.TextEnd   = if(x.E.IsSome) then x.E.Value else ""

            member x.AddChildNSetParent(edge:MEdge) =  
                edge.Source.Parent <- Some(x)  
                edge.Target.Parent <- Some(x)  
           
                x.AddMEdge(edge)

            member x.NoEdgeSegs = x.NoEdgeBaseSegs |> Seq.cast<Seg> |> Seq.sortBy(fun s -> s.SegName)

     

    and
        /// Modeled Edge : 사용자가 작성한 모델 상의 segment 간의 연결 edge (Wire)
        [<DebuggerDisplay("{Source.FullName}{Causal.ToText()}{Target.FullName}")>]
        MEdge(src:Seg, tgt:Seg, causal:EdgeCausal) =
            inherit EdgeBase(src, tgt, causal)
            member x.Source = src
            member x.Target = tgt
            member x.IsSameSys = src.BaseSys = tgt.BaseSys
            member x.SrcSystem = src.BaseSys
            member x.TgtSystem = tgt.BaseSys
            
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
        /// Flo : 페이지별 구성
        [<DebuggerDisplay("{Name}")>]
        Flo(name:string, index:int, baseSystem)  =
            inherit SegBase(name,  baseSystem)
            let drawSubs  = ConcurrentHash<Seg>()
            let dummySeg  = ConcurrentHash<Seg>()
            let safeties  = ConcurrentDictionary<Seg, Seg seq>()
            let edges  = ConcurrentHash<MEdge>()
            let interlocks  = ConcurrentDictionary<string, MEdge>()
            let setIL  = ConcurrentHash<HashSet<Seg>>()
            let aliasSet  = ConcurrentDictionary<string, HashSet<string>>()

            let rec getLink(start:Seg, find:HashSet<Seg>, full:HashSet<Seg>) =
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

            member x.Name = name
            member x.ToText() =  name
            member x.Page = index

            member x.Edges = edges.Values |> Seq.sortBy(fun edge -> edge.Source.Name)
            member x.AddEdge(edge) = edges.TryAdd(edge) |> ignore 
            member x.AddSafety(seg, segSafetys:Seg seq) = safeties.TryAdd(seg, segSafetys) |> ignore 
            member x.Safeties = safeties
            member x.AliasSet   = aliasSet

            member x.Interlockedges = 
                        let FullNodesIL = interlocks.Values.GetNodes() 
                                            |> Seq.cast<Seg>
                                            |> Seq.filter(fun seg -> seg.Alias.IsNone)
                                            |> HashSet
                        interlocks.Values.GetNodes()
                                        |> Seq.cast<Seg>
                                        |> Seq.filter(fun seg -> FullNodesIL.Contains(seg))
                                        |> Seq.iter(fun seg -> 
                                                    let findSet = HashSet<Seg>()
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
            member x.NoEdgeSegs =  x.NoEdgeBaseSegs  |> Seq.cast<Seg>

            member x.UsedSegs =
                let rootUsedSegs  = 
                    edges 
                    |> Seq.collect(fun edge -> edge.Key.GetSegs())
                    |> Seq.append x.NoEdgeSegs

                rootUsedSegs 
                |> Seq.collect (fun node -> node.ChildSegsSubAll)
                |> Seq.append rootUsedSegs
                |> Seq.distinctBy(fun seg -> seg.PathName)
                    
            member x.CallSegs() = x.UsedSegs
                                        |> Seq.filter(fun seg -> seg.NodeCausal.IsCall)
                                        |> Seq.filter(fun seg -> seg.Bound = ThisFlow)
                                        |> Seq.distinctBy(fun seg -> seg.FullName)

            member x.ExRealSegs() = x.UsedSegs
                                        |> Seq.filter(fun seg -> seg.NodeCausal.IsReal)
                                        |> Seq.filter(fun seg -> seg.Bound = ExSeg)
                                        |> Seq.distinctBy(fun seg -> seg.FullName)

            //member x.BtnSegs()  =  x.UsedSegs 
            //                            |> Seq.filter(fun seg -> seg.Bound = ExBtn)
            //                            |> Seq.distinctBy(fun seg -> seg.SegName)

            member x.NotMySegs() =  x.CallSegs() |> Seq.append (x.ExRealSegs())
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
        DsSys(name:string, active:bool)  =
            inherit SystemBase(name)
            let mutable sysSeg: System.Lazy<Seg> = null
            let flows  = ConcurrentDictionary<int, Flo>()
            let locationSet  = ConcurrentDictionary<string, System.Drawing.Rectangle>()
            let commandSet  = ConcurrentDictionary<string, string>()
            let observeSet  = ConcurrentDictionary<string, string>()
            let variableSet  = ConcurrentDictionary<string, DataType>()
            let addressSet  = ConcurrentDictionary<string, Tuple<string, string, string>>()
            let noEdgesSegs = flows |> Seq.collect(fun f-> f.Value.NoEdgeSegs)
            let emgSet  = ConcurrentDictionary<string, List<Flo>>()
            let startSet  = ConcurrentDictionary<string, List<Flo>>()
            let resetSet  = ConcurrentDictionary<string, List<Flo>>()
            let autoSet   = ConcurrentDictionary<string, List<Flo>>()
            
            let updateBtn (btnType:BtnType, btnName, btnFlow) = 
                let name = GetValidName(btnName)
                match btnType with
                |StartBTN ->    if(startSet.ContainsKey(name)) then startSet.[name].Add(btnFlow) |>ignore else startSet.TryAdd(name, [btnFlow] |> List) |>ignore
                |ResetBTN ->    if(resetSet.ContainsKey(name)) then resetSet.[name].Add(btnFlow) |>ignore else resetSet.TryAdd(name, [btnFlow] |> List) |>ignore
                |AutoBTN ->     if(autoSet.ContainsKey(name))  then autoSet.[name].Add(btnFlow)  |>ignore else autoSet.TryAdd(name,  [btnFlow] |> List) |>ignore
                |EmergencyBTN-> if(emgSet.ContainsKey(name))   then emgSet.[name].Add(btnFlow)   |>ignore else emgSet.TryAdd(name,   [btnFlow] |> List) |>ignore


            new (name:string)       = new DsSys(name,  false)
            new (name, active:bool) = new DsSys(name,  active)

            member x.SysSeg =
                if isNull sysSeg then
                    sysSeg <- Lazy(fun () -> Seg("(ENG)Main_"+name, x))
                sysSeg.Value

            member val Debug = false   with get, set
            member val Active = active with get, set
            member val SystemID = -1   with get, set

            member x.Flows      = flows
            member x.LocationSet   = locationSet
            member x.CommandSet   = commandSet
            member x.ObserveSet   = observeSet
            member x.VariableSet   = variableSet
            member x.AddressSet   = addressSet
            member x.EmgSet   = emgSet
            member x.StartSet   = startSet
            member x.ResetSet   = resetSet
            member x.AutoSet   = autoSet

            member x.TryAddStartBTN(name, flow) = updateBtn(StartBTN, name, flow)
            member x.TryAddResetBTN(name, flow) = updateBtn(ResetBTN, name, flow)
            member x.TryAddAutoBTN(name, flow) = updateBtn(AutoBTN, name, flow)
            member x.TryAddEmergBTN(name, flow) = updateBtn(EmergencyBTN, name, flow)

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
                                        |> Seq.append (noEdgesSegs)

                                segs
                                |> Seq.collect(fun seg -> seg.ChildSegsSubAll) |> Seq.append segs
                                |> Seq.sortBy(fun seg -> seg.Name)
                                |> Seq.distinct
            

            member x.RootEdges()    = x.SysSeg.MEdges 

            member x.NotRootSegs() = 
                x.SysSeg.MEdges.GetNodes()
                    |> Seq.cast<Seg>
                    |> Seq.append noEdgesSegs
                    |> Seq.collect(fun seg -> seg.ChildSegs)
                    |> Seq.distinct

            member x.RootSegs() =
                x.SysSeg.ChildSegs
                    |> Seq.append noEdgesSegs
                    |> Seq.distinct

            member x.RootFlow()    = flows
                                     |> Seq.sortBy(fun flow -> flow.Key)
                                     |> Seq.map(fun flow -> flow.Value)
                                     |> Seq.filter(fun flow -> (flow.Page = Int32.MaxValue)|>not)  //Int32.MaxValue 는 데모 flow
            
            member x.BtnSegs()    = 
                                    x.RootFlow() 
                                    |> Seq.collect(fun flow -> flow.UsedSegs)
                                    |> Seq.filter(fun seg -> seg.Bound = ExBtn)
                                    |> Seq.distinctBy(fun seg -> seg.SegName)

