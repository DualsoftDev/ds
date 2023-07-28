namespace T


open Engine.Core
open Dual.Common.Core.FS
open NUnit.Framework


[<AutoOpen>]
module ModelGrapTests =
    type V(name:string) =
        inherit Named(name)
    type E(source, target) =
        inherit EdgeBase<V>(source, target, EdgeType.Start)

    type CycleDetectTest() =
        inherit EngineTestBaseClass()

        [<Test>]
        member __.``CycleDetectTest test`` () =

            let vs = [0..10] |> List.map (fun n -> V $"{n}")
            // 0>1>2>3
            let es0 = [
                E(vs[0], vs[1])
                E(vs[1], vs[2])
                E(vs[2], vs[3])
            ]
            let g = Graph<V, E>(vs, es0)
            validateGraph g === true

            // 0>1>2>3; 1>5>6; 1>6
            let es =
                [   E(vs[1], vs[5])
                    E(vs[1], vs[6])
                    E(vs[5], vs[6])
                ]@es0
            let g = Graph<V, E>(vs, es)
            validateGraph g === true

            // 0 > 0 : Self-replexive cycle
            let g = Graph<V, E>(vs, [E(vs[0], vs[0])] )
            (fun () -> validateGraph g |> ignore )  |> ShouldFailWithSubstringT "Cyclic"

            // 0>1>2>3; 2>0
            let es = E(vs[2], vs[0])::es0
            let g = Graph<V, E>(vs, es)
            (fun () -> validateGraph g |> ignore )  |> ShouldFailWithSubstringT "Cyclic"
