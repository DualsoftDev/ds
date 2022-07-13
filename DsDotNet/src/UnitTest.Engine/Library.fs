namespace UnitTest.Engine


open Xunit
open System
open FsUnit.Xunit
open Engine
open Engine.Core
open System.Linq

[<AutoOpen>]
module ModelTests =
    type DemoTests() =
        interface IClassFixture<Fixtures.DemoFixture>

        [<Fact>]
        member __.``Parse Cylinder`` () =
            let text = """
[sys] P = {
    [flow] F = {
        Vp > Pp > Sp;
        Vm > Pm > Sm;

        Pp |> Sm;
        Pm |> Sp;
        Vp <||> Vm;
    }
}
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
            (childrenNames, ["Vp"; "Pp"; "Sp"; "Vm"; "Pm"; "Sm"]) |> Enumerable.SequenceEqual |> ShouldBeTrue
            children |> Seq.forall(fun seg -> seg.ChildFlow = null) |> ShouldBeTrue

            flow.Cpu === cpu
            let rootFlow = cpu.RootFlows |> Seq.exactlyOne
            flow === rootFlow

            engine.Run();
            //var model = ModelParser.ParseFromString(text);
            //foreach (var cpu in model.Cpus)
            //    cpu.Run();

