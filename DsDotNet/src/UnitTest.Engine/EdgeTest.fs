namespace UnitTest.Engine


open Xunit
open Engine
open Engine.Core
open System.Linq
open Dual.Common
open Xunit.Abstractions

[<AutoOpen>]
module EdgeTest =
    type EdgeTests1(output1:ITestOutputHelper) =

        interface IClassFixture<Fixtures.DemoFixture>

        [<Fact>]
        member __.``Parser detail test`` () =
            logInfo "============== Parser detail test"
            let mutable text = """
[sys] L = {
    [task] T = {
        Cp = {P.F.Vp ~ P.F.Sp}
        Cm = {P.F.Vm ~ P.F.Sm}
    }
    [flow] F = {
        Main = { T.Cp |> T.Cm; }
    }
}
"""
            text <- text + sysP + cpus

            let builder = new EngineBuilder(text)
            let model = builder.Model
            let activeCpuName = "Cpu"
            let cpu = model.Cpus.First(fun cpu -> cpu.Name = activeCpuName);
            cpu.ForwardDependancyMap.isNullOrEmpty() |> ShouldBeTrue
            cpu.BackwardDependancyMap |> ShouldBeNull
            cpu.IsActive <- true

            let rootFlow = cpu.RootFlows |> Seq.exactlyOne
            let main = rootFlow.Coins |> Enumerable.OfType<Segment> |> Seq.find(fun seg -> seg.Name = "Main")


            model.BuidGraphInfo();
            builder.InitializeAllFlows()

            let fwd = cpu.ForwardDependancyMap
            let bwd = cpu.BackwardDependancyMap

            // tag 기준으로 해당 port 와 연결되어 있는지 check
            main.TagsStart |> Seq.forall(fun s -> fwd[s].Contains(main.PortS)) |> ShouldBeTrue
            main.TagsReset |> Seq.forall(fun r -> fwd[r].Contains(main.PortR)) |> ShouldBeTrue
            main.TagsEnd   |> Seq.forall(fun e -> bwd[e].Contains(main.PortE)) |> ShouldBeTrue

            // port 기준으로 해당 tag 와 연결되어 있는지 check
            (bwd[main.PortS] |> Enumerable.OfType<Tag>, main.TagsStart) |> seqEq
            (bwd[main.PortR] |> Enumerable.OfType<Tag>, main.TagsReset) |> seqEq
            (fwd[main.PortE] |> Enumerable.OfType<Tag>, main.TagsEnd)   |> seqEq


            // todo : subflow check
            ()
