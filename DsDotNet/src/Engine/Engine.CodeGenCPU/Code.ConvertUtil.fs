namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open System
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
            | :? RealEx as re -> re.Real
            | :? Alias  as a  ->
                match a.TargetWrapper.GetTarget() with
                | :? Real as real -> real
                | _ -> failwithlog "Error GetPureReal"
            |_ -> failwithlog "Error GetPureReal"


        //let origins, resetChains = OriginHelper.GetOriginsWithJobDefs real.Graph
        //origins
        //    .Where(fun w-> w.Value = initialType)
        //    .Select(fun s-> s.Key)


    let getOriginJobDefs(real:Real, initialType:InitialType) =
        let origins, resetChains = OriginHelper.GetOriginsWithJobDefs real.Graph
        [ for w in origins do
            if w.Value = initialType then
                yield w.Key ]

    let getOriginIOs(real:Real, initialType:InitialType) =
        let origins = getOriginJobDefs(real, initialType)
        origins.Select(fun jd -> jd.InTag).Cast<PlcTag<bool>>()

    let getStartPointExpr(call:Call, jd:JobDef) =
        match call.Parent.GetCore() with
        | :? Real as r ->
                let ons = getOriginJobDefs (r, InitialType.On)
                if ons.Contains(jd)
                    then r.V.RO.Expr <&&> call._on.Expr
                    else call._off.Expr
        | _ -> call._off.Expr

    let getEdgeSources(graph:DsGraph, target:Vertex, bStartEdge:bool) =
        let edges = graph.GetIncomingEdges(target)
        let srcsWeek   =
            if bStartEdge
            then edges.Where(fun e -> e.EdgeType = EdgeType.Start )
            else edges.Where(fun e -> e.EdgeType = EdgeType.Reset )

        let srcsStrong =
            if bStartEdge
            then edges.Where(fun e -> e.EdgeType = (EdgeType.Start &&& EdgeType.Strong))
            else edges.Where(fun e -> e.EdgeType = (EdgeType.Reset &&& EdgeType.Strong))

        if srcsWeek.Any() && srcsStrong.Any()
            then failwithlog "Error Week and Strong can't connenct same node target"

        srcsWeek.Select(fun e->e.Source), srcsStrong.Select(fun e->e.Source)

    let getNeedCheck(real:Real) =
        let origins, resetChains = OriginHelper.GetOriginsWithJobDefs real.Graph
        let needChecks = origins.Where(fun w-> w.Value = NeedCheck)

        let needCheckSet:PlcTag<bool> list list =
            let apiNameToInTagMap =
                needChecks.Map(fun (KeyValue(k, v)) -> k.ApiName, k.InTag)
                |> Tuple.toDictionary
            [
                if apiNameToInTagMap.Any() then
                    (*
                        apiNameToInTagMap: [ "A.+" => "A.+.I"; ... ]
                        r: "A.+"
                        rs ["A.+"; "A.-"]
                        resetChains = [ ["A.+"; "A.-"]; ]
                     *)
                    for rs in resetChains do
                        [
                            for r in rs do
                                apiNameToInTagMap.TryFind(r).Map(fun intag -> intag :?> PlcTag<bool>)
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

    //let rec getCoinTags(v:Vertex, isInTag:bool) : Tag<bool> seq =
    //        match v with
    //        | :? Call as c ->
    //            [ for j in c.CallTargetJob.JobDefs do
    //                let typ = if isInTag then "I" else "O"
    //                PlcTag( $"{j.ApiName}_{typ}", "", false) :> Tag<bool>
    //            ]
    //        | :? Alias as a ->
    //            match a.TargetWrapper with
    //            | DuAliasTargetReal ar    -> getCoinTags( ar, isInTag)
    //            | DuAliasTargetCall ac    -> getCoinTags( ac, isInTag)
    //            | DuAliasTargetRealEx ao  -> getCoinTags( ao, isInTag)
    //        | _ -> failwithlog "Error"

    //let getTxTags(c:Call) : DsTag<bool> seq = c.CallTargetJob.JobDefs.Select(fun j-> j.ApiItem.TX)

    [<AutoOpen>]
    [<Extension>]
    type CodeConvertUtilExt =
        [<Extension>] static member STs (FList(vms:VertexManager list)): DsBit list = vms |> map (fun vm -> vm.RT)
        [<Extension>] static member SFs (FList(vms:VertexManager list)): DsBit list = vms |> map (fun vm -> vm.SF)
        [<Extension>] static member RTs (FList(vms:VertexManager list)): DsBit list = vms |> map (fun vm -> vm.RT)
        [<Extension>] static member ETs (FList(vms:VertexManager list)): DsBit list = vms |> map (fun vm -> vm.ET)
        [<Extension>] static member ERRs(FList(vms:VertexManager list)): DsBit list = vms |> bind(fun vm -> [vm.E1; vm.E2])
        [<Extension>] static member CRs (FList(vms:VertexMCoin list))  : DsBit list = vms |> map (fun vm -> vm.CR)

        [<Extension>] static member ToAndElseOn(ts:#Tag<bool> seq, sys:DsSystem) = if ts.Any() then ts.ToAnd() else sys._on.Expr
        [<Extension>] static member ToOrElseOff(ts:#Tag<bool> seq, sys:DsSystem) = if ts.Any() then ts.ToOr()  else sys._off.Expr
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
                | :? RealEx as re -> re.Real.V.EP
                | :? Call   as c  -> if usingRoot then  c.V.ET else  c.V.CR
                | :? Alias  as a  -> if usingRoot then  a.V.ET else  a.V.CR
                | _ -> failwithlog "Error"
                )

            tags.ToAndElseOn(s)
