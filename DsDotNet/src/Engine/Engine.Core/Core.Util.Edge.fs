namespace Engine.Core

open System.Linq
open System.Runtime.CompilerServices
open System.Collections.Generic
open Engine.Common.FS


[<AutoOpen>]
module internal EdgeModule =
    let getEdgeType(operator:string) =
        match operator with
        | TextSEdge -> EdgeType.Default //">"  
        | TextSPush -> EdgeType.Strong  //">>" 
        | TextREdge -> EdgeType.Reset   //"|>"  
        | TextRPush -> EdgeType.Reset  ||| EdgeType.Strong //"||>" 

        | TextSEdgeRev -> EdgeType.Reversed //"<"   
        | TextSPushRev -> EdgeType.Reversed ||| EdgeType.Strong //"<<"  
        | TextREdgeRev -> EdgeType.Reversed ||| EdgeType.Reset //"<|" 
        | TextRPushRev -> EdgeType.Reversed ||| EdgeType.Reset  ||| EdgeType.Strong //"<||" 

        | TextInterlock -> EdgeType.Reset  ||| EdgeType.Bidirectional ||| EdgeType.Strong //"<||>" 
        | TextInterlockWeak -> EdgeType.Reset  ||| EdgeType.Bidirectional //"<|>"
        //| TextSReset   "=>" 후행리셋 처리필요
        //| TextSResetRev"<="
        | _ -> failwith "ERROR"

    let getEdgeText(edgeType:EdgeType) =
        if(  edgeType.HasFlag(EdgeType.Default))                        then TextSEdge 
        elif(edgeType.HasFlag(EdgeType.Reversed))                       then TextSEdgeRev
        elif(edgeType.HasFlag(EdgeType.Strong))                         then TextSPush 
        elif(edgeType.HasFlag(EdgeType.Reversed ||| EdgeType.Strong))   then TextSPushRev
        elif(edgeType.HasFlag(EdgeType.Reset))                          then TextREdge 
        elif(edgeType.HasFlag(EdgeType.Reset ||| EdgeType.Reversed))    then TextREdgeRev 
        elif(edgeType.HasFlag(EdgeType.Reset ||| EdgeType.Strong))      then TextRPush
        elif(edgeType.HasFlag(EdgeType.Reversed ||| EdgeType.Reset ||| EdgeType.Strong))       then TextRPushRev
        elif(edgeType.HasFlag(EdgeType.Reset ||| EdgeType.Bidirectional ||| EdgeType.Strong )) then TextInterlock
        elif(edgeType.HasFlag(EdgeType.Reset ||| EdgeType.Bidirectional))                      then TextInterlockWeak
        else failwith "ERROR"

    let createFlowEdges(flow:Flow, source:SegmentBase, target:SegmentBase, operator:string) =
        let eType = getEdgeType(operator)
        [|
            if eType.HasFlag(EdgeType.Bidirectional) then
                let single = eType &&& (~~~ EdgeType.Bidirectional)
                yield InFlowEdge.Create(flow, source, target, single)
                yield InFlowEdge.Create(flow, target, source, single)
            else
                yield InFlowEdge.Create(flow, source, target, eType)
        |]

    let createChildEdges(segment:Segment, source:Child, target:Child, operator:string) =
        let eType = getEdgeType(operator)
        [|
            if eType.HasFlag(EdgeType.Bidirectional) then
                let single = eType &&& (~~~ EdgeType.Bidirectional)
                yield InSegmentEdge.Create(segment, source, target, single)
                yield InSegmentEdge.Create(segment, target, source, single)
            else
                yield InSegmentEdge.Create(segment, source, target, eType)
        |]

    /// 상호 reset 정보(Mutual Reset Info) 확장
    let createMRIEdgesTransitiveClosure(flow:Flow) =    // graph:Graph<'V, 'E>
        // todo: system 의 flow 에 대해서 MRI 를 갖는 real 들의 MRI edge 생성
        // https://www.tutorialspoint.com/Transitive-closure-of-a-Graph
        (*
            Begin
               copy the adjacency matrix into another matrix named transMat
               for any vertex k in the graph, do
                  for each vertex i in the graph, do
                     for each vertex j in the graph, do
                        transMat[i, j] := transMat[i, j] OR (transMat[i, k]) AND transMat[k, j])
                     done
                  done
               done
               Display the transMat
            End
        *)
        let es =
            flow.Graph.Edges
                .OfType<EdgeBase<'V>>()
                .Where(fun e -> e.EdgeType.HasFlag(EdgeType.Strong ||| EdgeType.Reset))
                .ToArray()

        let gr = Graph(Seq.empty, es)
        let vs = gr.Vertices.ToArray()
        let dic = Dictionary<'V*'V, bool>()
        for i in vs do
            for j in vs do
            if i <> j then
                dic.Add((i, j), false)
        for e in es do
            dic[(e.Source, e.Target)] <- true


        for i in vs do
            for j in vs do
            for k in vs do
            if i <> j then
                dic[(i, j)] <- dic[(i, j)] || (i <> k && dic[(i, k)] && dic[(i, k)])

        for KeyValue( (i, j), v) in dic do
            // i -> j 의 reset edge 가 존재하고, j -> i 로도 reset edge 가 존재해야 하지만, 실제 j -> i reset edge 가 없는 경우
            if v && dic[(j, i)] && flow.Graph.FindEdges(j, i).isNullOrEmpty() then
                InFlowEdge.Create(flow, j, i, EdgeType.Reset ||| EdgeType.Strong ||| EdgeType.AugmentedTransitiveClosure) |> ignore
        ()
    let createMRIEdgesTransitiveClosure4System(system:DsSystem) =
        for f in system.Flows do
            createMRIEdgesTransitiveClosure f

[<Extension>]
type EdgeHelper =
    [<Extension>] static member CreateEdges(flow:Flow, source:SegmentBase, target:SegmentBase, operator:string) =
                    createFlowEdges(flow, source, target, operator)
    [<Extension>] static member CreateEdges(segment:Segment, source:Child, target:Child, operator:string) =
                    createChildEdges(segment, source, target, operator)

    [<Extension>] static member GetEdgeType(edgeCausal:EdgeCausal) =
                    getEdgeType(edgeCausal.ToText())
    [<Extension>] static member GetEdgeCausal(edgeType:EdgeType) =
                    getEdgeText(edgeType) |> EdgeCausalType

    [<Extension>] static member CreateMRIEdgesTransitiveClosure(model:Model) =
                    for sys in model.Systems do
                        createMRIEdgesTransitiveClosure4System sys
