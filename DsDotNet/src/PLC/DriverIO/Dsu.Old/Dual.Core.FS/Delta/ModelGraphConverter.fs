namespace Old.Dual.Core

open System.Runtime.CompilerServices
open System.Collections.Generic
open FSharpPlus
open QuickGraph
open Old.Dual.Common
open Old.Dual.Core
open Old.Dual.Core.QGraph
open Old.Dual.Common.Graph.QuickGraph

[<AutoOpen>]
module ModelGraphConverter =

    let toQgEdges (edges:seq<Edge>) =
        let edges = edges |> List.ofSeq

        /// 모델링된 모든 edge 의 끝단 : Circle or CircleSlot type 의 vertices
        let vs =
            edges
            |> List.collect(fun e -> [e.Source; e.Target])
            |> List.distinct

        /// circle --> QgVertex map
        let circlesQgVetexDic =
            vs
            |> List.ofType<CircleSlot>
            |> List.map(fun v -> v.Circle)
            |> List.distinct
            |> List.map (fun circle -> circle, QgVertex(circle.Name))
            |> Tuple.toDictionary

        /// Slot --> QgVertex map
        let slotsQgVetexDic =
            vs
            |> List.ofType<Slot>
            |> List.distinct
            |> List.map (fun slot -> slot, QgVertex(slot.Name))
            |> Tuple.toDictionary

        let getQgVertex (v:ISlot) =
            match v with
            | :? CircleSlot as cs -> circlesQgVetexDic.[cs.Circle]
            | :? Slot as slot -> slotsQgVetexDic.[slot]
            | _ -> failwith "ERROR"

        /// 모델링된 원래의 vertex --> QVertex map
        let dic : Dictionary<ISlot, QgVertex> =
            [
                for v in vs do
                    match v with
                    | :? CircleSlot as cs -> v, QgVertex(cs.Circle.Name)
                    | _ -> v, QgVertex(v.Name)
            ] |> Tuple.toDictionary

        edges
            |> List.map (fun e ->
                let src = getQgVertex e.Source
                let tgt = getQgVertex e.Target
                QgEdge(src, tgt, Some(e :> IEdge)))

    [<Extension>] // type TaskConverterExt =
    type TaskConverterExt =
        [<Extension>]
        static member ToQgModel(task:Task) =
            let edges = task.Edges |> toQgEdges
            let g = edges.ToAdjacencyGraph()    // full-blown graph

            /// Task 내부의 Circle 끼리의 연결 graph : 외부 입출력과의 연결 부분 제외
            let dag =
                let interCircleEdges =
                    edges
                    |> List.filter(fun e ->
                        let oe = e.OriginalEdge.Value
                        (oe.Source :? CircleSlot) && (oe.Target :? CircleSlot))
                    |> List.cast<IEdge>
                interCircleEdges.ToAdjacencyGraph()
            let dcg = makeDCG dag []

            let model = {
                createDefaultModel() with
                    DAG = dag
                    DCG = dcg
            }
            model
