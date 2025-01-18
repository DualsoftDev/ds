namespace T.Expression
open T

open NUnit.Framework

open Dual.Common.UnitTest.FS
open Dual.Common.Core.FS

open Engine.Parser.FS
open Engine.Core


[<AutoOpen>]
module LiteralTestModule =

    type LiteralTestTest() =
        inherit ExpressionTestBaseClass()

        [<Test>]
        member __.``2 Expression literal test`` () =
            let h1:LiteralHolder<_> = {Value=true}
            let h2:LiteralHolder<_> = {Value=true}
            h1 === h2
            let t1 = literal2expr true
            let t2 = literal2expr true
            let str1 = literal2expr "true"
            str1.ToText() === "\"true\""

            t1.ToText() === "true"
            t1.IsEqual t2 === true

            t1.IsEqual str1 === false

            let theTrueExpression = literal2expr true
            let theFalseExpression = literal2expr false

            let xxx = [1..3].OrElse([2..5])
            let yyy = [].OrElse([2..5])
            ()

        [<Test>]
        member __.``Literal test`` () =
            let storages = Storages()
            storages.Add(  "b1", (createVariable "b1" ({Object = true}:BoxedObjectHolder)) None   )
            storages.Add(  "pi", (createVariable "pi" ({Object = 3.14}:BoxedObjectHolder)) None   )
            let tests: (string*obj) array =
                [|
                    ("true", true)
                    ("false", false)
                    ("false && true", false)
                    ("false || true", true)
                    ("2 > 3", false)
                    ("$b1", null)
                    ("$b1 && true", null)
                    ("true && $b1", null)
                    ("false && true && $b1", null)
                    ("2 + 3", 5)
                    //("2.1 + 3.2", 5.3)    : 정밀도 연산에 의해서 false 남
                    ("abs(2.1 + 3.2 - 5.3) < 0.1", true)
                    ("2.1 > 3.2", false)
                    ("2.1 < 3.2", true)
                    ("2.1 + 3.1 < 6.0", true)
                    ("2.1 + $pi < 6.0", null)
                    ($"{dq}hello{dq} + {dq}world{dq}", "helloworld")
                    ($"{dq}hello{dq} + {dq}world{dq} == {dq}helloworld{dq}", true)

                |]

            for (str, answer) in tests do
                str |> parseExpression4UnitTest storages |> tryGetLiteralValue === answer

            ()