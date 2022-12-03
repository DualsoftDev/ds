namespace UnitTest.Engine

open NUnit.Framework

open Engine.Parser.FS

[<AutoOpen>]
module ExpressionCustomFunctionTestModule =

    type ExpressionTest() =
        do Fixtures.SetUpTest()

        [<Test>]
        member __.``1 Trigonometry test`` () =
            let trues =
                [
                    "sin(0.0) = 0.0"            // "sin(0) = 0.0f" Not yet!!
                    "sin(Double(0)) = 0.0"
                    "Int(sin(0.0)) = 0"
                ]
            for t in trues do
                t |> evalExpr === true


