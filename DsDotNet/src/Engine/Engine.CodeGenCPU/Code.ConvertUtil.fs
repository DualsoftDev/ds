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
                | _ -> failwith "Error GetPureReal"
            |_ -> failwith "Error GetPureReal"


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
                    then r.V.RO.Expr <||> call.System._on.Expr
                    else call.System._off.Expr
        | _ -> call.System._off.Expr

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
        else real.V.System._on.Expr

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
    //        | _ -> failwith "Error"

    //let getTxTags(c:Call) : DsTag<bool> seq = c.CallTargetJob.JobDefs.Select(fun j-> j.ApiItem.TX)

    [<AutoOpen>]
    [<Extension>]
    type CodeConvertUtilExt =
        [<Extension>] static member STs (FList(xs:VertexManager list)): DsBit list = xs |> map(fun s->s.ST)
        [<Extension>] static member SFs (FList(xs:VertexManager list)): DsBit list = xs |> map(fun s->s.SF)
        [<Extension>] static member RTs (FList(xs:VertexManager list)): DsBit list = xs |> map(fun s->s.RT)
        [<Extension>] static member ETs (FList(xs:VertexManager list)): DsBit list = xs |> map(fun s->s.ET)
        [<Extension>] static member ERRs(FList(xs:VertexManager list)): DsBit list = xs |> bind(fun s-> [s.E1;s.E2])
        [<Extension>] static member CRs (FList(xs:VertexMCoin list))  : DsBit list = xs |> map(fun s->s.CR)

        [<Extension>] static member ToAndElseOn(xs:PlcTag<bool> seq, sys:DsSystem) = if xs.Any() then xs.ToAnd() else sys._on.Expr
        [<Extension>] static member ToAndElseOn(xs:DsBit seq,        sys:DsSystem) = if xs.Any() then xs.ToAnd() else sys._on.Expr
        [<Extension>] static member ToAndElseOn(xs:DsTag<bool> seq,  sys:DsSystem) = if xs.Any() then xs.ToAnd() else sys._on.Expr
        [<Extension>] static member ToOrElseOff(xs:PlcTag<bool> seq, sys:DsSystem) = if xs.Any() then xs.ToOr() else sys._off.Expr
        [<Extension>] static member ToOrElseOff(xs:DsBit seq,        sys:DsSystem) = if xs.Any() then xs.ToOr() else sys._off.Expr
        [<Extension>] static member ToOrElseOff(xs:DsTag<bool> seq,  sys:DsSystem) = if xs.Any() then xs.ToOr() else sys._off.Expr
        [<Extension>] static member GetSharedReal(v:VertexManager) = v |> getSharedReal
        [<Extension>] static member GetSharedCall(v:VertexManager) = v |> getSharedCall
        ///Real 자신이거나 RealEx Target Real
        [<Extension>] static member GetPureReal  (v:VertexManager) = v |> getPureReal
        [<Extension>] static member GetPureCall  (v:VertexManager) = v |> getPureCall
        [<Extension>]
        static member FindEdgeSources(graph:DsGraph, target:Vertex, edgeType:ModelingEdgeType): Vertex seq =
            let edges = graph.GetIncomingEdges(target)
            let foundEdges =
                match edgeType with
                | StartPush -> edges.OfNotResetEdge().Where(fun e -> e.EdgeType.HasFlag(EdgeType.Strong))
                | StartEdge -> edges.OfNotResetEdge().Where(fun e -> not <| e.EdgeType.HasFlag(EdgeType.Strong))
                | ResetEdge -> edges.OfWeakResetEdge()
                | ResetPush -> edges.OfStrongResetEdge()
                | ( StartReset | InterlockWeak | Interlock )
                    -> failwith $"Do not use {edgeType} Error"

            foundEdges.Select(fun e->e.Source)

        [<Extension>]
        static member GetCausalTagsExpression(xs:Vertex seq, s:DsSystem, usingRoot:bool) =
            let tags = [
                for f in xs do
                    match f with
                    | :? Real   as r  -> r.V.EP
                    | :? RealEx as re -> re.Real.V.EP
                    | :? Call   as c  -> if usingRoot then  c.V.ET else  c.V.CR
                    | :? Alias  as a  -> if usingRoot then  a.V.ET else  a.V.CR
                    | _ -> failwith "Error"
            ]

            tags.ToAndElseOn(s)
