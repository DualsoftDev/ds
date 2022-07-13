namespace UnitTest.Engine


open Xunit
open System
open FsUnit.Xunit
open Engine
open Engine.Core
open System.Linq
open Dual.Common

[<AutoOpen>]
module ModelTests =
    type DemoTests() =
        let sysP = """
[sys] P = {
    [flow] F = {
        Vp > Pp > Sp;
        Vm > Pm > Sm;

        Pp |> Sm;
        Pm |> Sp;
        Vp <||> Vm;
    }
}
"""

        let seqEq(a, b) = Enumerable.SequenceEqual(a, b) |> ShouldBeTrue

        interface IClassFixture<Fixtures.DemoFixture>

        [<Fact>]
        member __.``Parse Cylinder`` () =
            let text = sysP + """
[cpu] Cpu = {
    P.F;
}
"""

            let engine = new Engine(text, "Cpu")
            let system = engine.Model.Systems |> Seq.exactlyOne
            let cpu = engine.Cpu
            cpu.Name === "Cpu"
            engine.FakeCpu |> ShouldBeNull
            system.Name === "P"
            let flow = system.RootFlows |> Seq.exactlyOne
            flow.Name === "F"
            let children = flow.Children |> Enumerable.OfType<Segment>
            let childrenNames = children |> Seq.map(fun (seg:Segment) -> seg.Name)
            (childrenNames, ["Vp"; "Pp"; "Sp"; "Vm"; "Pm"; "Sm"]) |> seqEq
            children |> Seq.forall(fun seg -> isNull seg.ChildFlow) |> ShouldBeTrue

            flow.Cpu === cpu
            let rootFlow = cpu.RootFlows |> Seq.exactlyOne
            flow === rootFlow


        [<Fact>]
        member __.``Parse Task`` () =
            let text = sysP + """
[sys] L = {
    [task] T = {
        Cp = {P.F.Vp ~ P.F.Sp}
        Cm = {P.F.Vm ~ P.F.Sm}
        C00 = { _ ~ _ }
        C01 = { _ ~ P.F.Sm }
        C02 = { _ ~ P.F.Sp, P.F.Sm}
        C10 = {P.F.Vm ~ _ }
        C20 = {P.F.Vp, P.F.Vm ~ _ }
        C21 = {P.F.Vp, P.F.Vm ~ P.F.Sm }
        C22 = {P.F.Vp, P.F.Vm ~ P.F.Sp, P.F.Sm }
    }
    [flow] F = {
        Main = { T.Cp > T.Cm; }
    }
}

[cpu] Cpu = {
    L.F;
}"""
            let engine = new Engine(text, "Cpu")
            let system = engine.Model.Systems |> Seq.find(fun s -> s.Name = "L")
            let cpu = engine.Cpu
            let task = system.Tasks |> Seq.exactlyOne
            let callProtos = task.CallPrototypes |> Seq.map(fun c -> c.Name, c) |> Tuple.toDictionary
            (callProtos.Keys, ["Cp"; "Cm"; "C00"; "C01"; "C02"; "C10"; "C20"; "C21"; "C22"; ])
            |> seqEq

            let checkC22 =
                let c22 = callProtos.["C22"]
                let txs = c22.TXs.OfType<Segment>()
                (txs |> Seq.map(fun tx -> tx.Name), ["Vp"; "Vm"]) |> seqEq

            cpu.Name === "Cpu"
            engine.FakeCpu |> ShouldBeNull
            system.Name === "P"
            let flow = system.RootFlows |> Seq.exactlyOne
            flow.Name === "F"
            let children = flow.Children |> Enumerable.OfType<Segment>
            let childrenNames = children |> Seq.map(fun (seg:Segment) -> seg.Name)
            (childrenNames, ["Vp"; "Pp"; "Sp"; "Vm"; "Pm"; "Sm"]) |> Enumerable.SequenceEqual |> ShouldBeTrue
            children |> Seq.forall(fun seg -> isNull seg.ChildFlow) |> ShouldBeTrue

            flow.Cpu === cpu
            let rootFlow = cpu.RootFlows |> Seq.exactlyOne
            flow === rootFlow

            ()
