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

    let forwardExist (source:'V) (target:'V) (graph:Graph<_, _>) =
        visitFromSourceToTarget source target graph |> Seq.any

    let backwardExist (source:'V) (target:'V) (graph:Graph<_, _>) =
        visitFromSourceToTarget target source graph |> Seq.any