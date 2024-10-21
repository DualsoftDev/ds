namespace T

open Dual.Common.UnitTest.FS
open Engine.Core
open NUnit.Framework
open System.Text.RegularExpressions
open System
open NUnit.Framework.Legacy

[<AutoOpen>]
module ValueParamTesterModule =

    // Helper function to create and assert ValueParam
    let createAndAssertValueParam (input: string) (expected: string) =
        let vp = createValueParam(input)
        ClassicAssert.AreEqual(expected, vp.ToText())

    // Define the ValueParamTester class for testing
    type ValueParamTester() =

        [<Test>]
        member this.``Test single value matching positive target``() =
            let vp = ValueParam(Some(box 5), None, None, false, false, false)
            ClassicAssert.AreEqual("5", vp.ToText())

        [<Test>]
        member this.``Test inclusive range positive``() =
            let vp = ValueParam(None, Some(box 1), Some(box 10), false, true, true)
            ClassicAssert.AreEqual("[1, 10]", vp.ToText())

        [<Test>]
        member this.``Test exclusive range positive``() =
            let vp = ValueParam(None, Some(box 1), Some(box 10), false, false, false)
            ClassicAssert.AreEqual("(1, 10)", vp.ToText())

        [<Test>]
        member this.``Test greater than or equal positive``() =
            let vp = ValueParam(None, Some(box 5), None, false, true, false)
            ClassicAssert.AreEqual(">= 5", vp.ToText())

        [<Test>]
        member this.``Test less than or equal positive``() =
            let vp = ValueParam(None, None, Some(box 10), false, false, true)
            ClassicAssert.AreEqual("<= 10", vp.ToText())

        [<Test>]
        member this.``Test greater than positive``() =
            let vp = ValueParam(None, Some(box 5), None, false, false, false)
            ClassicAssert.AreEqual("> 5", vp.ToText())

        [<Test>]
        member this.``Test less than positive``() =
            let vp = ValueParam(None, None, Some(box 10), false, false, false)
            ClassicAssert.AreEqual("< 10", vp.ToText())

        // Negative test cases (opposite forms)

        [<Test>]
        member this.``Test single value matching negative target``() =
            let vp = ValueParam(Some(box 5), None, None, true, false, false)
            ClassicAssert.AreEqual("!5", vp.ToText())

        [<Test>]
        member this.``Test inclusive range negative``() =
            let vp = ValueParam(None, Some(box 1), Some(box 10), true, true, true)
            ClassicAssert.AreEqual("![1, 10]", vp.ToText())

        [<Test>]
        member this.``Test exclusive range negative``() =
            let vp = ValueParam(None, Some(box 1), Some(box 10), true, false, false)
            ClassicAssert.AreEqual("!(1, 10)", vp.ToText())

        [<Test>]
        member this.``Test greater than or equal negative``() =
            let vp = ValueParam(None, Some(box 5), None, true, true, false)
            ClassicAssert.AreEqual("!>= 5", vp.ToText())

        [<Test>]
        member this.``Test less than or equal negative``() =
            let vp = ValueParam(None, None, Some(box 10), true, false, true)
            ClassicAssert.AreEqual("!<= 10", vp.ToText())

        [<Test>]
        member this.``Test greater than negative``() =
            let vp = ValueParam(None, Some(box 5), None, true, false, false)
            ClassicAssert.AreEqual("!> 5", vp.ToText())

        [<Test>]
        member this.``Test less than negative``() =
            let vp = ValueParam(None, None, Some(box 10), true, false, false)
            ClassicAssert.AreEqual("!< 10", vp.ToText())

        // Unit Tests for parsing input and comparing ValueParam
        [<Test>]
        member this.``Test inclusive range parsing``() =
            createAndAssertValueParam "[1, 10]" "[1, 10]"

        [<Test>]
        member this.``Test exclusive range parsing``() =
            createAndAssertValueParam "(1, 10)" "(1, 10)"

        [<Test>]
        member this.``Test inclusive min and exclusive max range parsing``() =
            createAndAssertValueParam "[1, 10)" "[1, 10)"

        [<Test>]
        member this.``Test exclusive min and inclusive max range parsing``() =
            createAndAssertValueParam "(1, 10]" "(1, 10]"

        [<Test>]
        member this.``Test single value parsing``() =
            createAndAssertValueParam "5" "5"

        [<Test>]
        member this.``Test greater than parsing``() =
            createAndAssertValueParam "> 5" "> 5"

        [<Test>]
        member this.``Test greater than or equal parsing``() =
            createAndAssertValueParam ">= 5" ">= 5"

        [<Test>]
        member this.``Test less than parsing``() =
            createAndAssertValueParam "< 10" "< 10"

        [<Test>]
        member this.``Test less than or equal parsing``() =
            createAndAssertValueParam "<= 10" "<= 10"

        [<Test>]
        member this.``Test invalid range input returns None``() =
            let vp = createValueParam("[10, 5]")  // Invalid range where min > max
            Assert.That(vp.IsDefaultValue, Is.True)

        [<Test>]
        member this.``Test invalid type input returns None``() =
            let vp = createValueParam("[5, 'A']")  // Invalid type comparison between int and string
            Assert.That(vp.IsDefaultValue, Is.True)

        [<Test>]
        member this.``Test invalid single value input returns None``() =
            let vp = createValueParam("x = 'invalid'")  // Invalid single value
            Assert.That(vp.IsDefaultValue, Is.True)

        [<Test>]
        member this.``Test range with floating point numbers parsing``() =
            createAndAssertValueParam "[1.1, 10.5]" "[1.1, 10.5]"

        [<Test>]
        member this.``Test greater than with floating point parsing``() =
            createAndAssertValueParam "> 1.5" "> 1.5"

        [<Test>]
        member this.``Test single floating point value parsing``() =
            createAndAssertValueParam "5.0" "5"

        // Exception handling tests

        [<Test>]
        member this.``Test invalid range format returns None``() =
            let vp = createValueParam("[1, 2, 3]")  // Invalid format for range
            Assert.That(vp.IsDefaultValue, Is.True)

        [<Test>]
        member this.``Test empty input returns None``() =
            let vp = createValueParam("")  // Empty input should return None
            Assert.That(vp.IsDefaultValue, Is.True)

       
        [<Test>]
        member this.``Test unsupported operator returns None``() =
            let vp = createValueParam("?? 5")  // Unsupported operator
            Assert.That(vp.IsDefaultValue, Is.True)

        [<Test>]
        member this.``Test value with special characters returns None``() =
            let vp = createValueParam("x = @#$")  // Special characters in value
            Assert.That(vp.IsDefaultValue, Is.True)
