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

        ///Call 자신을 공용으로 사용하는 Vertex들
    let getSharedCall(v:VertexManager) : Vertex seq =
            (v.Vertex :?> Call).GetVertexSharedCall()

        ///Call 자신이거나 Alias Target Call
    let getPureCall(v:VertexManager) : Call option=
            match v.Vertex with
            | :? Call  as c  ->  Some (c)
            | :? Alias as a  ->
                match a.TargetWrapper.GetTarget() with
                | :? Call as call -> Some call
                | _ -> None
            |_ -> None

        ///Real 자신이거나 RealEx Target Real
    let getPureReal(v:VertexManager)  : Real =
            match v.Vertex with
            | :? Real   as r  -> r
            | :? RealExF as rf -> rf.Real   //test ahn
            | :? Alias  as a  ->
                match a.TargetWrapper.GetTarget() with
                | :? Real as real -> real
                | _ -> failwithlog $"Error"
            |_ -> failwithlog $"Error"


        //let origins, resetChains = OriginHelper.GetOriginsWithDeviceDefs real.Graph
        //origins
        //    .Where(fun w-> w.Value = initialType)
        //    .Select(fun s-> s.Key)


    let getOriginDeviceDefs(real:Real, initialType:InitialType) =
        let origins, resetChains = OriginHelper.GetOriginsWithDeviceDefs real.Graph
        [ for w in origins do
            if w.Value = initialType then
                yield w.Key ]

    let getOriginIOs(real:Real, initialType:InitialType) =
        let origins = getOriginDeviceDefs(real, initialType)
        origins.Select(fun jd -> jd.InTag).Cast<Tag<bool>>()

    let getStartPointExpr(call:Call, jd:TaskDevice) =
        match call.Parent.GetCore() with
        | :? Real as r ->
                let ons = getOriginDeviceDefs (r, InitialType.On)
                if ons.Contains(jd)
                    then r.V.RO.Expr <&&> call._on.Expr
                    else call._off.Expr
        | _ -> call._off.Expr

    /// returns [week] * [strong] incoming edges
    let private getEdgeSources(graph:DsGraph, target:Vertex, bStartEdge:bool) =
        let edges = graph.GetIncomingEdges(target)
        let mask  = if bStartEdge then EdgeType.Start else EdgeType.Reset

        let srcsWeek   = edges.Where(fun e -> e.EdgeType = mask )
        let srcsStrong = edges.Where(fun e -> e.EdgeType = (mask &&& EdgeType.Strong))

        if srcsWeek.Any() && srcsStrong.Any()
            then failwithlog "Error Week and Strong can't connenct same node target"

        srcsWeek.Select(fun e->e.Source), srcsStrong.Select(fun e->e.Source)
    /// returns [week] * [strong] start incoming edges for target
    let getStartEdgeSources(graph:DsGraph, target:Vertex) = getEdgeSources (graph, target, true)
    /// returns [week] * [strong] reset incoming edges for target
    let getResetEdgeSources(graph:DsGraph, target:Vertex) = getEdgeSources (graph, target, false)

    /// 원위치 고려했을 때, reset chain 중 하나라도 켜져 있는지 검사하는 expression 반환
    let getNeedCheckExpression(real:Real) =
        let origins, resetChains = OriginHelper.GetOriginsWithDeviceDefs real.Graph

        (* [ KeyValuePair(JogDef, InitialType) ] *)
        let needChecks = origins.Where(fun w-> w.Value = NeedCheck)

        let needCheckSet:Tag<bool> list list =
            let apiNameToInTagMap =
                needChecks.Map(fun (KeyValue(taskDevice, v)) -> taskDevice.ApiName, taskDevice.InTag)
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

        if needChecks.Any()
        then sets.ToAnd()
        else real._on.Expr

    [<AutoOpen>]
    [<Extension>]
    type CodeConvertUtilExt =
        [<Extension>] static member STs (FList(vms:VertexManager list)): PlanVar<bool> list = vms |> map (fun vm -> vm.RT)
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
                | :? Real   as r  -> r.V.EP
                | :? RealExF as rf -> rf.Real.V.EP  //test ahn
                | :? RealExS as rs -> rs.V.ET
                | :? Call   as c  -> if usingRoot then  c.V.ET else  c.V.CR
                | :? Alias  as a  -> if usingRoot then  a.V.ET else  a.V.CR
                | _ -> failwithlog $"Error {get_current_function_name()}"
                )

            tags.ToAndElseOn(s)