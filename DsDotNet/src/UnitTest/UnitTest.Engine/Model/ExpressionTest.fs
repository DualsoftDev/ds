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
        
            (Value 1).Evaluate() |> unbox === 1
            Fun( add, "+", [1; 2]).Evaluate() |> unbox === 3

            Value 1 |> resolve === 1
            Value "hello" |> resolve === "hello"
            Value Math.PI |> resolve === Math.PI
            Value true |> resolve === true
            Value false |> resolve === false
            Value 3.14f |> resolve === 3.14f
            Value 3.14 |> resolve === 3.14
            
            
        [<Test>]
        member __.``ExpressionFuncUnit test`` () =
            Fun( add, "+", [1; 2]) |> resolve === 3
            Fun( sub, "-", [5; 3]) |> resolve === 2
            Fun( mul, "*", [2; 3]) |> resolve === 6
            Fun( div, "/", [3; 2]) |> resolve === 1.5
            Fun( add, "+", [1; 2; 3]) |> resolve === 6
            Fun( add, "+", [1..10] |> List.map box) |> resolve === 55
            Fun( mul, "*", [1..5] |> List.map box) |> resolve === 120
            Fun( sub, "-", [10; 1; 2]) |> resolve === 7
            Fun( addd, "+", [1.1; 2.2]) |> resolve |> sprintf "%.1f"=== "3.3"
            Fun( muld, "+", [1.1; 2.0]) |> resolve |> sprintf "%.1f"=== "2.2"
            Fun( concat, "concat", ["Hello, "; "world!"]) |> resolve === "Hello, world!"
            Fun( mul, "*", [2; 3]) |> resolve === 6
            Fun( equal, "=", ["Hello"; "world"]) |> resolve === false
            Fun( equal, "=", ["Hello"; "Hello"]) |> resolve === true
            Fun( notEqual, "=", ["Hello"; "world"]) |> resolve === true
            Fun( notEqual, "=", ["Hello"; "Hello"]) |> resolve === false
            Fun( notEqual, "=", [1; 2]) |> resolve === true
            Fun( notEqual, "=", [2; 2]) |> resolve === false

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
           