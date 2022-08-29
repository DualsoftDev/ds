open System.Runtime.InteropServices.ComTypes
open System.Collections.Generic

#r "nuget: QuickGraph.NETStandard" 
#I "../../bin/netcoreapp3.1"

//#I @"..\..\packages\YC.QuickGraph.3.7.5-deta\lib\net45"
//#r "YC.QuickGraph.dll"
//#r "YC.QuickGraph.Graphviz.dll"
// #I @"..\..\packages\QuickGraph.3.6.61119.7\lib\net4"
// #r "QuickGraph.dll"
// #r "QuickGraph.Graphviz.dll"
#r "Dual.Core.FS.dll"
#r "Dual.Common.FS.dll"

open System
open QuickGraph
open QuickGraph.Algorithms.Observers
open QuickGraph.Algorithms.Search
open Dual.Common
open QuickGraph.Graphviz
open QuickGraph.Graphviz.Dot

#load "../Graph/Graph.fs"
open Dual.Core.Graph

// https://stackoverflow.com/questions/703871/quickgraph-dijkstra-example
//
// QuickGraph 소스 샘플
// quickgraph\sourceCode\sourceCode\3.0\sources\QuickGraph.Samples
let graph = EdgeListGraph<string, SEdge<string>>();

let nodes = ['A'..'Z'] |> List.map (fun ch -> ch.ToString())
let edges =
    nodes
        |> List.pairwise
        |> List.map(fun (n1, n2) -> SEdge<string>(n1, n2))

let graph = edges.ToAdjacencyGraph<string, SEdge<string>>()
printfn "%s" <| graph.ToGraphviz()



let g2 = AdjacencyGraph<string, MyEdge<string>>();
nodes |> List.iter(fun n -> g2.AddVertex(n) |> ignore)
edges |> List.iter(fun e -> g2.AddEdge(e) |> ignore)
printfn "%s" <| g2.ToGraphviz()


type IPoint = interface end
type Point =
    | Point of float * float
    interface IPoint





let testCase1() =
    let recorder = new EdgeRecorderObserver<int, Edge<int>>();

    let edge12 = new Edge<int>(1, 2);
    let edge32 = new Edge<int>(3, 2);   // Is not reachable
    let graph = new AdjacencyGraph<int, Edge<int>>();

    graph.AddVerticesAndEdgeRange([edge12; edge32]);
    let dfs = new DepthFirstSearchAlgorithm<int, Edge<int>>(graph);

    use bindings = recorder.Attach(dfs)
    dfs.Compute()

    assert(recorder.Edges |> Seq.toList = [edge12]) // // 3 -> 2 is not reachable (wrong orientation)

let testCase2() =

    let edge12 = new Edge<int>(1, 2);
    let edge22 = new Edge<int>(2, 3);
    let edge31 = new Edge<int>(3, 4);
    let edges = [edge12; edge22; edge31]
    let graph = edges.ToAdjacencyGraph()

    let dfs = new DepthFirstSearchAlgorithm<int, Edge<int>>(graph)
    dfs.add_BackEdge          (fun e -> printfn "BackEdge:%A" e)
    dfs.add_DiscoverVertex    (fun v -> printfn "DiscoverVertex:%A" v)
    dfs.add_ExamineEdge       (fun e -> printfn "ExamineEdge:%A" e)
    dfs.add_FinishVertex      (fun v -> printfn "FinishVertex:%A" v)
    dfs.add_ForwardOrCrossEdge(fun e -> printfn "ForwardOrCrossEdge:%A" e)
    dfs.add_InitializeVertex  (fun v -> printfn "InitializeVertex:%A" v)
    dfs.add_StartVertex       (fun v -> printfn "StartVertex:%A" v)
    dfs.add_TreeEdge          (fun e -> printfn "TreeEdge:%A" e)

    let recorder = new EdgeRecorderObserver<int, Edge<int>>();
    use disposable = recorder.Attach(dfs)
    dfs.Compute()

    assert(recorder.Edges |> Seq.toList = edges)

