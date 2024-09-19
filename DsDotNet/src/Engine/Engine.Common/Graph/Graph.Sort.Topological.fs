namespace Engine.Common

open System.Collections.Generic
open Dual.Common.Core.FS
open System.Linq
open GraphUtilImpl

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

    
    let topologicalGroupSort (graph:Graph<_, _>) =
        let vertices = topologicalSort graph

        let graphOrder = GraphPairwiseOrderImpl.isAncestorDescendant (graph, EdgeType.Start)
         

        let dicVs = Dictionary<int, ResizeArray<IVertex>>()
        let addedV = ResizeArray<IVertex>()
        //하나 항목 추가
        let addSeq (iSEQ: int) (v: IVertex) =
            if not (addedV.Contains v) then
                addedV.Add(v)
                match dicVs.TryGetValue(iSEQ) with
                | true, seq -> seq.Add v
                | false, _ -> dicVs.Add(iSEQ, ResizeArray [v])
                true
            else false

        //currV 기준으로 자식들 항목 추가
        let addMultiSeq iSEQ currV= 
            let notAddeds = vertices.Where(fun f-> not <| addedV.Contains(f))
            let outgoingVs = graph.GetOutgoingVertices(currV)

            let multiSeqVs = 
                let validoutgoingVs = 
                    outgoingVs |> Seq.filter (fun o -> 
                                not <| notAddeds.Except([o])
                                                .any(fun notAdded -> backwardExist o notAdded graphOrder)
                                ) //자신 o  뒤에 notAdded하나라도 존재하면 Skip

                validoutgoingVs
                |> Seq.filter (fun o ->
                    let targetIncomes = graph.GetIncomingVertices(o)
                    if targetIncomes.length() = 1 
                    then true
                    else 
                        targetIncomes //currV 조건 앞에 income 있으면 추가 불가
                        |> Seq.filter (fun income -> income <> currV)
                        |> Seq.exists (fun income -> forwardExist currV income graphOrder)
                        |> not
                )
        
            if multiSeqVs.length() > 1 
            then
                multiSeqVs 
                |> Seq.map (fun outV -> addSeq iSEQ outV) |> Seq.toArray  |> Seq.contains(true)
            else 
                false


        let mutable iSEQ = 1
        let increaseSeqNum() = iSEQ <- iSEQ + 1

        graph.Inits |> Seq.iter (fun vertex -> addSeq 0 vertex |> ignore)

        vertices |> Seq.iter (fun currV ->
            if addSeq iSEQ currV then increaseSeqNum()
            if addMultiSeq iSEQ currV then increaseSeqNum())

        dicVs