namespace UnitTest.Engine

open System
open Engine.Core
open NUnit.Framework


[<AutoOpen>]
module ExpressionTestModule =
    type ExpressionTest() =
        do Fixtures.SetUpTest()

        [<Test>]
        member __.``ExpressionValueUnit test`` () =

            (value 1).Evaluate()  === 1
            Fun( add, "+", [1; 2]).Evaluate() |> unbox === 3

            (value 1).Evaluate() === 1
            (value "hello").Evaluate() === "hello"
            (value Math.PI).Evaluate() === Math.PI
            (value true).Evaluate() === true
            (value false).Evaluate() === false
            (value 3.14f).Evaluate() === 3.14f
            (value 3.14).Evaluate() === 3.14

        [<Test>]
        member __.``ExpressionTagUnit test`` () =
            let t1 = PLCTag("1", 1)
            (tag t1).Evaluate() === 1
            t1.Value <- 2
            (tag t1).Evaluate() === 2
            (PLCTag("Two", "Two") |> tag).Evaluate() === "Two"

            Fun( concat, "concat", [
                    (PLCTag("Hello", "Hello, ") |> tag).Evaluate()
                    (PLCTag("World", "world!" ) |> tag).Evaluate()
                ]).Evaluate() === "Hello, world!"

            let tt1 = t1 |> tag
            t1.Value <- 1
            let tt2 = PLCTag("t2", 2) |> tag
            let addTwoExpr = Fun( add, "+", [ tt1; tt2 ])
            addTwoExpr.Evaluate() === 3
            t1.Value <- 10
            addTwoExpr.Evaluate() === 12



        [<Test>]
        member __.``ExpressionFuncUnit test`` () =
            Fun( add, "+", [1; 2]).Evaluate() === 3
            Fun( sub, "-", [5; 3]).Evaluate() === 2
            Fun( mul, "*", [2; 3]).Evaluate() === 6
            Fun( div, "/", [3; 2]).Evaluate() === 1.5
            Fun( add, "+", [1; 2; 3]).Evaluate() === 6
            Fun( add, "+", [1..10] |> List.map box).Evaluate() === 55
            Fun( mul, "*", [1..5] |> List.map box).Evaluate() === 120
            Fun( sub, "-", [10; 1; 2]).Evaluate() === 7
            Math.Abs(Fun( addd, "+", [1.1; 2.2]).Evaluate() - 3.3) <= 0.00001 |> ShouldBeTrue
            Math.Abs(Fun( muld, "+", [1.1; 2.0]).Evaluate() - 2.2) <= 0.00001 |> ShouldBeTrue
            Fun( concat, "concat", ["Hello, "; "world!"]).Evaluate() === "Hello, world!"
            Fun( mul, "*", [2; 3]).Evaluate() === 6
            Fun( equal, "=", ["Hello"; "world"]).Evaluate() === false
            Fun( equal, "=", ["Hello"; "Hello"]).Evaluate() === true
            Fun( notEqual, "=", ["Hello"; "world"]).Evaluate() === true
            Fun( notEqual, "=", ["Hello"; "Hello"]).Evaluate() === false
            Fun( notEqual, "=", [1; 2]).Evaluate() === true
            Fun( notEqual, "=", [2; 2]).Evaluate() === false

            Fun( equal, "=", [2; 2]) |> resolve === true
            Fun( equal, "=", [2; 2.0]) |> resolve === true
            Fun( equal, "=", [2; 2.0f]) |> resolve === true
            Fun( equal, "=", [  6
                                Fun( mul, "*", [2; 3]) |> resolve
                             ]) |> resolve === true

            Fun( gte, ">=", [2; 3; 5; 5; 1]) |> resolve === false
            Fun( gte, ">=", [5; 4; 3; 2; 1]) |> resolve === true

            Fun( neg, "!", [true]) |> resolve === false
            Fun( neg, "!", [false]) |> resolve === true
            Fun( logicalAnd, "&", [true; false]) |> resolve === false
            Fun( logicalAnd, "&", [true; true]) |> resolve === true
            Fun( logicalAnd, "&", [true; true; true; false]) |> resolve === false
            Fun( logicalOr, "|", [true; false]) |> resolve === true
            Fun( logicalOr, "|", [false; false]) |> resolve === false
            Fun( logicalOr, "|", [true; true; true; false]) |> resolve === true
            Fun( shiftLeft, "<<", [1; 1]) |> resolve === 2
            Fun( shiftLeft, "<<", [1; 1; 1; 1]) |> resolve === 8
            Fun( shiftLeft, "<<", [1; 3]) |> resolve === 8
            Fun( shiftRight, ">>", [8; 3]) |> resolve === 1

            (fun () -> Fun( neg, "!", []) |> resolve)
                |> ShouldFailWithSubstringT "Wrong number of arguments"
            (fun () -> Fun( neg, "!", [true; false]) |> resolve)
                |> ShouldFailWithSubstringT "Wrong number of arguments"
            (fun () -> Fun( add, "+", [1]) |> resolve)
                |> ShouldFailWithSubstringT "Wrong number of arguments"

        [<Test>]
        member __.``ExpressionComposition test`` () =
            Fun (mul, "*", [
                2
                Fun (add, "+", [1; 2])
                Fun (add, "+", [4; 5])
            ]) |> resolve === 54

            Fun (mul, "*", [2; 3; 4]) |> resolve === 24

            Fun (mul, "*", [
                Fun( shiftLeft, "<<", [1; 2])   // 4
                Fun (add, "+", [
                    Fun( shiftRight, ">>", [8; 3])  // 1
                    4])
                5]) |> resolve === 100   // 4 * (1+4) * 5



        [<Test>]
        member __.``Statement test`` () =
            let expr = Fun (mul, "*", [2; 3; 4])
            let target = PLCTag("target", 1)

            let stmt = Assign (expr, target)
            stmt.Do()
            target.Value === 24
