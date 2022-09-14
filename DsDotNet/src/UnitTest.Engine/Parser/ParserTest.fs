#nowarn "0988"  //: 프로그램의 주 모듈이 비어 있습니다. 프로그램 실행 시 아무 작업도 수행되지 않습니다.
namespace UnitTest.Engine


open Engine.Common.FS
open Engine.Parser
open System
open NUnit.Framework

[<AutoOpen>]
module ParserTestModule =
    type ParserTest() =
        do Fixtures.SetUpTest()

        [<Test>]
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

            ()

