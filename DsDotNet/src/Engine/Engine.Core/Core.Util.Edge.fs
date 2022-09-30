namespace Engine.Core

open System.Runtime.CompilerServices


[<AutoOpen>]
module internal EdgeModule =
    let getEdgeType(operator:string) =
        match operator with
        | ">"   -> EdgeType.Default
        | ">>"  -> EdgeType.Strong
        | "|>"  -> EdgeType.Reset
        | "||>" -> EdgeType.Reset  ||| EdgeType.Strong

        | "<"   -> EdgeType.Reversed
        | "<<"  -> EdgeType.Reversed ||| EdgeType.Strong
        | "<|"  -> EdgeType.Reversed ||| EdgeType.Reset
        | "<||" -> EdgeType.Reversed ||| EdgeType.Reset  ||| EdgeType.Strong

        | "<|>" -> EdgeType.Reset  ||| EdgeType.Bidirectional
        | "<||>" -> EdgeType.Reset  ||| EdgeType.Strong ||| EdgeType.Bidirectional

        | _ -> failwith "ERROR"

    let createFlowEdges(flow:Flow, source:SegmentBase, target:SegmentBase, operator:string) =
        let eType = getEdgeType(operator)
        [|
            if eType.HasFlag(EdgeType.Bidirectional) then
                let single = eType &&& (~~~ EdgeType.Bidirectional)
                yield InFlowEdge.Create(flow, source, target, single)
                yield InFlowEdge.Create(flow, target, source, single ||| EdgeType.Reversed)
            else
                yield InFlowEdge.Create(flow, source, target, eType)
        |]

    let createChildEdges(segment:Segment, source:Child, target:Child, operator:string) =
        let eType = getEdgeType(operator)
        [|
            if eType.HasFlag(EdgeType.Bidirectional) then
                let single = eType &&& (~~~ EdgeType.Bidirectional)
                yield InSegmentEdge.Create(segment, source, target, single)
                yield InSegmentEdge.Create(segment, target, source, single ||| EdgeType.Reversed)
            else
                yield InSegmentEdge.Create(segment, source, target, eType)
        |]

[<Extension>]
type EdgeHelper =
    [<Extension>] static member CreateEdges(flow:Flow, source:SegmentBase, target:SegmentBase, operator:string) =
                    createFlowEdges(flow, source, target, operator)
    [<Extension>] static member CreateEdges(segment:Segment, source:Child, target:Child, operator:string) =
                    createChildEdges(segment, source, target, operator)




