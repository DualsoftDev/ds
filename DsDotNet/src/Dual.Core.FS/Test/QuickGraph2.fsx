
module File1


open QuickGraph
open QuickGraph.Graphviz
open QuickGraph.Graphviz.Dot

type IMyVertex =
    abstract ToText : unit -> string
type MyVertex(s:string) = 
    member x.ToText() = s
    interface IMyVertex with
        override x.ToText() = s
    override x.ToString() = x.ToText()

type MyExtVertex(s:string) =
    inherit MyVertex(s)

type MyEdge<'TVertex>(s1, s2) =
    inherit Edge<'TVertex>(s1, s2)

type MyExtEdge<'TVertex>(s1, s2) =
    inherit MyEdge<'TVertex>(s1, s2)

// https://stackoverflow.com/questions/703871/quickgraph-dijkstra-example

let nodes =
    ['A'..'Z']
    |> List.map (fun ch ->
        let n = ch.ToString()
        if n > "F" then
            MyExtVertex(n) :> MyVertex
        else
            MyVertex(n))
let edges =
    nodes
        |> List.pairwise
        |> List.map(fun (n1, n2) ->
            if n1.ToText() > "F" then
                MyExtEdge<MyVertex>(n1, n2) :> MyEdge<MyVertex>
            else
                MyEdge<MyVertex>(n1, n2))



let test1() =
    let ag = edges.ToAdjacencyGraph<MyVertex, MyEdge<MyVertex>>()
    printfn "%s" <| ag.ToGraphviz()
    ag




let test2() =
    let g2 = AdjacencyGraph<MyVertex, MyEdge<MyVertex>>();
    nodes |> List.iter(fun n -> g2.AddVertex(n) |> ignore)
    edges |> List.iter(fun e -> g2.AddEdge(e) |> ignore)
    printfn "%s" <| g2.ToGraphviz()
    g2