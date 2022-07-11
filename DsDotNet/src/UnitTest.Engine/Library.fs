namespace UnitTest.Engine


open Xunit
open System
open FsUnit.Xunit
open Engine
open Engine.Core

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

            true |> ShouldBeTrue
            let logger = Global.Logger
            logger.Debug("Good start.")
            let engine = new Engine(text, "Cpu")
            engine.Run();
            //var model = ModelParser.ParseFromString(text);
            //foreach (var cpu in model.Cpus)
            //    cpu.Run();

