namespace UnitTest.Engine

open System
open Engine.Core
open NUnit.Framework
open Engine.Cpu.Expression

[<AutoOpen>]
module ExpressionTestModule =
    let toString x = x.ToString()
    type ExpressionTest() =
        do Fixtures.SetUpTest()

        [<Test>]
        member __.``ExpressionValueUnit test`` () =

            (value 1).Evaluate()  === 1
            Fun( _add, "+", [1; 2]).Evaluate() |> unbox === 3

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

            concat([
                    (PLCTag("Hello", "Hello, ") |> tag).Evaluate()
                    (PLCTag("World", "world!" ) |> tag).Evaluate()
                ]).Evaluate() === "Hello, world!"

            let tt1 = t1 |> tag
            t1.Value <- 1
            let tt2 = PLCTag("t2", 2) |> tag
            let addTwoExpr = Fun( _add, "+", [ tt1; tt2 ])
            addTwoExpr.Evaluate() === 3
            t1.Value <- 10
            addTwoExpr.Evaluate() === 12



        [<Test>]
        member __.``ExpressionFuncUnit test`` () =
            add([1; 2]).Evaluate() === 3
            sub([5; 3]).Evaluate() === 2
            mul([2; 3]).Evaluate() === 6
            div([3; 2]).Evaluate() === 1.5
            add([1; 2; 3]).Evaluate() === 6
            add([1..10] |> List.map box).Evaluate() === 55
            mul([1..5] |> List.map box).Evaluate() === 120
            sub([10; 1; 2]).Evaluate() === 7
            Math.Abs(addd([1.1; 2.2]).Evaluate() - 3.3) <= 0.00001 |> ShouldBeTrue
            Math.Abs(muld([1.1; 2.0]).Evaluate() - 2.2) <= 0.00001 |> ShouldBeTrue
            concat(["Hello, "; "world!"]).Evaluate() === "Hello, world!"
            mul([2; 3]).Evaluate() === 6
            equal(["Hello"; "world"]).Evaluate() === false
            equal(["Hello"; "Hello"]).Evaluate() === true
            notEqual(["Hello"; "world"]).Evaluate() === true
            notEqual(["Hello"; "Hello"]).Evaluate() === false
            notEqual([1; 2]).Evaluate() === true
            notEqual([2; 2]).Evaluate() === false
            equal([2; 2]) |> resolve === true
            equal([2; 2.0]) |> resolve === true
            equal([2; 2.0f]) |> resolve === true
            equal([6; mul [2; 3]]) |> resolve === true

            gte [2; 3; 5; 5; 1] |> resolve === false
            gte [5; 4; 3; 2; 1] |> resolve === true
            neg [true] |> resolve === false
            neg [false] |> resolve === true
            logicalAnd [true; false] |> resolve === false
            logicalAnd [true; true] |> resolve === true
            logicalAnd [true; true; true; false] |> resolve === false
            logicalOr [true; false] |> resolve === true
            logicalOr [false; false] |> resolve === false
            logicalOr [true; true; true; false] |> resolve === true
            shiftLeft [1; 1] |> resolve === 2
            shiftLeft [1; 1; 1; 1] |> resolve === 8
            shiftLeft [1; 3] |> resolve === 8
            shiftRight [8; 3] |> resolve === 1

            (fun () -> neg [] |> resolve)
                |> ShouldFailWithSubstringT "Wrong number of arguments"
            (fun () -> neg [true; false] |> resolve)
                |> ShouldFailWithSubstringT "Wrong number of arguments"
            (fun () -> add [1] |> resolve)
                |> ShouldFailWithSubstringT "Wrong number of arguments"

        [<Test>]
        member __.``ExpressionComposition test`` () =
            (* 2 * (1+2) * (4+5) = 54 *)
            mul [   2
                    add [1; 2]
                    add [4; 5]
            ] |> resolve === 54

            mul [2; 3; 4] |> resolve === 24

            (*
             (1<<2) * ((8>>3) + 4) * 5
             = 4 * (1+4) * 5
             = 100
            *)
            mul [   shiftLeft [1; 2]   // 4
                    add [   shiftRight [8; 3]  // 1
                            4]
                    5] |> resolve === 100   // 4 * (1+4) * 5



        [<Test>]
        member __.``Statement test`` () =
            let expr = mul [2; 3; 4]
            let target = PLCTag("target", 1)

            let stmt = Assign (expr, target)
            stmt.Do()
            target.Value === 24

            Assign(value 9, target).Do()
            target.Value === 9

            let source = PLCTag("source", 33)
            Assign(tag source, target).Do()
            target.Value === 33

            source.Value <- 44
            target.Value === 33
            Assign(tag source, target).Do()
            target.Value === 44

        [<Test>]
        member __.``Serialization test`` () =
            mul [2; 3; 4] |> toString === "*(2, 3, 4)"

            mul [   2
                    add [1; 2]
                    add [4; 5]
            ] |> toString === "*(2, +(1, 2), +(4, 5))"


            let sTag = PLCTag("address", "value")
            sTag.ToString() === "(address=value)"
            let exprTag = tag sTag
            exprTag.ToString() === "(address=value)"

            (value 1).ToString()  === "1"
            (value "hello").ToString() === "hello"
            (value Math.PI).ToString() === Math.PI.ToString()
            (value true).ToString() === "True"
            (value false).ToString() === "False"
            (value 3.14f).ToString() === "3.14"
            (value 3.14).ToString() === "3.14"


            let expr = mul [2; 3; 4]
            let target = PLCTag("target", 1)
            target.ToString() === "(target=1)"

            let stmt = Assign (expr, target)
            stmt.ToString() === "assign(*(2, 3, 4), (target=1))"
