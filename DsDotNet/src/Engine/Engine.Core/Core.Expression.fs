[<AutoOpen>]
module rec Engine.Core.ExpressionModule

open Engine.Common.FS
open System
open System.Linq
open System.Runtime.CompilerServices

[<AutoOpen>]
module private SubModule =
    let expectN (n:int) (xs:'a seq) = if xs.Count() <> n then failwith $"Wrong number of arguments: expect {n}"
    let expect1 xs = expectN 1 xs; xs.First()
    let expect2 xs = expectN 2 xs; Array.ofSeq xs
    let expectGteN (n:int) (xs:'a seq) =
        if xs.Count() < n then failwith $"Wrong number of arguments: expect at least {n} arguments"

    let (|Double|_|) (x:obj) =
        match x with
        | :? double as a -> Some a
        | :? int as a -> Some (double a)
        | :? single as a -> Some (double a)
        | _ -> None
    let (|Integer|_|) (x:obj) =
        match x with
        | :? int as n -> Some n
        | :? uint as n -> Some (int n)
        | _ -> None

    let toDouble = (|Double|_|)   >> Option.get
    let toint    = (|Integer|_|)  >> Option.get

    let isEqual (x:obj) (y:obj) =
        match x, y with
        | Double x, Double y -> x = y
        | (:? string as a), (:? string as b) -> a = b
        | _ -> false

    [<Extension>] // type SeqExt =
    type SeqExt =
        [<Extension>] static member ExpectGteN(xs:'a seq, n) = expectGteN n xs; xs
        [<Extension>] static member Expect1(xs:'a seq) = expect1 xs
        [<Extension>] static member Expect2(xs:'a seq) = expect2 xs

    let toDoublePairwise (args:Arguments) =
        args.ExpectGteN(2)
            .Select(evalArg >> toDouble)
            .Pairwise()


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

let resolve (expr:Expression<'T>) = expr |> eval |> unbox

let add (args:Arguments) =
    args.ExpectGteN(2)
        .Select(evalArg).Cast<int>()
        .Reduce(( + ))
let sub (args:Arguments) =
    args.ExpectGteN(2)
        .Select(evalArg).Cast<int>()
        .Reduce(( - ))
let mul (args:Arguments) =
    args.ExpectGteN(2)
        .Select(evalArg).Cast<int>()
        .Reduce(( * ))
let div (args:Arguments) =
    args.ExpectGteN(2)
        .Select(evalArg >> toDouble)
        .Reduce(( / ))

let equal (args:Arguments) =
    args.ExpectGteN(2)
        .Select(evalArg)
        .Pairwise()
        .All(fun (x, y) -> isEqual x y)
let notEqual (args:Arguments) =
    let xs = args.Expect2().Select(evalArg).ToArray()
    xs[0] <> xs[1]

let gt  (args:Arguments) = toDoublePairwise(args).All(fun (x, y) -> x > y)
let lt  (args:Arguments) = toDoublePairwise(args).All(fun (x, y) -> x < y)
let gte (args:Arguments) = toDoublePairwise(args).All(fun (x, y) -> x >= y)
let lte (args:Arguments) = toDoublePairwise(args).All(fun (x, y) -> x <= y)

let muld (args:Arguments) =
    args.ExpectGteN(2)
        .Select(evalArg >> toDouble)
        .Reduce(( * ))
let addd       (args:Arguments) = args.Select(evalArg >> toDouble)   .ExpectGteN(2).Reduce(( + ))
let concat     (args:Arguments) = args.Select(evalArg).Cast<string>().ExpectGteN(2).Reduce(( + ))
let logicalAnd (args:Arguments) = args.Select(evalArg).Cast<bool>()  .ExpectGteN(2).Reduce(( && ))
let logicalOr  (args:Arguments) = args.Select(evalArg).Cast<bool>()  .ExpectGteN(2).Reduce(( || ))
let shiftLeft  (args:Arguments) = args.Select(evalArg >> toint)      .ExpectGteN(2).Reduce((<<<))
let shiftRight (args:Arguments) = args.Select(evalArg >> toint)      .ExpectGteN(2).Reduce((>>>))

let neg (args:Arguments) =
    args.Select(evalArg).Cast<bool>()
        .Expect1()
        |> not
let sin (args:Arguments) =
    args.Select(evalArg >> toDouble)
        .Expect1()
        |> Math.Sin

