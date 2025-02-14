namespace T

open NUnit.Framework
open Dual.Common.Core.FS
open Dual.Common.UnitTest.FS
open Dual.Common.Base.FS

[<AutoOpen>]
module GraphTestModule =
    // 간단한 Vertex와 Edge 정의
    type Vertex(name: string) =
        interface INamed with
            member x.Name with get() = x.Name and set(v) = x.Name <- v
        interface IVertexKey with
            member x.VertexKey with get() = x.Name and set(v) = x.Name <- v
        member val Name = name with get, set

    type SimpleEdge(source: Vertex, target: Vertex, edgeInfo: string) =
        inherit EdgeBase<Vertex, string>(source, target, edgeInfo)
        new (s, d) = SimpleEdge(s, d, $"Edge {s.Name}->{d.Name}")

    [<TestFixture>]
    type GraphTest() =
        [<Test>]
        member x.TestInitsAndLasts() =
            let vs = [0..10] |> map (toString >> Vertex)
            let es = vs |> pairwise |> map (fun (s, d) -> SimpleEdge(s, d))

            let es2 = [
                SimpleEdge(vs[2], vs[4])
                SimpleEdge(vs[5], vs[4])
                SimpleEdge(vs[5], vs[4])    // duplicates: 필터링되서 하나만 들어가야 함
            ]

            // 그래프 생성
            let graph = Graph<Vertex, SimpleEdge>(vs, es @ es2, None)

            // 정점과 엣지 출력
            tracefn "Vertices: %A" (graph.Vertices |> Seq.map (fun v -> v.Name) |> Seq.toList)
            tracefn "Edges: %A" (graph.Edges |> Seq.map (fun e -> e.Source.Name + " -> " + e.Target.Name) |> Seq.toList)
            //Vertices: ["0"; "1"; "2"; "3"; "4"; "5"; "6"; "7"; "8"; "9"; "10"]
            //Edges: ["0 -> 1"; "1 -> 2"; "2 -> 3"; "3 -> 4"; "4 -> 5"; "5 -> 6"; "6 -> 7"; "7 -> 8"; "8 -> 9"; "9 -> 10"; "2 -> 4"; "5 -> 4"]

            tracefn "Init Vertices: %A" (graph.Inits |> Seq.map (fun v -> v.Name) |> Seq.toList)
            tracefn "Last Vertices: %A" (graph.Lasts |> Seq.map (fun v -> v.Name) |> Seq.toList)
            //Init Vertices: ["0"]
            //Last Vertices: ["10"]


            /// vs[4] 로 들어오는 vertices 중에서 이름이 "2" 인 vertices
            let ivs = graph.GetIncomingVerticesWithEdgeFilter(vs[4],
                fun e ->
                    e.GetType() === typeof<SimpleEdge>
                    tracefn $"TypeName: {e.GetType().Name}"         // TypeName: SimpleEdge
                    e.Source.Name = "2").ToArray()
            ivs.Length === 1
            ivs[0] === vs[2]
            ()


        [<Test>]
        member x.TestConnectedComponents() =
            // 6개의 정점 생성
            let v0 = Vertex("zero")
            let v1 = Vertex("one")
            let v2 = Vertex("two")
            let v3 = Vertex("three")
            let v4 = Vertex("four")
            let v5 = Vertex("five")

            // 엣지 생성
            let e0_1 = SimpleEdge(v0, v1, "e0_1")
            let e1_2 = SimpleEdge(v1, v2, "e1_2")
            let e3_4 = SimpleEdge(v3, v4, "e3_4")
            let e4_5 = SimpleEdge(v4, v5, "e4_5")

            // 첫 번째 컴포넌트: v0 -> v1 -> v2
            // 두 번째 컴포넌트: v3 -> v4 -> v5
            let vertices = [v0; v1; v2; v3; v4; v5]
            let edges = [e0_1; e1_2; e3_4; e4_5]

            // 그래프 생성
            let graph = Graph<Vertex, SimpleEdge>(vertices, edges, None)

            // 연결 성분 확인
            let components = graph.GetEdgesOfConnectedComponents()

            // 첫 번째 연결 성분에는 [e0_1, e1_2]가 있어야 함
            let component1 = components.[0]
            let component2 = components.[1]

            // 각각의 컴포넌트에 들어있는 엣지들을 확인
            let component1Edges = component1 |> Array.map (fun e -> (e.Source.Name, e.Target.Name))
            let component2Edges = component2 |> Array.map (fun e -> (e.Source.Name, e.Target.Name))

            // 첫 번째 컴포넌트는 v0 -> v1, v1 -> v2
            let expectedComponent1 = [| ("zero", "one"); ("one", "two") |]

            // 두 번째 컴포넌트는 v3 -> v4, v4 -> v5
            let expectedComponent2 = [| ("three", "four"); ("four", "five") |]

            // 각 컴포넌트의 엣지들이 예상과 일치하는지 검증
            expectedComponent1 |> SeqEq component1Edges
            expectedComponent2 |> SeqEq component2Edges



