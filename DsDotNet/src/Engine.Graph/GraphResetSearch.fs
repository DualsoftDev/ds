namespace Engine.Graph

open System.Linq
open QuickGraph
open Engine.Core



[<AutoOpen>]
module GraphResetSearch =
    let getAdjacencyGraphFromEdge(e:Edge seq) =
        (edges2QgEdge e) |> GraphExtensions.ToAdjacencyGraph

    let checkSourceInGraph(sourceFlow:Flow) (src:IVertex) =
        let gri =
            let isRootFlow = true
            FsGraphInfo(seq[sourceFlow], isRootFlow)
        let routes =
            getInits(gri.SolidGraph)
            |> Seq.map(fun s ->
                (computeDijkstra gri.SolidGraph s src)
                |> Seq.collect(fun e -> [e.Source; e.Target])
                |> Seq.distinct
            )
            |> Seq.filter(fun r -> (r |> Seq.length) <> 0)
            |> Seq.collect id
        let graphs =
            gri.SolidConnectedComponets
            |> Seq.filter(fun gs -> gs |> Seq.contains(src))
            |> Seq.collect id

        routes, graphs

    let findGraphIncludeSource(rSrc:IVertex) =
        match rSrc with
        | :? SegmentBase as s ->
            checkSourceInGraph s.ContainerFlow rSrc
        | :? Call as c ->
            checkSourceInGraph c.Container rSrc
        | _ ->
            failwith "[error] find source native graph"

    let targetResetMarker(srcs:ITxRx seq) (tgts:ITxRx seq) =
        let sourceSegs = srcs |> Seq.map(fun v -> v:?>IVertex)
        let targetSegs = tgts |> Seq.map(fun v -> v:?>IVertex)

        targetSegs
        |> Seq.map(fun ts ->
            getIncomingEdges (getAdjacencyGraphFromEdge (ts:?>SegmentBase).ContainerFlow.Edges) ts
        )
        |> Seq.map(fun es ->
            es
            |> Seq.filter(fun e ->
                e.OriginalEdge.Operator = "|>" || e.OriginalEdge.Operator = "|>>"
            )
            |> Seq.map(fun e ->
                findGraphIncludeSource e.Source
            )
        )
        |> Seq.collect id
        |> Seq.filter(fun rv ->
            Enumerable.SequenceEqual(Enumerable.Intersect(sourceSegs, fst(rv)), sourceSegs)
        )

    let searchCallTargets(cpts:CallPrototype seq) (tgt:CallPrototype) =
        cpts
        |> Seq.filter(fun src->
            src <> tgt && src.TXs.Count <> 0 && tgt.RXs.Count <> 0
        )
        |> Seq.iter(fun src->
            targetResetMarker src.TXs tgt.RXs
            |> Seq.iter(fun v ->
                printfn
                    "%A strongly resets %A in progress of flow %A"
                    src.Name tgt.Name (snd(v)|>List.ofSeq)
            )
        )

    //let checkCallResetSource(task:DsTask seq) =
    //    task
    //    |> Seq.iter(fun t ->
    //        let cpts = t.CallPrototypes
    //        cpts
    //        |> Seq.iter(fun c ->
    //            searchCallTargets cpts c
    //        )
    //    )