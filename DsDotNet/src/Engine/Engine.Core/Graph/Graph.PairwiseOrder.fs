namespace Engine.Core

open System.Collections.Generic
open Dual.Common.Core.FS
open System.Linq

module internal GraphPairwiseOrderImpl =
    let isAncestorDescendant (graph:Graph<'V, 'E>) =
        // ancestor, descendant 관계에 있는 모든 node 쌍들에 대해서 hash 값으로 저정
        let hash = HashSet<'V * 'V>()

        let rec traverse (v:'V) (ancestors:'V list) =
            ancestors |> iter (fun a -> hash.Add(a, v) |> ignore)
            let ancestors = v::ancestors

            graph.GetOutgoingVertices(v)
            |> iter (fun a -> traverse a ancestors)

        let curried (v1:'V) (v2:'V): bool option =
            // hash 값을 참고해서 결과를 반환..   관계가 없으면 None 값으로
            if hash.Contains ((v1, v2)) then
                Some true
            elif hash.Contains ((v2, v1)) then
                Some false
            else
                None
    
        graph.Inits |> iter (fun v -> traverse v [])

        curried    
