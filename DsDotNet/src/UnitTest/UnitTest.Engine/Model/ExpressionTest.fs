namespace UnitTest.Engine

open System
open Engine.Core
open NUnit.Framework


[<AutoOpen>]
module ExpressionTestModule =
    type ExpressionTest() = 
        do Fixtures.SetUpTest()

        let resolve (expr:Expression<'T>) = expr |> eval |> unbox

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
            Fun( mul, "*", [2; 3]) |> resolve === 6
            Fun( add, "+", [1; 2; 3]) |> resolve === 6
            Fun( addd, "+", [1.1; 2.2]) |> resolve |> sprintf "%.1f"=== "3.3"
            Fun( muld, "+", [1.1; 2.0]) |> resolve |> sprintf "%.1f"=== "2.2"
            Fun( concat, "concat", ["Hello, "; "world!"]) |> resolve === "Hello, world!"
            Fun( mul, "*", [2; 3]) |> resolve === 6
            
            Fun( neg, "!", [true]) |> resolve === false
            Fun( neg, "!", [false]) |> resolve === true
            Fun( logicalAnd, "&", [true; false]) |> resolve === false
            Fun( logicalAnd, "&", [true; true]) |> resolve === true
            Fun( logicalAnd, "&", [true; true; true; false]) |> resolve === false
            Fun( logicalOr, "|", [true; false]) |> resolve === true
            Fun( logicalOr, "|", [false; false]) |> resolve === false
            Fun( logicalOr, "|", [true; true; true; false]) |> resolve === true

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
           