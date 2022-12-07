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
            ss.TagSR.Value === true
            ss.TagSG.Value === false
            ss.TagSF.Value === false
            ss.TagSH.Value === false
            ss.Status4 === Status4.Ready


            (* start command ON *)
            ss.TagCS.Value <- true
            // Re-evaluate rungs
            ss.SingleScan()

            ss.TagSR.Value === false
            ss.TagSG.Value === true
            ss.TagSF.Value === false
            ss.TagSH.Value === false

            ss.TagCS.Value === true
            ss.Status4 === Status4.Going

            (* end command ON by external sensor? *)
            ss.TagCE.Value <- true
            ss.SingleScan()

            ss.TagSR.Value === false
            ss.TagSG.Value === false
            ss.TagSF.Value === true
            ss.TagSH.Value === false

            ss.TagCS.Value === true
            ss.TagCE.Value === true
            ss.Status4 === Status4.Finish

            (* turn off start command and turn on reset *)
            ss.TagCS.Value <- false
            ss.TagCR.Value <- true
            ss.SingleScan()

            ss.TagSR.Value === false
            ss.TagSG.Value === false
            ss.TagSF.Value === false
            ss.TagSH.Value === true

            ss.TagCE.Value === true
            ss.TagCR.Value === true
            ss.Status4 === Status4.Homing
