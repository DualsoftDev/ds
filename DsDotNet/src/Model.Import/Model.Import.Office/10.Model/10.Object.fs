// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System
open System.Linq
open System.Diagnostics
open System.Collections.Concurrent
open System.Collections.Generic
open DocumentFormat.OpenXml
open Engine.Base

[<AutoOpen>]
module Object =

    // 행위 Bound 정의
    type Bound =
        | Normal    //사용자 정의한 대부분 Seg
        | External  //외부 행위의 인에 해당하는 Engine에서 만들어낸 Dummy Seg
        | System    //ParentRoot  Seg 의 부모 Seg(전원시 Going 비전원시 Ready)

    //Seg 편집자
    type Editor =
        | User      //사용자 
        | Engine    //Dualsoft Engine 자동 생성
    
    /// Start/Reset/End relay tuple
    and
        /// 사용자가 모델링을 통해서 만든 segment (SegEditor = User)
        [<DebuggerDisplay("{ToText()}")>]
        Seg(name:string, baseSystem:DsSystem, editor:Editor, location:Bound, nodeCausal:NodeCausal,  ownerFlow:string, bDummy:bool) as this =
            inherit SegBase(name,  baseSystem)
            /// modeled edges
            let mutable status4 = Status4.Homing
            let mEdges  = ConcurrentHash<MEdge>()

            new (name, baseSystem, nodeCausal) = Seg (name, baseSystem, Editor.User, Normal, nodeCausal,  "", false)
            new (name, baseSystem) = Seg (name, baseSystem, Editor.Engine, Normal, MY,  "", false)

            member x.NodeCausal = nodeCausal
            member x.Status4 = status4 
            member x.SetStatus(s:Status4) = status4 <- s
                                            ChangeStatus(this, s)

            member x.BaseSys = baseSystem
            member x.Editor = editor
            member x.Location = location
            member x.RemoveMEdge(edge:MEdge) = mEdges.TryRemove(edge) |> ignore
            member x.AddMEdge(edge:MEdge) =
                    mEdges.TryAdd(edge) |> ignore
                    let src = edge.Source
                    let tgt = edge.Target
                    if(this = src) then failwith $"parent [{this.ToText()}] = SourceVertex [{src.ToText()}]"
                    if(this = tgt) then failwith $"parent [{this.ToText()}] = TargetVertex [{tgt.ToText()}]"

            member val Alias :string  option = None with get, set
            member val ShapeID = 0u with get, set
            member val CountTX = 0 with get, set
            member val CountRX = 0 with get, set
            member x.OwnerFlow = ownerFlow
            member x.DisplayName  = if(this.Alias.IsSome) then this.Alias.Value else name
            
            ///공백은'_' 로 일괄 변환한다.
            ///금칙 문자 및 선두숫자가 있으면 "" 로 이름 앞뒤에 배치한다.
            member x.ToText() =  let _name =  this.Name.Replace(" ","_") 
                                 if(IsInvalidName(_name))  then $"\"{_name}\"" else _name

            member x.ToTextInFlow() =  match nodeCausal with
                                         |MY         -> x.ToText()
                                         |TR |TX |RX -> sprintf "%s_T.%s" x.OwnerFlow (x.ToText())
                                         |EX         -> sprintf "EX.%s.TR" (x.ToText())
            
            member x.ToCallText() = let callName = sprintf "%s_%s"   this.OwnerFlow (x.Name.Replace(" ","_")) 
                                    if(IsInvalidName(callName))  then $"\"{callName}\"" else callName
            member x.ToLayOutPath() = sprintf "%s.%s" baseSystem.Name (this.ToTextInFlow())

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

        
            member x.IsDummy = bDummy
            member x.IsChildExist = mEdges.Any()
            member x.IsChildEmpty = mEdges.IsEmpty
            member x.IsRoot =  x.Parent.IsSome && x.Parent.Value.Location = System
            member x.UIKey:string =  $"{x.DisplayName};{x.Key}"
            member val Key : string = "" with get, set
            member val Parent : Seg option = None with get, set
            member val S = "" with get, set
            member val R = "" with get, set
            member val E = "" with get, set


            member x.AddChildNSetParent(edge:MEdge) =  
                edge.Source.Parent <- Some(x)  
                edge.Target.Parent <- Some(x)  
           
                x.AddMEdge(edge)

            member x.NoEdgeSegs = x.NoEdgeSubSegs |> Seq.cast<Seg> |> Seq.sortBy(fun s -> s.DisplayName)

     

    and
        /// Modeled Edge : 사용자가 작성한 모델 상의 segment 간의 연결 edge (Wire)
        [<DebuggerDisplay("{Source.ToText()}{Causal.ToText()}{Target.ToText()}")>]
        MEdge(src:Seg, tgt:Seg, causal:EdgeCausal) =
            inherit EdgeBase(src, tgt, causal)
            member x.Source = src
            member x.Target = tgt
            member x.IsSameSys = src.BaseSys = tgt.BaseSys
            member x.SrcSystem = src.BaseSys
            member x.TgtSystem = tgt.BaseSys

            member x.ToText() = $"{src.ToText()}  {causal.ToText()}  {tgt.ToText()}"
            member x.ToCheckText(parentName:string) = 
                            let srcName = if(src.Alias.IsSome) then src.Alias.Value else src.ToCallText()
                            let tgtName = if(tgt.Alias.IsSome) then tgt.Alias.Value else tgt.ToCallText()
                            $"[{parentName}]{srcName}  {causal.ToCheckText()}  {tgtName}"
    
    
    and
        /// Flo : 페이지별 구성
        [<DebuggerDisplay("{Name}")>]
        Flo(name:string, index:int, baseSystem)  =
            inherit SegBase(name,  baseSystem)
            let drawSubs  = ConcurrentHash<Seg>()
            let dummySeg  = ConcurrentHash<Seg>()
            let edges  = ConcurrentHash<MEdge>()
            let interlocks  = ConcurrentHash<MEdge>()
            let setIL  = ConcurrentHash<HashSet<Seg>>()

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
            member x.ToText() =  let _name =  x.Name.Replace(" ","_") 
                                 if(IsInvalidName(_name))  then $"\"{_name}\"" else _name

            member x.Page = index

            member x.Edges = edges.Values |> Seq.sortBy(fun edge -> edge.Source.Name)
            member x.AddEdge(edge) = edges.TryAdd(edge) |> ignore 

            member x.Interlockedges = 
                        let FullNodesIL = interlocks.Values.GetNodes() |> Seq.cast<Seg> |> HashSet
                        interlocks.Values.GetNodes()
                                        |> Seq.cast<Seg>
                                        |> Seq.filter(fun seg -> FullNodesIL.Contains(seg))
                                        |> Seq.iter(fun seg -> 
                                                    let findSet = HashSet<Seg>()
                                                    getLink (seg, findSet, FullNodesIL)
                                                    if(findSet.Any()) then setIL.TryAdd(findSet) |> ignore
                                            )
                        setIL.Values

            member x.AddInterlock(edge) = interlocks.TryAdd(edge) |> ignore 

            member x.DrawSubs = drawSubs.Values |> Seq.sortBy(fun seg -> seg.Name)
            member x.AddSegDrawSub(seg) = drawSubs.TryAdd(seg) |> ignore 

            member x.DummySeg = dummySeg.Values 
            member x.AddDummySeg(seg) = dummySeg.TryAdd(seg) |> ignore 

            member x.NoEdgeSegs = x.NoEdgeSubSegs  |> Seq.cast<Seg>
            member x.ExportSegs = edges.Values
                                    |> Seq.collect(fun edge -> edge.Nodes) 
                                    |> Seq.append x.NoEdgeSubSegs 
                                    |> Seq.cast<Seg> 
                                    |> Seq.filter(fun seg -> seg.MEdges.Any()) 
                                    |> Seq.distinctBy(fun seg -> seg.Name)

            member x.UsedSegs = let children = 
                                    edges.Values.GetNodes()
                                    |> Seq.cast<Seg>
                                    |> Seq.collect (fun node -> node.ChildSegsSubAll)
                                x.NoEdgeSubSegs 
                                |> Seq.cast<Seg>
                                |> Seq.collect (fun parent -> parent.MEdges.GetNodes())
                                |> Seq.append (x.Edges.GetNodes())
                                |> Seq.cast<Seg>
                                |> Seq.append children
                                |> Seq.append (x.NoEdgeSubSegs |> Seq.cast<Seg>)
                                |> Seq.toList

            
            member x.CallSegs() = x.UsedSegs
                                        |> Seq.filter(fun seg -> seg.NodeCausal.IsCall)
                                        |> Seq.distinctBy(fun seg -> seg.Name)

            member x.CallWithoutInterLock() 
                = x.CallSegs()
                  |> Seq.filter(fun seg -> interlocks.Values.GetNodes().Contains(seg)|>not)

            member x.ExSegs() = x.UsedSegs
                                        |> Seq.filter(fun seg -> seg.NodeCausal = EX)
                                        |> Seq.distinctBy(fun seg -> seg.Name)

              
    and
        [<DebuggerDisplay("{Name}")>]
        /// System 내부 Seg의 내외부 Seg간 시작/리셋 연결 정보 구조
        DsSystem(name:string, active:bool)  =
            inherit SystemBase(name)
            let mutable sysSeg: System.Lazy<Seg> = null
            let flows  = ConcurrentDictionary<int, Flo>()
            let aliasSet  = ConcurrentDictionary<string, HashSet<string>>()
            let locationSet  = ConcurrentDictionary<string, System.Drawing.Rectangle>()
            let commandSet  = ConcurrentDictionary<string, string>()
            let observeSet  = ConcurrentDictionary<string, string>()
            let variableSet  = ConcurrentDictionary<string, DataType>()
            let addressSet  = ConcurrentDictionary<string, Tuple<string, string, string>>()
            let noEdgesSegs = flows |> Seq.collect(fun f-> f.Value.NoEdgeSubSegs) |> Seq.cast<Seg>

            new (name:string)       = new DsSystem(name,  false)
            new (name, active:bool) = new DsSystem(name,  active)

            member x.SysSeg =
                if isNull sysSeg then
                    sysSeg <- Lazy(fun () -> Seg("(ENG)Main_"+name, x))
                sysSeg.Value

            member val Debug = false   with get, set
            member val Active = active with get, set
            member val SystemID = -1   with get, set

            member x.Flows      = flows
            member x.AliasSet   = aliasSet
            member x.LocationSet   = locationSet
            member x.CommandSet   = commandSet
            member x.ObserveSet   = observeSet
            member x.VariableSet   = variableSet
            member x.AddressSet   = addressSet
            
            member x.Segs()     =   let segs = 
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
            
         