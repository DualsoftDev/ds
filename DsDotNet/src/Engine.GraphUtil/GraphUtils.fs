namespace Engine.GraphUtil

open System.Diagnostics
open Engine.Core

[<AutoOpen>]
module GraphUtils =
    type DsGraph(graph:Graph<Child, InSegmentEdge>) =
        /// Get node index map(key:name, value:idx)
        member x.Indices = graph |> getIndexedMap
        
        member x.AllResets = graph |> getAllResets

        /// Get all routes of child DAGs
        member x.Origins = graph |> getOrigins
        
        /// Get pre-calculated targets that 
        /// child segments to be 'ON' in progress(Theta)
        //member x.ThetaTargets = getThetaTargets flow