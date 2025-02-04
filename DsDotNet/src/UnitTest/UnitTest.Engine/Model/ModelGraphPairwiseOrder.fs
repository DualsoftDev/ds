namespace T

open NUnit.Framework

open Dual.Common.UnitTest.FS
open Dual.Common.Core.FS
open Engine.Core
open Engine.Common


[<AutoOpen>]
module GraphPairwiseOrderTest =
    type V(name:string) =
        inherit Named(name)
        interface IVertexKey with
            member x.VertexKey with get() = x.Name and set(v) = x.Name <- v
    type E(source, target) =
        inherit DsEdgeBase<V>(source, target, EdgeType.Start)

    type TopologicalSortTest() =
        inherit EngineTestBaseClass()

        //https://miro.medium.com/v2/resize:fit:1100/format:webp/1*Xj-MeUJBmRe5YvIXT05VAA.png
        [<Test>]
        member __.``PairwiseOrderingTest1`` () =
            // Edges: (A, B), (B, C), (C, D)
            let vs = ['A'..'D'] |> List.map toString |> List.map (fun n -> n, V $"{n}") |> Tuple.toDictionary
            let es = [
                E(vs["A"], vs["B"])
                E(vs["B"], vs["C"])
                E(vs["C"], vs["D"])
            ]
            let g = TDsGraph(vs.Values, es)
            let isAncestor = g.BuildPairwiseComparer()

            // 관계 검사
            isAncestor vs["A"] vs["B"] === Some true
            isAncestor vs["B"] vs["A"] === Some false
            isAncestor vs["A"] vs["C"] === Some true
            isAncestor vs["C"] vs["A"] === Some false
            isAncestor vs["A"] vs["D"] === Some true
            isAncestor vs["D"] vs["A"] === Some false
            isAncestor vs["B"] vs["C"] === Some true
            isAncestor vs["C"] vs["B"] === Some false
            isAncestor vs["B"] vs["D"] === Some true
            isAncestor vs["D"] vs["B"] === Some false
            isAncestor vs["C"] vs["D"] === Some true
            isAncestor vs["D"] vs["C"] === Some false

        //https://joshhug.gitbooks.io/hug61b/content/chap21/21.1.1.jpg
        [<Test>]
        member __.``PairwiseOrderingTest2`` () =

            let vs = ['A'..'F'] |> List.map toString |> List.map (fun n -> n, V $"{n}") |> Tuple.toDictionary
            let es0 = [
                E(vs["D"], vs["B"])
                E(vs["B"], vs["A"])
                E(vs["A"], vs["F"])

                E(vs["D"], vs["C"])
                E(vs["E"], vs["C"])
                E(vs["E"], vs["F"])

            ]
            let g = TDsGraph<V, E>(vs.Values, es0)
            let isAncestor = g.BuildPairwiseComparer()

            // 순방향 direct edge 검사
            isAncestor vs["D"] vs["B"] === Some true
            isAncestor vs["D"] vs["C"] === Some true
            isAncestor vs["E"] vs["C"] === Some true
            isAncestor vs["E"] vs["F"] === Some true
            isAncestor vs["B"] vs["A"] === Some true
            isAncestor vs["A"] vs["F"] === Some true


            // 역방향 direct edge 검사
            isAncestor vs["B"] vs["D"] === Some false
            isAncestor vs["C"] vs["D"] === Some false
            isAncestor vs["C"] vs["E"] === Some false
            isAncestor vs["F"] vs["E"] === Some false
            isAncestor vs["A"] vs["B"] === Some false
            isAncestor vs["F"] vs["A"] === Some false

            // 순방향 n-degree edge 검사
            isAncestor vs["D"] vs["A"] === Some true
            isAncestor vs["D"] vs["F"] === Some true

            // 순서 무관 검사
            isAncestor vs["E"] vs["A"] === None
            isAncestor vs["C"] vs["A"] === None
            isAncestor vs["B"] vs["C"] === None

        [<Test>]
        member __.``PairwiseOrderingTest3`` () =
            let vs = [0..39] |> List.map (fun n -> V $"{n}") |> indexed |> Tuple.toDictionary
            let es =
                [|  for i in 0..2 do
                        for j in 0..9 do
                            let v1 = vs.[i*10 + j]
                            let nextLine = (i+1)*10
                            for k in 0..9 do
                                let v2 = vs.[nextLine + k]
                                yield E(v1, v2)
                |]

            let g = TDsGraph<V, E>(vs.Values, es)
            let isAncestor = g.BuildPairwiseComparer()

            // 순방향 direct edge 검사
            for i in 0..2 do
                for j in 0..9 do
                    let v1 = vs.[i*10 + j]
                    for n = i+1 to 3 do
                        for k in 0..9 do
                            let v2 = vs.[n*10 + k]
                            isAncestor v1 v2 === Some true

            // 역방향 direct edge 검사
            for i in 0..2 do
                for j in 0..9 do
                    let v1 = vs.[i*10 + j]
                    for n = i+1 to 3 do
                        for k in 0..9 do
                            let v2 = vs.[n*10 + k]
                            isAncestor v2 v1 === Some false

            // 동일 레벨 검사
            for i in 0..2 do
                for j in 0..9 do
                    let v1 = vs.[i*10 + j]
                    for k in 0..9 do
                        let v2 = vs.[i*10 + k]
                        isAncestor v1 v2 === None
                        isAncestor v2 v1 === None
