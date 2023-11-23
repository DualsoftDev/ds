namespace Engine.Core

open System.Collections.Generic
open Dual.Common.Core.FS
open System.Linq

module internal GraphUtilImpl =
    
    /// Get ordered routes from start to end
    /// source = target 같을 경우 유효 판단
    let visitFromSourceToTarget (now:'V) (target:'V) (graph:Graph<_, _>) =
        let rec searchNodes
            (now:'V) (target:'V)
            (graph:Graph<_, _>) (path:'V list)
          = [
                let nowPath = path.Append(now) |> List.ofSeq
                if now <> target then
                    for node in graph.GetOutgoingVertices(now) do
                        yield! searchNodes node target graph nowPath
                else
                    yield nowPath
            ]
        searchNodes now target graph []


    let forwardExist (source:'V) (target:'V) (graphOrder:'V->'V->bool option) =
        (graphOrder source target) |> fun f-> f.IsSome && f.Value

    let backwardExist (source:'V) (target:'V) (graphOrder:'V->'V->bool option) =
        (graphOrder source target) |> fun f-> f.IsSome && not(f.Value)

    let directionNotExist (source:'V) (target:'V) (graphOrder:'V->'V->bool option) =
        (graphOrder source target) |> fun f-> f.IsNone

