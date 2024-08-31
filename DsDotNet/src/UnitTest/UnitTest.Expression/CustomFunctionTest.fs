namespace T.Expression
open Dual.Common.UnitTest.FS

open NUnit.Framework
open T

open Engine.Core

//[<AutoOpen>]
//module CustomFunctionTestModule =

    type ExpressionTest() =
        inherit ExpressionTestBaseClass()

        [<Test>]
        member __.``1 Trigonometry test`` () =
            let storages = Storages()
            let trues =
                [
                    "sin(0.0) == 0.0"            // todo: "sin(0) = 0.0f" Not yet!!
                    "sin(toDouble(0)) == 0.0"
                    "toInt(sin(0.0)) == 0"

                    "sin(3.14 / 2.0) >= 0.9999"
                    "sin(3.14 / 2.0) <= 1.0"
                ]
            for t in trues do
                t |> evalExpr storages === true




