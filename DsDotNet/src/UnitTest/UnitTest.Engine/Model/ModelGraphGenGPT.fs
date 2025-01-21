namespace T

open System.Linq
open NUnit.Framework
open NUnit.Framework.Legacy

open Dual.Common.Core.FS
open Engine.Core
open Engine.Common


// 테스트용 더미 클래스 정의
type DummyVertex(name:string) =
    inherit Named(name)

type DummyEdge(source, target) =
    inherit DsEdgeBase<DummyVertex>(source, target, EdgeType.Start)

module ModelGraphGenGPT =

    [<TestFixture>]
    type ModelGraphGPT() =

        [<OneTimeSetUp>]
        member x.OneTimeSetUp() = ()
            // OneTimeSetUp 메서드의 내용 추가

        [<Test>]
        member x.``Create Graph and Add Vertices Test`` () =
            let vertices = [DummyVertex("A"); DummyVertex("B"); DummyVertex("C")]
            let edges = [DummyEdge(vertices.[0], vertices.[1]); DummyEdge(vertices.[1], vertices.[2])]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, edges)

            // 그래프에 정상적으로 정점과 간선이 추가되었는지 확인
            ClassicAssert.AreEqual(3, graph.Vertices.Count)
            ClassicAssert.AreEqual(2, graph.Edges.Count)

        [<Test>]
        member x.``Add and Remove Vertices Test`` () =
            let vertices = [DummyVertex("A"); DummyVertex("B"); DummyVertex("C")]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, [])

            // 정점 추가 후 확인
            graph.AddVertex(DummyVertex("D")) |> ignore
            ClassicAssert.AreEqual(4, graph.Vertices.Count)

            // 정점 제거 후 확인
            graph.RemoveVertex(vertices.[0]) |> ignore
            ClassicAssert.AreEqual(3, graph.Vertices.Count)

        [<Test>]
        member x.``Add and Remove Edges Test`` () =
            let vertices = [DummyVertex("A"); DummyVertex("B"); DummyVertex("C")]
            let edges = [DummyEdge(vertices.[0], vertices.[1]); DummyEdge(vertices.[1], vertices.[2])]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, edges)

            // 간선 추가 후 확인
            let newEdge = DummyEdge(vertices.[0], vertices.[2])
            graph.AddEdge(newEdge) |> ignore
            ClassicAssert.AreEqual(3, graph.Edges.Count)

            // 간선 제거 후 확인
            graph.RemoveEdge(edges.[0]) |> ignore
            ClassicAssert.AreEqual(2, graph.Edges.Count)

        [<Test>]
        member x.``Add and Remove Vertices Test 2`` () =
            let vertices = [DummyVertex("A"); DummyVertex("B"); DummyVertex("C")]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, [])

            // 정점 추가 후 확인
            graph.AddVertex(DummyVertex("D")) |> ignore
            graph.AddVertex(DummyVertex("E")) |> ignore
            ClassicAssert.AreEqual(5, graph.Vertices.Count)

            // 정점 제거 후 확인
            graph.RemoveVertex(vertices.[0]) |> ignore
            ClassicAssert.AreEqual(4, graph.Vertices.Count)

        [<Test>]
        member x.``Add and Remove Edges Test 2`` () =
            let vertices = [DummyVertex("A"); DummyVertex("B"); DummyVertex("C")]
            let edges = [DummyEdge(vertices.[0], vertices.[1]); DummyEdge(vertices.[1], vertices.[2])]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, edges)

            // 간선 추가 후 확인
            let newEdge1 = DummyEdge(vertices.[0], vertices.[2])
            let newEdge2 = DummyEdge(vertices.[2], vertices.[0])
            graph.AddEdges([newEdge1; newEdge2]) |> ignore
            ClassicAssert.AreEqual(4, graph.Edges.Count)

            // 간선 제거 후 확인
            graph.RemoveEdges([edges.[0]; edges.[1]]) |> ignore
            ClassicAssert.AreEqual(2, graph.Edges.Count)

        [<Test>]
        member x.``Find Vertex Test`` () =
            let vertices = [DummyVertex("A"); DummyVertex("B"); DummyVertex("C"); DummyVertex("D")]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, [])

            // 정점 찾기 테스트
            let foundVertex = graph.TryFindVertex("B")
            ClassicAssert.IsTrue(foundVertex.IsSome)
            ClassicAssert.AreEqual("B", foundVertex.Value.Name)

            // 존재하지 않는 정점 찾기 테스트
            let notFoundVertex = graph.TryFindVertex("X")
            ClassicAssert.IsTrue(notFoundVertex.IsNone)

        [<Test>]
        member x.``Find Edges Test`` () =
            let vertices = [DummyVertex("A"); DummyVertex("B"); DummyVertex("C")]
            let edges = [DummyEdge(vertices.[0], vertices.[1]); DummyEdge(vertices.[1], vertices.[2])]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, edges)

            // 간선 찾기 테스트
            let foundEdges = graph.FindEdges("A", "B")
            ClassicAssert.AreEqual(1, foundEdges.Count())

            // 존재하지 않는 간선 찾기 테스트
            let notFoundEdges = graph.FindEdges("A", "C")
            ClassicAssert.AreEqual(0, notFoundEdges.Count())

        [<Test>]
        member x.``Connected Vertices Test`` () =
            let vertices = [DummyVertex("A"); DummyVertex("B"); DummyVertex("C"); DummyVertex("D")]
            let edges = [DummyEdge(vertices.[0], vertices.[1]); DummyEdge(vertices.[1], vertices.[2])]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, edges)

            // 연결된 정점 확인
            let connectedVertices = graph.Edges
            ClassicAssert.AreEqual(2, connectedVertices.Count)

        [<Test>]
        member x.``Islands Test`` () =
            let vertices = [DummyVertex("A"); DummyVertex("B"); DummyVertex("C"); DummyVertex("D")]
            let edges = [DummyEdge(vertices.[0], vertices.[1]); DummyEdge(vertices.[1], vertices.[2])]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, edges)

            // 고립된 정점 확인
            let islands = graph.Islands
            ClassicAssert.AreEqual(1, islands.Count())

        [<Test>]
        member x.``Get Incoming Edges Test`` () =
            let vertices = [DummyVertex("A"); DummyVertex("B"); DummyVertex("C")]
            let edges = [DummyEdge(vertices.[0], vertices.[1]); DummyEdge(vertices.[1], vertices.[2])]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, edges)

            // 들어오는 간선 확인
            let incomingEdges = graph.GetIncomingEdges(vertices.[1])
            ClassicAssert.AreEqual(1, incomingEdges.Count())

        [<Test>]
        member x.``Get Outgoing Edges Test`` () =
            let vertices = [DummyVertex("A"); DummyVertex("B"); DummyVertex("C")]
            let edges = [DummyEdge(vertices.[0], vertices.[1]); DummyEdge(vertices.[1], vertices.[2])]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, edges)

            // 나가는 간선 확인
            let outgoingEdges = graph.GetOutgoingEdges(vertices.[1])
            ClassicAssert.AreEqual(1, outgoingEdges.Count())

        [<Test>]
        member x.``Get Edges Test`` () =
            let vertices = [DummyVertex("A"); DummyVertex("B"); DummyVertex("C")]
            let edges = [DummyEdge(vertices.[0], vertices.[1]); DummyEdge(vertices.[1], vertices.[2])]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, edges)

            // 간선 확인
            let allEdges = graph.GetEdges(vertices.[1])
            ClassicAssert.AreEqual(2, allEdges.Count())

        [<Test>]
        member x.``Get Incoming Vertices Test`` () =
            let vertices = [DummyVertex("A"); DummyVertex("B"); DummyVertex("C")]
            let edges = [DummyEdge(vertices.[0], vertices.[1]); DummyEdge(vertices.[1], vertices.[2])]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, edges)

            // 들어오는 정점 확인
            let incomingVertices = graph.GetIncomingVertices(vertices.[1])
            ClassicAssert.AreEqual(1, incomingVertices.Count())

        [<Test>]
        member x.``Get Outgoing Vertices Test`` () =
            let vertices = [DummyVertex("A"); DummyVertex("B"); DummyVertex("C")]
            let edges = [DummyEdge(vertices.[0], vertices.[1]); DummyEdge(vertices.[1], vertices.[2])]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, edges)

            // 나가는 정점 확인
            let outgoingVertices = graph.GetOutgoingVertices(vertices.[1])
            ClassicAssert.AreEqual(1, outgoingVertices.Count())

        [<Test>]
        member x.``Inits Test`` () =
            let vertices = [DummyVertex("A"); DummyVertex("B"); DummyVertex("C")]
            let edges = [DummyEdge(vertices.[0], vertices.[1]); DummyEdge(vertices.[1], vertices.[2])]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, edges)

            // Inits 확인
            let inits = graph.Inits
            ClassicAssert.AreEqual(1, inits.Count())

        [<Test>]
        member x.``Lasts Test`` () =
            let vertices = [DummyVertex("A"); DummyVertex("B"); DummyVertex("C")]
            let edges = [DummyEdge(vertices.[0], vertices.[1]); DummyEdge(vertices.[1], vertices.[2])]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, edges)

            // Lasts 확인
            let lasts = graph.Lasts
            ClassicAssert.AreEqual(1, lasts.Count())




        [<Test>]
        member x.``Add and Remove Multiple Vertices Test`` () =
            let vertices = [for i in 1..100 -> DummyVertex(sprintf "Vertex%d" i)]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, [])

            // 정점 추가 후 확인
            let newVertices = [for i in 101..105 -> DummyVertex(sprintf "Vertex%d" i)]
            graph.AddVertices(newVertices) |> ignore
            ClassicAssert.AreEqual(105, graph.Vertices.Count)

            // 정점 제거 후 확인
            graph.RemoveVertices(newVertices) |> ignore
            ClassicAssert.AreEqual(100, graph.Vertices.Count)

        [<Test>]
        member x.``Add and Remove Multiple Edges Test`` () =
            let vertices = [for i in 1..10 -> DummyVertex(sprintf "Vertex%d" i)]
            let edges = [for i in 1..9 -> DummyEdge(vertices.[i-1], vertices.[i])]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, edges)

            // 간선 추가 후 확인
            let newEdges = [for i in 1..4 -> DummyEdge(vertices.[i], vertices.[i+5])]
            graph.AddEdges(newEdges) |> ignore
            ClassicAssert.AreEqual(13, graph.Edges.Count)

            // 간선 제거 후 확인
            graph.RemoveEdges(newEdges) |> ignore
            ClassicAssert.AreEqual(9, graph.Edges.Count)

        [<Test>]
        member x.``Add and Remove Multiple Vertices and Edges Test`` () =
            let vertices = [for i in 1..10 -> DummyVertex(sprintf "Vertex%d" i)]
            let edges = [for i in 1..9 -> DummyEdge(vertices.[i-1], vertices.[i])]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, edges)

            // 정점과 간선 추가 후 확인
            let newVertices = [for i in 11..15 -> DummyVertex(sprintf "Vertex%d" i)]
            let newEdges = [for i in 1..5 -> DummyEdge(vertices.[i-1], newVertices.[i-1])]
            graph.AddVertices(newVertices) |> ignore
            graph.AddEdges(newEdges) |> ignore
            ClassicAssert.AreEqual(15, graph.Vertices.Count)
            ClassicAssert.AreEqual(14, graph.Edges.Count)

            // 정점과 간선 제거 후 확인
            graph.RemoveEdges(newEdges) |> ignore
            graph.RemoveVertices(newVertices) |> ignore
            ClassicAssert.AreEqual(10, graph.Vertices.Count)
            ClassicAssert.AreEqual(9, graph.Edges.Count)

        [<Test>]
        member x.``TryFindVertex with Type Filter Test`` () =
            let vertices = [DummyVertex("A"); DummyVertex("B"); DummyVertex("C"); DummyVertex("D")]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, [])

            // 특정 타입의 정점 찾기 테스트
            let foundVertex = graph.TryFindVertex<Named>("B")
            ClassicAssert.IsTrue(foundVertex.IsSome)
            ClassicAssert.AreEqual("B", foundVertex.Value.Name)

            // 다른 타입의 정점 찾기 테스트
            let notFoundVertex = graph.TryFindVertex<DsEdgeBase<DummyVertex>>("C")
            ClassicAssert.IsTrue(notFoundVertex.IsNone)

        [<Test>]
        member x.``Find Edges with Source and Target Test`` () =
            let vertices = [DummyVertex("A"); DummyVertex("B"); DummyVertex("C")]
            let edges = [DummyEdge(vertices.[0], vertices.[1]); DummyEdge(vertices.[1], vertices.[2])]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, edges)

            // Source와 Target로 간선 찾기 테스트
            let foundEdges = graph.FindEdges(vertices.[0], vertices.[1])
            ClassicAssert.AreEqual(1, foundEdges.Count())

            // 존재하지 않는 Source와 Target로 간선 찾기 테스트
            let notFoundEdges = graph.FindEdges(vertices.[0], vertices.[2])
            ClassicAssert.AreEqual(0, notFoundEdges.Count())



        [<Test>]
        member x.``Get Incoming and Outgoing Edges Test`` () =
            let vertices = [DummyVertex("A"); DummyVertex("B"); DummyVertex("C")]
            let edges = [DummyEdge(vertices.[0], vertices.[1]); DummyEdge(vertices.[1], vertices.[2])]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, edges)

            // 들어오는 간선 확인
            let incomingEdges = graph.GetIncomingEdges(vertices.[1])
            ClassicAssert.AreEqual(1, incomingEdges.Count())

            // 나가는 간선 확인
            let outgoingEdges = graph.GetOutgoingEdges(vertices.[1])
            ClassicAssert.AreEqual(1, outgoingEdges.Count())

        [<Test>]
        member x.``Get Incoming and Outgoing Vertices Test`` () =
            let vertices = [DummyVertex("A"); DummyVertex("B"); DummyVertex("C")]
            let edges = [DummyEdge(vertices.[0], vertices.[1]); DummyEdge(vertices.[1], vertices.[2])]
            let graph = TDsGraph<DummyVertex, DummyEdge>(vertices, edges)

            // 들어오는 정점 확인
            let incomingVertices = graph.GetIncomingVertices(vertices.[1])
            ClassicAssert.AreEqual(1, incomingVertices.Count())

            // 나가는 정점 확인
            let outgoingVertices = graph.GetOutgoingVertices(vertices.[1])
            ClassicAssert.AreEqual(1, outgoingVertices.Count())
