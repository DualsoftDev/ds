[<AutoOpen>]
module rec Engine.Core.ExpressionModule

open Engine.Common.FS
open System
open System.Linq

[<AutoOpen>]
module private SubModule =
    let toDouble (x:obj) =
        match x with
        | :? int as n -> double n
        | :? double as n -> n
        | :? single as n -> double n
        | _ -> failwith "ERROR"

    let expectN (n:int) (xs:'a seq) = if xs.Count() <> n then failwith $"Wrong number of arguments: expect {n}"
    let expect1 xs = expectN 1 xs
    let expect2 xs = expectN 2 xs
    let expectGreaterThanOrEqualN (n:int) (xs:'a seq) =
        if xs.Count() < n then failwith $"Wrong number of arguments: expect at least {n} arguments"

type Arguments = obj list

type Expression<'T> =
    | Value of 'T
    | Fun of f:(Arguments -> 'T) * name:string * args:Arguments

    member x.Evaluate() =
        match x with
        | Value b -> b
        | Fun (f, n, args) -> f (args |> List.map evalArg)
        |> box

let eval (expr:Expression<'T>) =
    match expr with
    | Fun (f, n, args) -> f (args |> List.map evalArg)
    | Value v -> v

let private evalArg (x:obj) =
    let t = x.GetType()
    match x with
    (* primitive types *)
    | (:? bool | :? string | :? int | :? double | :? single) ->
        x
    (*  expression: 'T general type 으로는 match 가 안되니, 중복해서 쓸 수 밖에 없음. *)
    | :? Expression<bool>   as exp -> exp.Evaluate()
    | :? Expression<int>    as exp -> exp.Evaluate()
    | :? Expression<double> as exp -> exp.Evaluate()
    | :? Expression<single> as exp -> exp.Evaluate()
    | :? Expression<string> as exp -> exp.Evaluate()
    | _ ->
        failwith "error"

let mul (args:Arguments) =
    args.Select(evalArg).Cast<int>()
        .TapWhole(expectGreaterThanOrEqualN 2)
        .Reduce(( * ))
let add (args:Arguments) =
    args.Select(evalArg).Cast<int>()
        .TapWhole(expectGreaterThanOrEqualN 2)
        .Reduce(( + ))
let sub (args:Arguments) =
    args.Select(evalArg).Cast<int>()
        .TapWhole(expectGreaterThanOrEqualN 2)
        .Reduce(( + ))
let muld (args:Arguments) =
    args.Select(evalArg >> toDouble)
        .TapWhole(expectGreaterThanOrEqualN 2)
        .Reduce(( * ))
let addd       (args:Arguments) = args.Select(evalArg >> toDouble)   .Reduce(( + ))
let concat     (args:Arguments) = args.Select(evalArg).Cast<string>().Reduce(( + ))
let logicalAnd (args:Arguments) = args.Select(evalArg).Cast<bool>()  .Reduce(( && ))
let logicalOr  (args:Arguments) = args.Select(evalArg).Cast<bool>()  .Reduce(( || ))
let neg (args:Arguments) =
    args.Select(evalArg).Cast<bool>()
        .TapWhole(expect1)
        |> Seq.exactlyOne |> not
let sin (args:Arguments) =
    args.Select(evalArg >> toDouble)
        .TapWhole(expect1)
        .First()
        |> Math.Sin
            
