namespace Engine.GraphUtil

open System.Diagnostics
open Engine.Core

[<AutoOpen>]
module GraphUtils =
    [<DebuggerDisplay("{name}")>]
    type DsGraph(graph:Graph<'V, 'E>) =
        /// Get node index map(key:name, value:idx)
        member x.Indices = getIndexedMap graph

        /// Get all routes of child DAGs
        member x.Origins = getOrigins graph
        
        /// Get pre-calculated targets that 
        /// child segments to be 'ON' in progress(Theta)
        //member x.ThetaTargets = getThetaTargets flow