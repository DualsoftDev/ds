namespace T

open NUnit.Framework
open Dual.Common.Core.FS
open Dual.Common.UnitTest.FS
open Engine.Common
open Engine.Core


[<AutoOpen>]
module GrapSortTests =
    type V(name:string) =
        inherit Named(name)
    type E(source, target) =
        inherit EdgeBase<V>(source, target, EdgeType.Start)

    type TopologicalSortTest() =
        inherit EngineTestBaseClass()

        //https://miro.medium.com/v2/resize:fit:1100/format:webp/1*Xj-MeUJBmRe5YvIXT05VAA.png
        [<Test>]
        member __.``TopSortTest1`` () =

            let vs = ['A'..'G'] |> List.map toString |> List.map (fun n -> n, V $"{n}") |> Tuple.toDictionary
            let es0 = [
                E(vs["A"], vs["B"])
                E(vs["B"], vs["D"])
                E(vs["D"], vs["F"])

                E(vs["A"], vs["C"])
                E(vs["C"], vs["E"])

                E(vs["G"], vs["E"])
                E(vs["G"], vs["F"])

                E(vs["B"], vs["C"])
            ]
            let g = Graph<V, E>(vs.Values, es0)
            let sorted = GraphSortImpl.topologicalSort g
            sorted |> SeqEq [vs["A"]; vs["G"]; vs["B"]; vs["D"]; vs["C"]; vs["F"]; vs["E"] ]
            ()

        //https://joshhug.gitbooks.io/hug61b/content/chap21/21.1.1.jpg
        [<Test>]
        member __.``TopSortTest2`` () =

            let vs = ['A'..'F'] |> List.map toString |> List.map (fun n -> n, V $"{n}") |> Tuple.toDictionary
            let es0 = [
                E(vs["D"], vs["B"])
                E(vs["B"], vs["A"])
                E(vs["A"], vs["F"])

                E(vs["D"], vs["C"])
                E(vs["E"], vs["C"])
                E(vs["E"], vs["F"])

            ]
            let g = Graph<V, E>(vs.Values, es0)
            let sorted = GraphSortImpl.topologicalSort g
            sorted |> SeqEq [vs["D"]; vs["E"]; vs["B"]; vs["C"]; vs["A"]; vs["F"]; ]
            ()


        //https://assets.leetcode.com/users/images/63bd7ad6-403c-42f1-b8bb-2ea41e42af9a_1613794080.8115625.png
        [<Test>]
        member __.``TopSortTest3`` () =

            let vs = [1..6] |> List.map toString |> List.map (fun n -> n, V $"{n}") |> Tuple.toDictionary
            let es0 = [
                E(vs["1"], vs["2"])
                E(vs["2"], vs["3"])
                E(vs["1"], vs["4"])
                E(vs["4"], vs["5"])
                E(vs["5"], vs["6"])
                E(vs["4"], vs["6"])
                E(vs["4"], vs["2"])
            ]
            let g = Graph<V, E>(vs.Values, es0)
            let sorted = GraphSortImpl.topologicalSort g
            sorted |> SeqEq [vs["1"]; vs["4"]; vs["5"]; vs["2"]; vs["6"]; vs["3"]; ]


        [<Test>]
        member __.``TopSortTestCyclic`` () =

            let vs = ['A'..'C'] |> List.map toString |> List.map (fun n -> n, V $"{n}") |> Tuple.toDictionary
            let es0 = [
                E(vs["A"], vs["B"])
                E(vs["B"], vs["C"])
                E(vs["C"], vs["A"])
            ]
            let g = Graph<V, E>(vs.Values, es0)
            let sorted = GraphSortImpl.topologicalSort g
            sorted |> SeqEq []
            ()

