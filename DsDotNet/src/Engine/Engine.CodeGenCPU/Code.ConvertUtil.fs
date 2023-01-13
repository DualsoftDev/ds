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
            | :? Call as c  ->  Some (c) 
            | :? Alias as a  ->
                    if a.TargetWrapper.GetTarget() :? Call then 
                        Some (a.TargetWrapper.GetTarget() :?> Call)
                    else None
            |_ -> None
           
        ///Real 자신이거나 RealEx Target Real
    let getPureReal(v:VertexManager)  : Real =
            match v.Vertex with
            | :? Real as r  ->  r
            | :? RealEx as re  -> re.Real
            | :? Alias as a  ->
                    if a.TargetWrapper.GetTarget() :? Real then 
                        a.TargetWrapper.GetTarget() :?> Real
                    else failwith "Error GetPureReal"
            |_ -> failwith "Error GetPureReal"
    
   
        //let origins, resetChains = OriginHelper.GetOriginsWithJobDefs real.Graph
        //origins
        //    .Where(fun w-> w.Value = initialType)
        //    .Select(fun s-> s.Key)
    

    let getOriginJobDefs(real:Real, initialType:InitialType) =
        let origins, resetChains = OriginHelper.GetOriginsWithJobDefs real.Graph
        origins
            .Where(fun w-> w.Value = initialType)
            .Select(fun s-> s.Key)
    
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
        let needCheckSet = 
            resetChains.Select(fun rs-> 
                     rs.SelectMany(fun r-> 
                        needChecks.Where(fun f->f.Key.ApiName = r)
                                 ).Select(fun s-> s.Key.InTag).Cast<PlcTag<bool>>()
                        )
        let sets = 
            needCheckSet 
            |> Seq.filter(fun ils -> ils.Any())
            |> Seq.map(fun ils -> 
                        ils.Select(fun il -> il.Expr 
                                             <&&> !!(ils.Except([il]).ToOr()))
                           .ToOr()
                       ) //각 리셋체인 단위로 하나라도 켜있으면 됨
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
        [<Extension>] static member STs(xs:VertexManager seq): DsBit list = xs.Select(fun s->s.ST) |> Seq.toList 
        [<Extension>] static member SFs(xs:VertexManager seq): DsBit list = xs.Select(fun s->s.SF) |> Seq.toList 
        [<Extension>] static member RTs(xs:VertexManager seq): DsBit list = xs.Select(fun s->s.RT) |> Seq.toList 
        [<Extension>] static member ETs(xs:VertexManager seq): DsBit list = xs.Select(fun s->s.ET) |> Seq.toList 
        [<Extension>] static member ERRs(xs:VertexManager seq):DsBit list = xs |>Seq.collect(fun s-> [s.E1;s.E2]) |> Seq.toList 
        [<Extension>] static member CRs(xs:VertexMCoin seq):   DsBit list = xs.Select(fun s->s.CR) |> Seq.toList 
        [<Extension>] static member EmptyOnElseToAnd(xs:PlcTag<bool> seq, sys:DsSystem) = if xs.Any() then xs.ToAnd() else sys._on.Expr
        [<Extension>] static member EmptyOnElseToAnd(xs:DsBit seq, sys:DsSystem) = if xs.Any() then xs.ToAnd() else sys._on.Expr
        [<Extension>] static member EmptyOnElseToAnd(xs:DsTag<bool> seq, sys:DsSystem) = if xs.Any() then xs.Cast<Tag<bool>>().ToAnd() else sys._on.Expr
        [<Extension>] static member EmptyOffElseToOr(xs:PlcTag<bool> seq, sys:DsSystem) = if xs.Any() then xs.ToOr() else sys._off.Expr
        [<Extension>] static member EmptyOffElseToOr(xs:DsBit seq, sys:DsSystem) = if xs.Any() then xs.ToOr() else sys._off.Expr
        [<Extension>] static member EmptyOffElseToOr(xs:DsTag<bool> seq, sys:DsSystem) = if xs.Any() then xs.Cast<Tag<bool>>().ToOr() else sys._off.Expr
        [<Extension>] static member GetSharedReal(v:VertexManager) = v |> getSharedReal
        [<Extension>] static member GetSharedCall(v:VertexManager) = v |> getSharedCall
        [<Extension>] static member GetPureReal(v:VertexManager) = v |> getPureReal
        [<Extension>] static member GetPureCall(v:VertexManager) = v |> getPureCall
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
        static member GetCausalTags(xs:Vertex seq, s:DsSystem, usingRoot:bool) =
            let tags = 
                xs.Select(fun f->
                match f with
                | :? Real as r -> r.V.EP
                | :? RealEx as re -> re.Real.V.EP
                | :? Call as c  -> if usingRoot then  c.V.ET else  c.V.CR
                | :? Alias as a -> if usingRoot then  a.V.ET else  a.V.CR
                | _ -> failwith "Error"
                )

            tags.EmptyOnElseToAnd(s)
