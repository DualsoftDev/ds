namespace Engine.Common

open System.Collections.Generic
open Dual.Common.Core.FS
open System.Linq

module internal GraphUtilImpl =

    /// Get ordered routes from start to end  //origin 신규 함수 완전 적용 후 추후 삭제
    /// source = target 같을 경우 유효 판단
    //let visitFromSourceToTarget (now:'V) (target:'V) (graph:Graph<_, _>) =
    //    let rec searchNodes
    //        (now:'V) (target:'V)
    //        (graph:Graph<_, _>) (path:'V list)
    //      = [
    //            let nowPath = path.Append(now) |> List.ofSeq
    //            if now <> target then
    //                for node in graph.GetOutgoingVertices(now) do
    //                    yield! searchNodes node target graph nowPath
    //            else
    //                yield nowPath
    //        ]
    //    searchNodes now target graph []


    let forwardExist (source:'V) (target:'V) (graphOrder:'V->'V->bool option) =
        (graphOrder source target) |> fun f-> f.IsSome && f.Value

    let backwardExist (source:'V) (target:'V) (graphOrder:'V->'V->bool option) =
        (graphOrder source target) |> fun f-> f.IsSome && not(f.Value)

    let directionNotExist (source:'V) (target:'V) (graphOrder:'V->'V->bool option) =
        (graphOrder source target) |> fun f-> f.IsNone

    let findHeadVertex (xs: 'V seq) (graphOrder: 'V -> 'V -> bool option) : 'V option=
        if xs.isEmpty() then None
        else
            let mutable head = xs.Head()
            xs |> Seq.iter (fun x ->
                if forwardExist x head graphOrder then
                    head <- x
                )
            Some head
