namespace UnitTest.Engine


open Xunit
open Engine.Common.FS
open Engine.Parser
open Xunit.Abstractions
open System

[<AutoOpen>]
module ParserTestModule =
    type ParserTest(output1:ITestOutputHelper) =

        interface IClassFixture<Fixtures.DemoFixture>

        [<Fact>]
        member __.``Quotation test`` () =
            logInfo "============== Quotation"
            let isValid = ParserExtension.IsValidIdentifier
            let invalidIdentifiers = """
1234
 A
hello world!!
+ABC1234
That'sGood
That is good
RobotWeld_‘G
A.B.C
A/B/C
A+B+C
A-B-C
@A
!A
#A
$A
%A
^A
띄어 쓰기
With"DQuote
With'Quote
"""
            invalidIdentifiers.Split([|'\r'; '\n'|], StringSplitOptions.RemoveEmptyEntries)
            |> Seq.forall (isValid) === false

            let validIdentifiers = """
_
__
_1234
ABCD
변수
PART_AD_ON
"""
            validIdentifiers.Split([|'\r'; '\n'|], StringSplitOptions.RemoveEmptyEntries)
            |> Seq.forall (isValid) === true

