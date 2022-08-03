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

            let b, (stB, rtB) = MuSegment.CreateWithDefaultTags(cpu, "B")
            let g, (stG, rtG) = MuSegment.CreateWithDefaultTags(cpu, "G")
            let r, (stR, rtR) = MuSegment.CreateWithDefaultTags(cpu, "R")

            let auto = new Tag(cpu, null, "auto")

            let vpB = Vps.Create(b, auto, (stB, rtB), [g], [g; r])
            Global.Logger.Debug($"B Start:{vpB.PortS.ToText()}");
            Global.Logger.Debug($"B Reset:{vpB.PortR.ToText()}");
            Global.Logger.Debug($"B End:{vpB.PortE.ToText()}");
            let vpG = Vps.Create(g, auto, (stG, rtG), [r], [b])
            let vpR = Vps.Create(r, auto, (stR, rtR), [b], [g])

            //b.PortS.Plan <- stB
            //let xx1 = b.PortS.ToString()
            //let xx2 = b.PortS.ToText()

            //b.PortS.Value === false
            //stB.Value <- true
            //wait()
            //b.PortS.Value === true

            [b :> MuSegmentBase; g; r; vpB; vpG; vpR] |> Seq.iter(fun seg -> seg.WireEvent() |> ignore)


            vpB.PortS.Value === false
            auto.Value <- true
            stB.Value <- true
            wait()
            vpB.PortS.Value === true
            ()
