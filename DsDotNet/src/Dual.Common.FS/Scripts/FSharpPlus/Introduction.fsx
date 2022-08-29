#r "nuget: FSharpPlus"

open FSharpPlus

// http://fsprojects.github.io/FSharpPlus//tutorial.html

module TestBind =
    let x = ["hello";" ";"world"] >>= toList
    // val x : char list = ['h'; 'e'; 'l'; 'l'; 'o'; ' '; 'w'; 'o'; 'r'; 'l'; 'd']


    let tryParseInt : string -> int option = tryParse
    let tryDivide x n = if n = 0 then None else Some (x / n)

    let y = Some "20" >>= tryParseInt >>= tryDivide 100



    let parseAndDivide100By = tryParseInt >=> tryDivide 100

    let parsedAndDivide100By20 = parseAndDivide100By "20"   // Some 5
    let parsedAndDivide100By0' = parseAndDivide100By "zero" // None
    let parsedAndDivide100By0  = parseAndDivide100By "0"    // None

    let parseElement n = List.tryItem n >=> tryParseInt
    let parsedElement  = parseElement 2 ["0"; "1";"2"]


    //let parseAndDivide100By = tryParseInt >=> tryDivide 100

    //let parsedAndDivide100By20 = parseAndDivide100By "20"   // Choice1Of2 5
    //let parsedAndDivide100By0' = parseAndDivide100By "zero" // Choice2Of2 "Failed to parse zero"
    //let parsedAndDivide100By0  = parseAndDivide100By "0"    // Choice2Of2 "Can't divide by zero"



// the generic applicative functor (space invaders) operator
module SpaceInvaders =
    let sumAllOptions = Some (+) <*> Some 2 <*> Some 10     // val sumAllOptions : int option = Some 12
    let sumAllElemets = [(+)] <*> [10; 100] <*> [1; 2; 3]   // int list = [11; 12; 13; 101; 102; 103]



// http://fsprojects.github.io/FSharpPlus//extensions.html
module TestProtect =
    // throws "ArgumentException: The input sequence was empty."
    let expectedSingleItem1 : int = List.exactlyOne []

    // returns a Result.Error holding the exception as its value:
    let expectedSingleItem2 : Result<int,exn> = Result.protect List.exactlyOne []

    // ...or like typical try prefixed functions, treat exception as None
    let expectedSingleItem3 : Option<int> = Option.protect List.exactlyOne []

    // which might look like this:
    let inline tryExactlyOne xs = Option.protect List.exactlyOne xs

// http://fsprojects.github.io/FSharpPlus//extensions.html
module TestResult =
    Result.get
    Result.defaultValue
    Result.defaultWith
    Result.either
    Result.flatten
    Result.bindError
    Result.mapError
    Result.map2

    Choice.either
    Choice.flatten

    Option.flatten
    Option.toResult



    let a = ["Bob"; "Jane"] |> List.intersperse "and"
    // vat a : string list = ["Bob"; "and"; "Jane"]

    let b = "WooHoo" |> String.intersperse '-'
    // val b : string = "W-o-o-H-o-o"