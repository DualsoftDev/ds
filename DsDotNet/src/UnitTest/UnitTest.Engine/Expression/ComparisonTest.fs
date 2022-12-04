namespace UnitTest.Engine.Expression

open NUnit.Framework

open Engine.Parser.FS
open UnitTest.Engine

[<AutoOpen>]
module ComparisonTestModule =

    type ExpressionTest() =
        do Fixtures.SetUpTest()

        [<Test>]
        member __.``1 ">" test`` () =
            let trues =
                [
                    "2 > 1"
                    "2.0 > 1.0"
                    "2s > 1s"
                    "2us > 1us"
                    "2y > 1y"
                    "2uy > 1uy"
                    "2L > 1L"
                    "2UL > 1UL"

                    "2 >= 1"
                    "2.0 >= 1.0"
                    "2s >= 1s"
                    "2us >= 1us"
                    "2y >= 1y"
                    "2uy >= 1uy"
                    "2L >= 1L"
                    "2UL >= 1UL"

                    "-5 > -6"
                    "(2 + 3) * 2 > 5"
                    "(2 + 3) * 2 < 20"
                ]
            for t in trues do
                t |> evalExpr === true

            let falses =
                [
                    "2 < 1"
                    "2.0 < 1.0"
                    "2s < 1s"
                    "2us < 1us"
                    "2y < 1y"
                    "2uy < 1uy"
                    "2L < 1L"
                    "2UL < 1UL"

                    "2 <= 1"
                    "2.0 <= 1.0"
                    "2s <= 1s"
                    "2us <= 1us"
                    "2y <= 1y"
                    "2uy <= 1uy"
                    "2L <= 1L"
                    "2UL <= 1UL"

                    "-5 < -6"
                    "(2 + 3) * 2 < 5"
                    "(2 + 3) * 2 > 20"
                ]
            for t in falses do
                t |> evalExpr === false


            let typeMismatches =
                [
                    "2 > 1y"
                    "2.0 > 1"
                ]
            for f in typeMismatches do
                (fun () -> f |> parseExpression |> ignore) |> ShouldFailWithSubstringT "Type mismatch"


        [<Test>]
        member __.``1 "=" test`` () =
            let trues =
                [
                    "1 = 1"
                    "1s = 1s"
                    "1us = 1us"
                    "1y = 1y"
                    "1uy = 1uy"
                    "1L = 1L"
                    "1UL = 1UL"
                    $"{dq}hello{dq} = {dq}hello{dq}"

                    "(1 + 1) * 2 = 4"
                    "equal(1, (2 / 2), (3 / 3), 1)"
                ]
            for t in trues do
                t |> evalExpr === true