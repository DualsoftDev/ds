namespace UnitTest.Engine.Expression

open NUnit.Framework

open Engine.Parser.FS
open UnitTest.Engine
open System
open Engine.Core
open Engine.Cpu


[<AutoOpen>]
module CpuTestModule =

    type CpuTest() =
        do Fixtures.SetUpTest()

        [<Test>]
        member __.``1 SegmentStatus initialization test`` () =
            let ss = SegmentStatus("Seg1")

            (* Initial state: Ready *)
            ss.TagS4R.Value === true
            ss.TagS4G.Value === false
            ss.TagS4F.Value === false
            ss.TagS4H.Value === false
            ss.Status4 === Status4.Ready


            (* start command ON *)
            ss.TagCS.Value <- true
            // Re-evaluate rungs
            ss.SingleScan()

            ss.TagS4R.Value === false
            ss.TagS4G.Value === true
            ss.TagS4F.Value === false
            ss.TagS4H.Value === false

            ss.TagCS.Value === true
            ss.Status4 === Status4.Going

            (* end command ON by external sensor? *)
            ss.TagCE.Value <- true
            ss.SingleScan()

            ss.TagS4R.Value === false
            ss.TagS4G.Value === false
            ss.TagS4F.Value === true
            ss.TagS4H.Value === false

            ss.TagCS.Value === true
            ss.TagCE.Value === true
            ss.Status4 === Status4.Finish

            (* turn off start command and turn on reset *)
            ss.TagCS.Value <- false
            ss.TagCR.Value <- true
            ss.SingleScan()

            ss.TagS4R.Value === false
            ss.TagS4G.Value === false
            ss.TagS4F.Value === false
            ss.TagS4H.Value === true

            ss.TagCE.Value === true
            ss.TagCR.Value === true
            ss.Status4 === Status4.Homing
