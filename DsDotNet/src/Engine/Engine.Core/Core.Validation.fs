namespace Engine.Core

open System.Linq
open System.Runtime.CompilerServices
open Dual.Common.Core.FS


[<AutoOpen>]
module ValidateMoudle =
    


    let private validateChildrenVertexType (mei:ModelingEdgeInfo<Vertex>) =
        let invalidEdge =  (mei.Sources @ mei.Targets).OfType<Alias>()
                             .Where(fun a->a.TargetWrapper.RealTarget().IsSome
                                        || a.TargetWrapper.RealExFlowTarget().IsSome)

        if invalidEdge.any() then failwith $"Vertex {invalidEdge.First().Name} children type error"

    let private validateEdge(graph:Graph<Vertex, Edge>, bRoot:bool) =
        graph.Edges
            .Where(fun e -> e.EdgeType.HasFlag(EdgeType.Reset))
            .Iter(fun edge ->
                [edge.Source ;edge.Target].Iter(fun v->
                    match getPure v with 
                    | :? Real    -> ()
                    | :? RealExF -> ()
                    | _ -> failwithlog $"Reset 연결은 Work 타입에만 연결가능합니다. \t[{edge.Source.Name} |> {edge.Target.Name}]"
                    //| _ -> failwithlog $"ResetEdge can only be used on Type Work \t[{edge.Source.Name} |> {edge.Target.Name}]"
            ))

        if bRoot  
        then
            graph.Edges
                .Where(fun e -> e.EdgeType.HasFlag(EdgeType.Start))
                .Iter(fun edge ->
                    match getPure edge.Target with 
                    | :? Real    -> ()
                    | :? RealExF -> ()
                    | _ -> failwithlog $"Action 시작 연결은 Work 내에서만 가능합니다. Work-Action 그룹작업이 필요합니다. [{edge.Source.Name} > {edge.Target.Name}]"
                    //| _ -> failwithlog $"The 'Action' start command must occur within the 'Work'. ((Work-Action Group work is required.))[{edge.Source.Name} > {edge.Target.Name}]"
                )

    let validateSystemEdge(system:DsSystem) =
         for f in system.Flows do
            validateEdge (f.Graph ,true)
            f.Graph.Vertices.OfType<Real>().Iter(fun r -> 
                    validateEdge (r.Graph, false)
                )

    let validateGraphOfSystem(system:DsSystem) =
        validateSystemEdge system
        for f in system.Flows do
            f.Graph.ValidateCylce(true) |> ignore    //flow는 사이클 허용
            f.Graph.Vertices.OfType<Real>().Iter(fun r -> 
                r.Graph.ValidateCylce(false) |> ignore //real는 사이클 허용 X
                )

    let guardedValidateSystem(system:DsSystem) =
        try validateGraphOfSystem system
        with exn ->
            logWarn $"%A{exn}"

  
    let validateParentOfEdgeVertices (mei:ModelingEdgeInfo<Vertex>) (parent:FqdnObject) =
        let invalid = (mei.Sources @ mei.Targets).Select(fun v -> v.Parent.GetCore()).TryFind(fun c -> c <> parent)
        match invalid with
        | Some v -> failwith $"Vertex {v.Name} is not child of flow {parent.Name}"
        | None -> ()


    type DsSystem with
        member x.ValidateGraph() = validateGraphOfSystem x
                

[<Extension>]
type ValidateExt =
    [<Extension>] static member ValidateGraph(x:DsSystem)   = validateGraphOfSystem x

