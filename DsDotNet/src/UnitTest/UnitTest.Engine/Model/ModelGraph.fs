namespace T
open Dual.Common.UnitTest.FS

open System.Linq
open NUnit.Framework

open Dual.Common.Core.FS
open Engine.Core
open Engine.Common
open Engine.Parser.FS

[<AutoOpen>]
module ModelGrapTests =
    type V(name:string) =
        inherit Named(name)
        interface IVertexKey with
            member x.VertexKey with get() = x.Name and set(v) = x.Name <- v

    type E(source, target) =
        inherit DsEdgeBase<V>(source, target, EdgeType.Start)

    type CycleDetectTest() =
        inherit EngineTestBaseClass()
        let systemRepo = ShareableSystemRepository()
        let parseText (systemRepo:ShareableSystemRepository) referenceDir text =
            ModelParser.ParseFromString(text, ParserOptions.Create4Simulation(systemRepo, referenceDir, "ActiveCpuName", None, DuNone))

        [<Test>]
        member __.``Edge test`` () =
            let text = """
[sys] sSYS = {
    [flow] fMES = {
        S201_RBT1 |> BUFFER;
        MES => BUFFER > MES;
    }
}
"""
            let system = parseText systemRepo "" text
            let g = system.Flows.First().Graph
            g.Edges.Any(fun e -> e.Source.Name = "S201_RBT1" && e.Target.Name = "BUFFER" && e.EdgeType = EdgeType.Reset) === true
            g.Edges.Any(fun e -> e.Source.Name = "MES" && e.Target.Name = "BUFFER" && e.EdgeType = EdgeType.Start) === true
            g.Edges.Any(fun e -> e.Source.Name = "BUFFER" && e.Target.Name = "MES" && e.EdgeType = EdgeType.Reset) === true
            g.Edges.Any(fun e -> e.Source.Name = "BUFFER" && e.Target.Name = "MES" && e.EdgeType = EdgeType.Start) === true
            g.Edges.Count === 4

            g.Vertices.Count === 3
            ()


        [<Test>]
        member __.``CycleDetectTest test`` () =

            let vs = [0..10] |> List.map (fun n -> V $"{n}")
            // 0>1>2>3
            let es0 = [
                E(vs[0], vs[1])
                E(vs[1], vs[2])
                E(vs[2], vs[3])
            ]
            let g = TDsGraph<V, E>(vs, es0)
            validateCylce(g, false) === true

            // 0>1>2>3; 1>5>6; 1>6
            let es =
                [   E(vs[1], vs[5])
                    E(vs[1], vs[6])
                    E(vs[5], vs[6])
                ]@es0
            let g = TDsGraph<V, E>(vs, es)
            validateCylce(g, false) === true

            // 0 > 0 : Self-replexive cycle
            let g = TDsGraph<V, E>(vs, [E(vs[0], vs[0])] )
            (fun () -> validateCylce(g, false) |> ignore )  |> ShouldFailWithSubstringT "Cyclic"

            // 0>1>2>3; 2>0
            let es = E(vs[2], vs[0])::es0
            let g = TDsGraph<V, E>(vs, es)
            (fun () -> validateCylce(g, false) |> ignore )  |> ShouldFailWithSubstringT "Cyclic"
