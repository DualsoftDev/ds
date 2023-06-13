namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open Engine.Common.FS

[<AutoOpen>]
module CodeConvertUtil =

        ///Real 자신을 공용으로 사용하는 Vertex들
    let getSharedReal(v:VertexManager) : Vertex seq =
            (v.Vertex :?> Real).GetVertexSharedReal()

        ///CallDev 자신을 공용으로 사용하는 Vertex들
    let getSharedCall(v:VertexManager) : Vertex seq =
            (v.Vertex :?> CallDev).GetVertexSharedCall()

        ///CallDev 자신이거나 Alias Target CallDev
    let getPureCall(v:VertexManager) : CallDev option=
            match v.Vertex with
            | :? CallDev  as c  ->  Some (c)
            | :? Alias as a  ->
                match a.TargetWrapper.GetTarget() with
                | :? CallDev as call -> Some call
                | _ -> None
            |_ -> None

        ///Real 자신이거나 RealEx Target Real
    let getPureReal(v:VertexManager)  : Real =
            match v.Vertex with
            | :? Real   as r  -> r
            | :? RealExF as rf -> rf.Real
            | :? Alias  as a  ->
                match a.TargetWrapper.GetTarget() with
                | :? Real as real -> real
                | :? RealExF as rf -> rf.Real
                | _ -> failwithlog $"Error"
            |_ -> failwithlog $"Error"


    let getOriginIOs(vr:VertexMReal, initialType:InitialType) =
        let origins = vr.OriginInfo.Tasks
        origins
            .Where(fun (_, init) -> init = initialType)
            .Select(fun (task, _) -> task.InTag).Cast<Tag<bool>>()

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
        let srcsStrong = edges |> filter(fun e -> e.EdgeType = (mask &&& EdgeType.Strong))

        match srcsWeek.Any(), srcsStrong.Any() with
        | true, true -> failwithlog "Error Week and Strong can't connenct same node target"
        | true, false -> srcsWeek |> map (fun e->e.Source) |> DuEssWeak
        | false, true -> srcsStrong |> map (fun e->e.Source) |> DuEssStrong
        | false, false -> DuEssNone

    /// returns [weak] start incoming edges for target
    let getStartWeakEdgeSources(graph:DsGraph, target:Vertex) =
        match getEdgeSources (graph, target, true) with
        | DuEssWeak ws when ws.Any() -> ws
        | _ -> []
    /// returns [strong] start incoming edges for target
    let getStartStrongEdgeSources(graph:DsGraph, target:Vertex) =
        match getEdgeSources (graph, target, true) with
        | DuEssStrong ss when ss.Any() -> ss
        | _ -> []
    /// returns [weak] reset incoming edges for target
    let getResetWeakEdgeSources(graph:DsGraph, target:Vertex) =
        match getEdgeSources (graph, target, false) with
        | DuEssWeak wr when wr.Any() -> wr
        | _ -> []
    /// returns [strong] reset incoming edges for target
    let getResetStrongEdgeSources(graph:DsGraph, target:Vertex) =
        match getEdgeSources (graph, target, false) with
        | DuEssStrong sr when sr.Any() -> sr
        | _ -> []

    /// 원위치 고려했을 때, reset chain 중 하나라도 켜져 있는지 검사하는 expression 반환
    let getNeedCheckExpression(real:Real) =
        let origins      = real.V.OriginInfo.Tasks
        let resetChains  = real.V.OriginInfo.ResetChains

        (* [ KeyValuePair(JogDef, InitialType) ] *)
        let needChecks = origins.Where(fun (_, init)-> init = NeedCheck)

        let needCheckSet:Tag<bool> list list =
            let apiNameToInTagMap =
                needChecks.Where(fun (task, _) -> task.ApiItem.RXs.any())
                          .Map(fun   (task, _) -> task.ApiName, task.InTag)
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
                                apiNameToInTagMap.TryFind(r).Map(fun intag -> intag :?> Tag<bool>)
                        ] |> List.choose id
            ] |> List.filter List.any


        let sets:Expression<bool> list = [
            for is in needCheckSet do
                [   for i in is do
                        i.Expr <&&> !!(is.Except([i]).ToOr())   // --| |--|/|--|/|--
                ].ToOr()
        ]   //각 리셋체인 단위로 하나라도 켜있으면 됨
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
        [<Extension>] static member CRs (FList(vms:VertexMCoin list))  : PlanVar<bool> list = vms |> map (fun vm -> vm.CR)

        [<Extension>] static member ToAndElseOn(ts:#TypedValueStorage<bool> seq, sys:DsSystem) = if ts.Any() then ts.ToAnd() else sys._on.Expr
        [<Extension>] static member ToOrElseOff(ts:#TypedValueStorage<bool> seq, sys:DsSystem) = if ts.Any() then ts.ToOr()  else sys._off.Expr
        [<Extension>] static member GetSharedReal(v:VertexManager) = v |> getSharedReal
        [<Extension>] static member GetSharedCall(v:VertexManager) = v |> getSharedCall
        ///Real 자신이거나 RealEx Target Real
        [<Extension>] static member GetPureReal  (v:VertexManager) = v |> getPureReal
        [<Extension>] static member GetPureCall  (v:VertexManager) = v |> getPureCall
        [<Extension>]
        static member GetCausalTags(xs:Vertex seq, s:DsSystem, usingRoot:bool) =
            let tags =
                xs.Select(fun f->
                match f with
                | :? Real    as r  -> r.V.EP
                | :? RealExF as rf -> rf.Real.V.EP
                | :? CallSys as rs -> rs.V.ET
                | :? CallDev as c  -> if usingRoot then  c.V.ET else  c.V.CR
                | :? Alias   as a  -> if usingRoot then  a.V.ET else  a.V.CR
                | _ -> failwithlog $"Error {getFuncName()}"
                )

            if tags.any()
                then tags.ToAnd()
                else s._off.Expr
