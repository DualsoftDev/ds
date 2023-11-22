namespace Engine.Core

open System.Collections.Generic
open Dual.Common.Core.FS

module internal GraphSortImpl =
    let topologicalSort(x:Graph<_, _>) =
        let inDegree = new Dictionary<'V, int>(x.Vertices.Count)
        let queue = new Queue<'V>()

        // 각 vertex incoming degree 설정
        for vertex in x.Vertices do
            inDegree.[vertex] <- 0

        // edge 고려해서 incoming degree 수정
        for edge in x.Edges do
            inDegree.[edge.Target] <- inDegree.[edge.Target] + 1

        // init vertices -> enqueu
        for vertex in x.Inits do
            if inDegree.[vertex] = 0 then
                queue.Enqueue(vertex)

        // do topological sort
        let mutable result = []
        while queue.Count > 0 do
            let vertex = queue.Dequeue()
            result <- vertex :: result
            for neighbor in x.GetOutgoingVertices(vertex) do
                inDegree.[neighbor] <- inDegree.[neighbor] - 1
                if inDegree.[neighbor] = 0 then
                    queue.Enqueue(neighbor)

        if result.Length = x.Vertices.Count then
            List.rev result |> toArray
        else
            [||]
