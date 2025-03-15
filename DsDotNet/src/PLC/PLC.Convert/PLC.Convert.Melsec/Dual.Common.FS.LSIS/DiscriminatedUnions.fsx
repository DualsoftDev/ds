open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Reflection

let rec isUnionCase = function
    | Lambda (_, expr) | Let (_, _, expr) -> isUnionCase expr
    | NewTuple exprs -> 
        let iucs = List.map isUnionCase exprs
        fun value -> List.exists ((|>) value) iucs
    | NewUnionCase (uci, _) ->
        let utr = FSharpValue.PreComputeUnionTagReader uci.DeclaringType
        box >> utr >> (=) uci.Tag
    | _ -> failwithlog "Expression is no union case."


type SomeType =
    | SomeCase1
    | SomeCase2 of int
    | SomeCase3 of int * int
    | SomeCase4 of int * int * int
    | SomeCase5 of int * int * int * int

let list =
    [
        SomeCase1
        SomeCase2  1
        SomeCase3 (2, 3)
        SomeCase4 (4, 5, 6)
        SomeCase5 (7, 8, 9, 10)
    ]

list 
    |> List.filter (isUnionCase <@ SomeCase4 @>)
    |> printfn "Matching SomeCase4: %A"

list
    |> List.filter (isUnionCase <@ SomeCase3, SomeCase4 @>)
    |> printfn "Matching SomeCase3 || SomeCase4: %A"