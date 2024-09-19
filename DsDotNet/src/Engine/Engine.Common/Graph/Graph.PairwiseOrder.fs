namespace Engine.Common

open System.Collections.Generic
open Dual.Common.Core.FS
open System.Linq

module internal GraphPairwiseOrderImpl =
    //let private isAncestorDescendantNaiveImpl (graph:Graph<'V, 'E>) =
    //    // ancestor, descendant 관계에 있는 모든 node 쌍들에 대해서 hash 값으로 저정
    //    let hash = HashSet<'V * 'V>()

    //    let rec traverse (v:'V) (ancestors:'V list) =
    //        ancestors |> iter (fun a -> hash.Add(a, v) |> ignore)
    //        let ancestors = v::ancestors

    //        graph.GetOutgoingVertices(v)
    //        |> iter (fun a -> traverse a ancestors)

    //    let curried (v1:'V) (v2:'V): bool option =
    //        // hash 값을 참고해서 결과를 반환..   관계가 없으면 None 값으로
    //        if hash.Contains ((v1, v2)) then
    //            Some true
    //        elif hash.Contains ((v2, v1)) then
    //            Some false
    //        else
    //            None
    
    //    graph.Inits |> iter (fun v -> traverse v [])

    //    curried    

    let isAncestorDescendant (graph:TDsGraph<'V, 'E>, edgeType:EdgeType)=
        let vs = graph.Vertices |> indexed |> map (fun (n, v) -> (v, n)) |> dict
        let n = vs.length()
        // ancestor, descendant 관계에 있는 모든 node 쌍들에 대해서 hash 값으로 저정
        let table: bool option array2d = Array2D.create<bool option> n n None

        let visited = HashSet<'V>()
 
        let rec traverse (v:'V) (ancestors:'V list) =
            ancestors |> iter (fun a -> table[vs[a], vs[v]] <- Some true )
            if visited.Contains(v) then
                let nv = vs[v]
                let descendants = table[nv, *]
                for a in ancestors |> map (fun a -> vs[a]) do
                    for d = 0 to n-1 do
                        if descendants.[d] = Some true then
                            table[a, d] <- Some true
            else
                visited.Add(v) |> ignore
                let ancestors = v::ancestors
 
                graph.GetOutgoingVerticesWithEdgeType (v, edgeType)
                |> iter (fun a -> traverse a ancestors)
 

        let curried (v1:'V) (v2:'V): bool option =
            let n1, n2 = vs[v1], vs[v2]
            // hash 값을 참고해서 결과를 반환..   관계가 없으면 None 값으로
            if table[n1, n2] = Some true then
                Some true
            elif table[n2, n1] = Some true then
                Some false
            else
                None
    
        graph.Inits |> iter (fun v -> traverse v [])

        curried    
