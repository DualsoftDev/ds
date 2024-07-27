namespace Engine.Core

open System.Linq
open System.Runtime.CompilerServices
open Dual.Common.Core.FS


[<AutoOpen>]
module ValidateMoudle =
    


    let private validateChildrenVertexType (mei:ModelingEdgeInfo<Vertex>) =
        let invalidEdge =
            (mei.Sources @ mei.Targets)
                .OfType<Alias>()
                .Where(fun a->a.TargetWrapper.RealTarget().IsSome)

        if invalidEdge.any() then
            failwith $"Vertex {invalidEdge.First().Name} children type error"

    let private validateEdge(graph:Graph<Vertex, Edge>, bRoot:bool) =
        graph.Edges
            .Where(fun e -> e.EdgeType.HasFlag(EdgeType.Reset))
            .Iter(fun edge ->
                    match getPure edge.Target with 
                    | :? Real    -> ()
                    | _ -> failwithlog $"Reset 연결은 Work 타입에만 연결가능합니다. \t[{edge.Source.Name} |> {edge.Target.Name}]"
                    //| _ -> failwithlog $"ResetEdge can only be used on Type Work \t[{edge.Source.Name} |> {edge.Target.Name}]"
            )

        if bRoot then
            graph.Edges
                .Where(fun e -> e.EdgeType.HasFlag(EdgeType.Start))
                .Iter(fun edge ->
                    match getPure edge.Target with 
                    | :? Real    -> ()
                    | _ -> failwithlog $"Action 시작 연결은 Work 내에서만 가능합니다. Work-Action 그룹작업이 필요합니다. [{edge.Source.Name} > {edge.Target.Name}]"
                    //| _ -> failwithlog $"The 'Action' start command must occur within the 'Work'. ((Work-Action Group work is required.))[{edge.Source.Name} > {edge.Target.Name}]"
                )

    let validateSystemEdge(sys:DsSystem) =
         for f in sys.Flows do
            validateEdge (f.Graph ,true)
            f.Graph.Vertices
                .OfType<Real>()
                .Iter(fun r -> validateEdge (r.Graph, false))

    let validateGraphOfSystem(sys:DsSystem) =
        validateSystemEdge sys
        for f in sys.Flows do
            f.Graph.ValidateCylce(true) |> ignore    //flow는 사이클 허용
            f.Graph.Vertices
                .OfType<Real>()
                .Iter(fun r -> r.Graph.ValidateCylce(false) |> ignore ) //real는 사이클 허용 X

    let guardedValidateSystem(sys:DsSystem) =
        try validateGraphOfSystem sys
        with exn ->
            logWarn $"%A{exn}"


    //하나의 Api는 여러개의 Job에 할당될 수 있다? 없다 ?
    let validateJobs(sys:DsSystem) =
        sys.ApiUsages.Iter(fun a->
            let parentJob = sys.Jobs.Where(fun j-> j.ApiDefs.Contains(a))
            if parentJob.Count() > 1 then 
                let jobNames = StringExt.JoinWith(parentJob.Select(fun j->j.QualifiedName), ", ")
                failwithf $"{a.QualifiedName} is 중복 assigned ({jobNames})"
        )

    let validateRootCallConnection(sys:DsSystem) =
        let rootEdgeSrcs = sys.GetFlowEdges().Select(fun e->e.Source).Distinct()
        sys.GetVerticesCallOperator().Iter(fun callOp->
            if rootEdgeSrcs.Contains (callOp) then
                ()
            else 
                failWithLog $"Flow에 존재하는 Action은 반드시 연결이 필요합니다. {callOp.QualifiedName}"
            )

                