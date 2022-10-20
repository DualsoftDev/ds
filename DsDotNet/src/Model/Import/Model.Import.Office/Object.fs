// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System
open System.IO
open System.Linq
open System.Diagnostics
open System.Collections.Concurrent
open System.Collections.Generic
open Model.Import.Office
open Engine.Core

[<AutoOpen>]
module Object =

 ///인과의 노드 종류
    type NodeType =
        | MY            //실제 나의 시스템 1 bit
        | TR            //지시관찰 TX RX 
        | TX            //지시만
        | RX            //관찰만
        | IF            //인터페이스
        | COPY          //시스템복사 
        | DUMMY         //그룹더미 
        | BUTTON        //버튼 emg,start, ...
        with
            member x.IsReal =   match x with
                                |MY  -> true
                                |_ -> false
            member x.IsCall =   match x with
                                |TR |TX |RX -> true
                                |_ -> false

            member x.IsRealorCall =  x.IsReal || x.IsCall 
    // 행위 Bound 정의
    type Bound =
        | ThisFlow         //이   MFlow        내부 행위정의
        | OtherFlow        //다른 MFlow     에서 행위 가져옴
        | ExBtn            //버튼(call) 가져옴

    and
        /// 사용자가 모델링을 통해서 만든 segment (SegEditor = User)
        [<DebuggerDisplay("{name}")>]
        MSeg(name:string, baseSystem:MSys, bound:Bound, nodeType:NodeType, rootFlow:RootFlow, bDummy:bool) as this =
            inherit SegBase(name, ChildFlow(name))
            let mChildFlow = (this :> SegBase).ChildFlow
            let mEdges = mChildFlow.Edges |> Seq.cast<MEdge>
            let mChildSegs   = mChildFlow.Nodes |> Seq.cast<MSeg>
            let ownerMFlow = rootFlow.Name
            new (name, baseSystem, rootFlow, nodeType) = MSeg (name, baseSystem,  ThisFlow, nodeType, rootFlow, false)
       
            member x.ChildFlow = mChildFlow 
            member x.NodeType = nodeType 
            member x.BaseSys = baseSystem
            member x.OwnerMFlow = ownerMFlow
            member x.Bound = bound

            member x.SetStatus(status:Status4) = 
                    this.Status4 <- status
                    ChangeStatus(this, status)


            member val ShapeID = 0u with get, set
            member val CountTX = 0 with get, set
            member val CountRX = 0 with get, set

            member x.ToTextInMFlow() = 
                                if(Bound.ThisFlow = bound) 
                                then x.Name
                                else sprintf "%s.%s"  ownerMFlow x.ValidName

            member x.FullName   = sprintf "%s.%s.%s" baseSystem.Name  ownerMFlow x.ValidName//    (if(x.Parent.IsSome) then x.Parent.Value.ValidName else "Root")
            member x.ApiName    = sprintf "%s"  (x.Name.Split('.').[1]) 


            member x.ValidName =  
                                if x.IsAlias
                                then x.AliasOrg.Value.ValidName
                                elif nodeType.IsCall
                                then sprintf "%s.%s" (NameUtil.QuoteOnDemand(x.Name.Split('.').[0])) (x.Name.Split('.').[1])
                                else NameUtil.QuoteOnDemand(x.Name)

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

            member x.Singles      = mChildFlow.Singles  |> Seq.cast<MSeg> |> Seq.sortBy(fun seg -> seg.Name)
            member x.IsDummy = bDummy
            member x.IsChildExist = mChildFlow.Nodes.Any()
            member x.IsChildEmpty = x.IsChildExist|>not
            member x.IsRoot =  x.Parent.IsNone
            member x.IsAlias = x.AliasOrg.IsSome
            member x.UIKey:string =    $"{if(x.IsAlias) then x.AliasOrg.Value.Name else x.Name};{x.Key}" 

            member val Key : string = "" with get, set
            member val AliasOrg : MSeg option = None  with get, set
            member val Parent : MSeg option = None with get, set
            member val S : string option = None    with get, set
            member val E : string option = None    with get, set

            member x.TagStart = if(x.S.IsSome) then x.S.Value else ""
            member x.TagEnd   = if(x.E.IsSome) then x.E.Value else ""

    and
        /// Modeled Edge : 사용자가 작성한 모델 상의 segment 간의 연결 edge (Wire)
        [<DebuggerDisplay("[{Source.FullName}]\t{Causal}\t[{Target.FullName}]")>]
        MEdge(src:MSeg, tgt:MSeg, causal:EdgeType) =
            inherit DsEdge(src, tgt, causal)
            member x.Source = src
            member x.Target = tgt
            member x.IsSameSys = src.BaseSys = tgt.BaseSys
            member x.SrcSystem = src.BaseSys
            member x.TgtSystem = tgt.BaseSys
            member x.Nodes = [src;tgt]
            member x.Causal = causal
            member x.IsRealorCall = src.NodeType.IsRealorCall && tgt.NodeType.IsRealorCall 
            member x.IsInterfaceEdge = src.NodeType = IF && tgt.NodeType = IF
            member val IsDummy = false with get,set
            member val IsSkipUI= false with get,set
            
            member x.ToText() = $"{src.Name}  {causal.ToText()}  {tgt.Name}"
            member x.ToCheckText(parentName:string) = 
                            let  checkText = if causal.IsStart() then "Start" else "Reset"
                            $"[{parentName}]{src.Name}  {checkText}  {tgt.Name}"

            member x.GetSegs() = [src;tgt]
    
    
    and
        /// MFlow : 페이지별 구성
        [<DebuggerDisplay("{Name}")>]
        MFlow private (name:string, system:MSys, index:int) as this  =
            inherit RootFlow(name, system)
            let mEdges       = (this :> RootFlow).Edges |> Seq.cast<MEdge>
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

            static member Create(name, sys, pageNum) =
                let mFlow = MFlow(name, sys, pageNum)
                if sys.Add(mFlow) |>not then failwith $"Duplicated flow name [{name}]"
                mFlow

            member x.Page = index
            member x.System = system;

            member x.CopyMEdges() = mEdges |> Seq.map(fun edge ->    MEdge(edge.Source, edge.Target, edge.Causal))
            member x.MEdges = mEdges |> Seq.sortBy(fun edge -> edge.Source.Name)
            member x.AddSafety(seg, segSafetys:MSeg seq) = safeties.TryAdd(seg, segSafetys) |> ignore 
            member x.Safeties = safeties
            member x.AliasSet   = aliasSet

            member x.Interlockedges = 
                        let FullNodesIL = interlocks.Values
                                            |> Seq.collect(fun seg -> [seg.Source;seg.Target])
                                            |> Seq.filter(fun seg -> seg.AliasOrg.IsNone)
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

            member x.DummySeg = dummySeg.Values 
            member x.AddDummySeg(seg) = dummySeg.TryAdd(seg) |> ignore 

            member x.UsedMSegs  = x.UsedSegs   |> Seq.cast<MSeg>
            member x.CallSegs() = x.UsedMSegs
                                    |> Seq.filter(fun seg -> seg.NodeType.IsCall)
                                    |> Seq.filter(fun seg -> seg.Bound = OtherFlow)
                                    |> Seq.map(fun seg -> if seg.IsAlias then seg.AliasOrg.Value else seg)
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
        MSys private (name:string, active:bool, model:MModel) as this  =
            inherit MSystem(name, model:>ModelBase)

            let systemFlow = RootFlow(name, this)
            let mutable sysSeg: System.Lazy<MSeg> = null
            let locationSet  = ConcurrentDictionary<string, System.Drawing.Rectangle>()
            let commandSet  = ConcurrentDictionary<string, string>()
            let observeSet  = ConcurrentDictionary<string, string>()
            let variableSet  = ConcurrentDictionary<string, DataType>()
            let addressSet  = ConcurrentDictionary<string, Tuple<string, string>>()
            let dicIf =  ConcurrentDictionary<string, MInterface>()
            let emgSet  =  this.EmergencyButtons 
            let startSet = this.AutoButtons     
            let resetSet = this.StartButtons 
            let autoSet  = this.ResetButtons 
            
            let updateBtn (btnType:BtnType, btnName, btnMFlow) = 
                let name = NameUtil.QuoteOnDemand(btnName)
                match btnType with
                |StartBTN ->    if(startSet.ContainsKey(name)) then startSet.[name].Add(btnMFlow) |>ignore else startSet.TryAdd(name, [btnMFlow] |> List) |>ignore
                |ResetBTN ->    if(resetSet.ContainsKey(name)) then resetSet.[name].Add(btnMFlow) |>ignore else resetSet.TryAdd(name, [btnMFlow] |> List) |>ignore
                |AutoBTN ->     if(autoSet.ContainsKey(name))  then autoSet.[name].Add(btnMFlow)  |>ignore else autoSet.TryAdd(name,  [btnMFlow] |> List) |>ignore
                |EmergencyBTN-> if(emgSet.ContainsKey(name))   then emgSet.[name].Add(btnMFlow)   |>ignore else emgSet.TryAdd(name,   [btnMFlow] |> List) |>ignore


            static member Create(name:string, active:bool, model:MModel) =
                let system = MSys(name, active, model)
                if model.Add(system) |>not then failwith $"Duplicated system name [{name}]"
                system

            member x.AddInterface(ifName:string, txs:IVertex seq , rxs:IVertex seq ) =
                                dicIf.TryAdd(ifName, MInterface(ifName, txs, rxs)) |> ignore

            member x.IFNames     =  dicIf |> Seq.map(fun dic-> dic.Value.Name)
            member x.IFFullNames =  dicIf |> Seq.map(fun dic-> name+"."+dic.Value.Name)
            member x.IFs         =  dicIf.Values
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
            
            member x.Flows = this.RootFlows   |> Seq.cast<MFlow>
           
            member x.RootMFlow()  = this.RootFlows |> Seq.sortBy(fun flow -> flow.Name)
            member x.BtnSegs()    = this.AllNodes
                                        |> Seq.filter(fun seg -> seg.Bound = ExBtn)
                                        |> Seq.distinctBy(fun seg -> seg.Name)

    and 
        [<DebuggerDisplay("{Name}")>]
       MModel(name:string) as this  =
            inherit ModelBase()
            let systems = ConcurrentDictionary<string, MSys>()

            //모델에 시스템 등록 및 삭제
            member x.Add(sys:MSys) = systems.TryAdd(sys.Name, sys)
            member x.Remove(sys:MSys) = systems.TryRemove(sys.Name)

            /// TotalSystems
            member x.DicSystems      = systems
            member x.Systems      = systems.Values
            member x.ActiveSys      = systems.Values |> Seq.filter(fun w-> w.Active) |> Seq.head
            //member x.SysActives  = 
            //    let activeSys = systems.Values |> Seq.filter (fun sys -> sys.Active)
            //    if((activeSys |> Seq.length) <> 1) then failwith "한개 이상의 Active 시스템 설정이 필요합니다."
            member x.AllFlows      = systems.Values |> Seq.collect(fun sys -> sys.RootFlows) 
            member x.Path = name
            member x.Name = Path.GetFileNameWithoutExtension(name) 
            member x.Flows = this.AllFlows |> Seq.cast<MFlow> 
            member x.GetFlow(page:int) = x.Flows.Where(fun flow -> flow.Page = page) |> Seq.head
          