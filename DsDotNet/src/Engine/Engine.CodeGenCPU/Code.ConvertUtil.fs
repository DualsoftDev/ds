namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open Dual.Common.Core.FS

[<AutoOpen>]
module CodeConvertUtil =

        ///Real 자신을 공용으로 사용하는 Vertex들
    let getSharedReal(v:VertexManager) : Vertex seq =
            (v.Vertex :?> Real).GetVertexSharedReal()

        ///CallDev 자신을 공용으로 사용하는 Vertex들
    let getSharedCall(v:VertexManager) : Vertex seq =
            (v.Vertex :?> CallDev).GetVertexSharedCall()

      


    let private getOriginTasks(vr:VertexMReal, initialType:InitialType) =
        let origins = vr.OriginInfo.Tasks
        origins
            .Where(fun (_, init) -> init = initialType)
            .Select(fun (task, _) -> task)

    let getOriginIOExprs(vr:VertexMReal, initialType:InitialType) =
        let vs = getOriginTasks(vr, initialType)
        let vs = vs.Where(fun f->f.InTag.IsNonNull())
        vs
          .Where(fun f-> f.ExistIn)
          .Select(fun f-> f.ActionINFunc)

    let getOriginSimPlanEnds(vr:VertexMReal, initialType:InitialType) =
        getOriginTasks(vr, initialType).Select(fun f->f.ApiItem.PE)

    /// Edge source 검색 결과 정보 : target 으로 들어오는 source vertices list 와 그것들이 약연결로 들어오는지, 강연결로 들어오는지 정보
    type EdgeSourcesWithStrength =
        | DuEssWeak of Vertex list
        | DuEssStrong of Vertex list
        | DuEssNone

    /// returns [week] * [strong] incoming edges
    let private getEdgeSources(graph:DsGraph, target:Vertex, bStartEdge:bool) =
        let edges = graph.GetIncomingEdges(target) |> List.ofSeq
        let mask  = if bStartEdge then EdgeType.Start else EdgeType.Reset

        let srcsWeek   = edges |> filter(fun e -> e.EdgeType = mask )
        let srcsStrong = edges |> filter(fun e -> e.EdgeType = (mask ||| EdgeType.Strong))

        match srcsWeek.Any(), srcsStrong.Any() with
        | true, true -> failwithlog "Error Week and Strong can't connenct same node target"
        | true, false -> srcsWeek   |> map (fun e->e.Source) |> DuEssWeak
        | false, true -> srcsStrong |> map (fun e->e.Source) |> DuEssStrong
        | false, false -> DuEssNone

     /// returns Weak outgoing edges
    let private getWeakEdgeTargets(graph:DsGraph, source:Vertex, bStartEdge:bool) =
        let edges = graph.GetOutgoingEdges(source) |> List.ofSeq
        let mask  = if bStartEdge then EdgeType.Start else EdgeType.Reset

        let srcsWeek   = edges |> filter(fun e -> e.EdgeType = mask )
        srcsWeek |> map (fun e->e.Target)

    /// returns [weak] start incoming/outgoing edges for target
    let getStartWeakEdgeSources(target:VertexManager) =
        match getEdgeSources (target.Vertex.Parent.GetGraph(), target.Vertex, true) with
        | DuEssWeak ws when ws.Any() -> ws
        | _ -> []
    /// returns [strong] start incoming/outgoing edges for target
    let getStartStrongEdgeSources(target:VertexManager) =
        match getEdgeSources (target.Vertex.Parent.GetGraph(), target.Vertex, true) with
        | DuEssStrong ss when ss.Any() -> ss
        | _ -> []
    /// returns [weak] reset incoming/outgoing edges for target
    let getResetWeakEdgeSources(target:VertexManager) =
        match getEdgeSources (target.Vertex.Parent.GetGraph(), target.Vertex, false) with
        | DuEssWeak wr when wr.Any() -> wr
        | _ -> []
    /// returns [strong] reset incoming/outgoing edges for target
    let getResetStrongEdgeSources(target:VertexManager) =
        match getEdgeSources (target.Vertex.Parent.GetGraph(), target.Vertex, false) with
        | DuEssStrong sr when sr.Any() -> sr
        | _ -> []

    /// returns  reset outgoing edges for target
    let getResetWeakEdgeTargets(source:VertexManager) =
        getWeakEdgeTargets (source.Vertex.Parent.GetGraph(), source.Vertex, false) 
    /// returns  Start outgoing edges for target
    let getStartWeakEdgeTargets(source:VertexManager) =
        getWeakEdgeTargets (source.Vertex.Parent.GetGraph(), source.Vertex, true) 


    ///// 원위치 고려했을 때, reset chain 중 하나라도 켜져 있는지 검사하는 expression 반환
    let private getNeedCheckTasks(real:Real) =
        let origins      = real.V.OriginInfo.Tasks
        let resetChains  = real.V.OriginInfo.ResetChains

        (* [ KeyValuePair(JogDef, InitialType) ] *)
        let needChecks = origins.Where(fun (_, init)-> init = NeedCheck)

        let needCheckSet:TaskDev list list =
            let apiNameToInTagMap =
                needChecks.Where(fun (task, _) -> task.ApiItem.RXs.any())
                          .Map(fun   (task, _) -> task.ApiName, task)
                |> Tuple.toDictionary
            [
                if apiNameToInTagMap.Any() then
                    (*
                        apiNameToInTagMap: [ "A.+" => "A.+.I"; ... ]        // name => tag
                        r: "A.+"
                        rs ["A.+"; "A.-"]
                        resetChains = [ ["A.+"; "A.-"]; ]
                     *)
                    for rs in resetChains do
                        [
                            for r in rs do
                                apiNameToInTagMap.TryFind(r).Map(fun intag -> intag)
                        ] |> List.choose id
            ] |> List.filter List.any

        needCheckSet

    let getNeedCheckIOs(real:Real, bSim:bool) =
        let taskMaps = getNeedCheckTasks(real)
        let sets =
            if bSim
            then
                [for is in taskMaps do
                    let is = is.Select(fun f->f.ApiItem.PE)
                    [
                        for i in is do i.Expr <&&> !!(is.Except([i]).ToOr())   // --| |--|/|--|/|--
                    ].ToOr()
                ]
            else
                [for is in taskMaps do
                    let is = is.Select(fun f->f.InTag :?> Tag<bool>)
                    [
                        for i in is do i.Expr <&&> !!(is.Except([i]).ToOr())   // --| |--|/|--|/|--
                    ].ToOr()
                ]
                //각 리셋체인 단위로 하나라도 켜있으면 됨
            //         resetChain1         resetChain2       ...
            //      --| |--|/|--|/|--------| |--|/|--|/|--   ...
            //      --|/|--| |--|/|--    --|/|--| |--|/|--
            //      --|/|--|/|--| |--    --|/|--|/|--| |--
        if sets.Any()
        then sets.ToAnd()
        else real._on.Expr



    [<AutoOpen>]
    [<Extension>]
    type CodeConvertUtilExt =
        [<Extension>] static member STs (FList(vms:VertexManager list)): PlanVar<bool> list = vms |> map (fun vm -> vm.ST)
        [<Extension>] static member SFs (FList(vms:VertexManager list)): PlanVar<bool> list = vms |> map (fun vm -> vm.SF)
        [<Extension>] static member RTs (FList(vms:VertexManager list)): PlanVar<bool> list = vms |> map (fun vm -> vm.RT)
        [<Extension>] static member ETs (FList(vms:VertexManager list)): PlanVar<bool> list = vms |> map (fun vm -> vm.ET)
        [<Extension>] static member ERRs(FList(vms:VertexManager list)): PlanVar<bool> list = vms |> bind(fun vm -> [vm.E1; vm.E2])

        [<Extension>] static member ToAndElseOn(ts:#TypedValueStorage<bool> seq, sys:DsSystem) = if ts.Any() then ts.ToAnd() else sys._on.Expr
        [<Extension>] static member ToAndElseOff(ts:#TypedValueStorage<bool> seq, sys:DsSystem) = if ts.Any() then ts.ToAnd() else sys._off.Expr
        [<Extension>] static member ToOrElseOn(ts:#TypedValueStorage<bool> seq, sys:DsSystem) = if ts.Any() then ts.ToOr()  else sys._on.Expr
        [<Extension>] static member ToOrElseOff(ts:#TypedValueStorage<bool> seq, sys:DsSystem) = if ts.Any() then ts.ToOr()  else sys._off.Expr
        [<Extension>] static member GetSharedReal(v:VertexManager) = v |> getSharedReal
        [<Extension>] static member GetSharedCall(v:VertexManager) = v |> getSharedCall
        ///Real 자신이거나 RealEx Target Real
        [<Extension>] static member GetPureReal  (v:VertexManager) = v.Vertex |> getPureReal
        [<Extension>] static member GetPureCall  (v:VertexManager) = v.Vertex |> getPureCall
        [<Extension>] static member GetPure      (v:VertexManager) = v.Vertex |> getPure

        [<Extension>]
        static member GetStartCausals(xs:Vertex seq, usingRoot:bool) =
                xs.Select(fun f->
                match f with
                | :? Real    as r  -> r.V.ET
                | :? RealExF as rf -> rf.Real.V.ET
                | :? CallSys as rs -> rs.V.ET
                | :? CallDev as c  -> c.V.ET
                | :? Alias   as a  -> if usingRoot then getPure(a.V.Vertex).V.ET else a.V.ET
                | _ -> failwithlog $"Error {getFuncName()}"
                ).Distinct()
        //리셋 원인
        [<Extension>]
        static member GetResetCausals(xs:Vertex seq) =
                xs.Select(fun f ->
                    match f with
                    | :? Real    as r  -> r.V.G
                    | :? RealExF as rf -> rf.V.G
                    | :? Alias   as a  -> a.GetPure().V.G
                    | _ -> failwithlog $"Error {getFuncName()}"
                ).Distinct()
   

        [<Extension>]
        static member GetResetStrongCausals(xs:Vertex seq) =
                xs.Select(fun f ->
                    match f with
                    | :? Real    as r  -> r.V.G
                    | :? RealExF as rf -> rf.Real.V.G
                    | :? Alias   as a  -> a.V.G
                    | :? CallSys as cs  -> cs.V.G
                    | _ -> failwithlog $"Error {getFuncName()}"
                ).Distinct()

        [<Extension>]
        static member GetResetStrongCausalReadys(xs:Vertex seq) =
                xs.Select(fun f ->
                    match f with
                    | :? Real    as r  -> r.V.R
                    | :? RealExF as rf -> rf.Real.V.R
                    | :? Alias   as a  -> a.V.R
                    | _ -> failwithlog $"Error {getFuncName()}"
                ).Distinct()

        [<Extension>]
        static member GetWeakStartRootAndCausals  (v:VertexManager) =
            let tags = getStartWeakEdgeSources(v).GetStartCausals(true)
            tags.ToAndElseOff(v.System)

        [<Extension>]
        static member GetWeakStartDAGAndCausals  (v:VertexManager) =
            let tags = getStartWeakEdgeSources(v).GetStartCausals(false)
            tags.ToAndElseOff(v.System)

        [<Extension>]
        static member GetWeakResetRootAndCausals  (v:VertexManager) =
            let tags = getResetWeakEdgeSources(v).GetResetCausals()
            tags.ToAndElseOff(v.System)

        [<Extension>]
        static member GetStrongStartRootAndCausals  (v:VertexManager) =
            let tags = getStartStrongEdgeSources(v).GetStartCausals(true)
            tags.ToAndElseOff(v.System)

        [<Extension>]
        static member GetStrongResetRootAndCausals  (v:VertexManager) =
            let tags = getResetStrongEdgeSources(v).GetResetStrongCausals()
            tags.ToAndElseOff(v.System)

        [<Extension>]
        static member GetStrongResetRootAndReadys  (v:VertexManager) =
            let tags = getResetStrongEdgeSources(v).GetResetStrongCausalReadys()
            tags.ToOrElseOn(v.System)
