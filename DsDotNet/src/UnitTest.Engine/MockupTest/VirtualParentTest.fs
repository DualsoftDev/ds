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
            let b = MuSegment(cpu, "B")
            let g = MuSegment(cpu, "G")
            let r = MuSegment(cpu, "R")
            let auto = new Tag(cpu, null, "auto")

            let vpB = Vps.Create(b, auto, [g; r])
            let xxx = vpB.PortR.ToText();
            let vpG = Vps.Create(g, auto, [b])
            let vpR = Vps.Create(r, auto, [g])
            ()
