namespace UnitTest.Mockup.Engine


open Xunit
open Engine.Core
open Dual.Common
open Xunit.Abstractions
open System.Threading
open UnitTest.Engine

[<AutoOpen>]
module VirtualParentTestTest =
    type Tests1(output1:ITestOutputHelper) =
        interface IClassFixture<Fixtures.DemoFixture>

        [<Fact>]
        member __.``Vps 생성 test`` () =
            let cpu = new MuCpu("dummy")
            let b = MuSegment.Create(cpu, "B")
            let g = MuSegment.Create(cpu, "G")
            let r = MuSegment.Create(cpu, "R")
            let auto = new Tag(cpu, null, "auto")

            let vpB = Vps.Create(b, auto, [g], [g; r])
            Global.Logger.Debug($"B Start:{vpB.PortS.ToText()}");
            Global.Logger.Debug($"B Reset:{vpB.PortR.ToText()}");
            Global.Logger.Debug($"B End:{vpB.PortE.ToText()}");
            let vpG = Vps.Create(g, auto, [r], [b])
            let vpR = Vps.Create(r, auto, [b], [g])
            ()
