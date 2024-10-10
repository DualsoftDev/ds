namespace T

open Dual.Common.UnitTest.FS
open Engine.Core
open NUnit.Framework
open System.Text.RegularExpressions
open System
open NUnit.Framework.Legacy

[<AutoOpen>]
module ValueParamTesterModule =

    type ValueParamTester() =

        [<Test>]
        member this.``Test ValueParam ToText with only value``() =
            let vp = ValueParam(Some(box 5), None, None, false, false)
            ClassicAssert.AreEqual("5", vp.ToText())

        [<Test>]
        member this.``Test ValueParam ToText with min and max inclusive``() =
            let vp = ValueParam(None, Some(box 1), Some(box 10), true, true)
            ClassicAssert.AreEqual("1 <= x <= 10", vp.ToText())

        [<Test>]
        member this.``Test ValueParam ToText with min inclusive and max exclusive``() =
            let vp = ValueParam(None, Some(box 1), Some(box 10), true, false)
            ClassicAssert.AreEqual("1 <= x < 10", vp.ToText())

        [<Test>]
        member this.``Test ValueParam ToText with min exclusive and max inclusive``() =
            let vp = ValueParam(None, Some(box 1), Some(box 10), false, true)
            ClassicAssert.AreEqual("1 < x <= 10", vp.ToText())

        [<Test>]
        member this.``Test ValueParam ToText with min exclusive and max exclusive``() =
            let vp = ValueParam(None, Some(box 1), Some(box 10), false, false)
            ClassicAssert.AreEqual("1 < x < 10", vp.ToText())

        [<Test>]
        member this.``Test ValueParam ToText with only min inclusive``() =
            let vp = ValueParam(None, Some(box 1), None, true, false)
            ClassicAssert.AreEqual("1 <= x", vp.ToText())

        [<Test>]
        member this.``Test ValueParam ToText with only min exclusive``() =
            let vp = ValueParam(None, Some(box 1), None, false, false)
            ClassicAssert.AreEqual("1 < x", vp.ToText())
   
        [<Test>]
        member this.``Test ValueParam ToText with only max inclusive``() =
            let vp = ValueParam(None, None, Some(box 10), false, true)
            ClassicAssert.AreEqual("x <= 10", vp.ToText())

        [<Test>]
        member this.``Test ValueParam ToText with only max exclusive``() =
            let vp = ValueParam(None, None, Some(box 10), false, false)
            ClassicAssert.AreEqual("x < 10", vp.ToText())

        [<Test>]
        member this.``Test createValueParam with only value``() =
            let vp = createValueParam("x = 5")
            match vp with
            | Some param -> ClassicAssert.AreEqual("5", param.ToText())
            | None -> Assert.Fail("Failed to create ValueParam")


        [<Test>]
        member this.``Test createValueParam with only text value``() =
            let text = "x = \"A\""
            let vp = createValueParam(text)
            match vp with
            | Some param -> ClassicAssert.AreEqual("\"A\"", param.ToText())
            | None -> Assert.Fail("Failed to create ValueParam")

        [<Test>]
        member this.``Test createValueParam with min and max inclusive``() =
            let vp = createValueParam("1.1f<=x<=10.2f")
            match vp with
            | Some param -> ClassicAssert.AreEqual("1.1f <= x <= 10.2f", param.ToText())
            | None -> Assert.Fail("Failed to create ValueParam")

        [<Test>]
        member this.``Test createValueParam with min inclusive and max exclusive``() =
            let vp = createValueParam("1.0 <=x< 10.1")
            match vp with
            | Some param -> ClassicAssert.AreEqual("1 <= x < 10.1", param.ToText())
            | None -> Assert.Fail("Failed to create ValueParam")

        [<Test>]
        member this.``Test createValueParam with min exclusive and max inclusive``() =
            let vp = createValueParam("1<x<=10")
            match vp with
            | Some param -> ClassicAssert.AreEqual("1 < x <= 10", param.ToText())
            | None -> Assert.Fail("Failed to create ValueParam")

        [<Test>]
        member this.``Test createValueParam with only min inclusive``() =
            let vp = createValueParam("1 <=x")
            match vp with
            | Some param -> ClassicAssert.AreEqual("1 <= x", param.ToText())
            | None -> Assert.Fail("Failed to create ValueParam")

        [<Test>]
        member this.``Test createValueParam with only max exclusive``() =
            let vp = createValueParam("x<10")
            match vp with
            | Some param -> ClassicAssert.AreEqual("x < 10", param.ToText())
            | None -> Assert.Fail("Failed to create ValueParam")
