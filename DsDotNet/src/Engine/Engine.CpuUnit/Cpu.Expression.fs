[<AutoOpen>]
module rec Engine.Cpu.Expression

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

    let (|PLCTag|_|) (x:Terminal<'T>) =
        match x with
        | Tag t -> Some t
        | _ -> None

    let toDouble = (|Double|_|)   >> Option.get
    let toint    = (|Integer|_|)  >> Option.get
    let toTag x  = (|PLCTag|_|) x |> Option.get

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
        [<Extension>] static member Pairwise(xs:'a seq)      = Seq.pairwise xs
        [<Extension>] static member Reduce(xs:'a seq, f)     = Seq.reduce f xs
        [<Extension>] static member JoinWith(xs:string seq, separator) = String.Join(separator, xs)

/// sample PLC tag class
type PLCTag<'T>(name, value:'T) =
    member _.Name = name
    member val Value = value with get, set
    override x.ToString() = $"({x.Name}={x.Value})"

type Terminal<'T> =
    | Tag of PLCTag<'T>
    | Value of 'T
    member x.Evaluate() =
        match x with
        | Tag t -> t.Value
        | Value v -> v
    override x.ToString() =
        match x with
        | Tag t -> $"({t.Name}={t.Value})"
        | Value v -> $"{v}"


type Arguments = obj list

type Expression<'T> =
    | Terminal of Terminal<'T>
    | Fun of f:(Arguments -> 'T) * name:string * args:Arguments

    member x.Evaluate() =
        match x with
        | Terminal b -> b.Evaluate()
        | Fun (f, n, args) -> f (args |> List.map evalArg)
    override x.ToString() =
        match x with
        | Terminal b -> b.ToString()
        | Fun (f, n, args) ->
            let strArgs = args.Select(fun x -> x.ToString()).JoinWith(", ")
            $"{n}({strArgs})"

let value (x:'T) = Terminal (Value x)
let tag (t: PLCTag<'T>) = Terminal (Tag t)

let eval (expr:Expression<'T>) =
    match expr with
    | Fun (f, n, args) -> f (args |> List.map evalArg)
    | Terminal v -> v.Evaluate()

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

[<AutoOpen>]
module FunctionModule =
    let add        (args:Arguments) = Fun (_add,        "+", args)
    let sub        (args:Arguments) = Fun (_sub,        "-", args)
    let mul        (args:Arguments) = Fun (_mul,        "*", args)
    let div        (args:Arguments) = Fun (_div,        "/", args)

    let equal      (args:Arguments) = Fun (_equal,      "=", args)
    let notEqual   (args:Arguments) = Fun (_notEqual,   "!=", args)
    let gt         (args:Arguments) = Fun (_gt,         ">", args)
    let lt         (args:Arguments) = Fun (_lt,         "<", args)
    let gte        (args:Arguments) = Fun (_gte,        ">=", args)
    let lte        (args:Arguments) = Fun (_lte,        "<=", args)

    let muld       (args:Arguments) = Fun (_muld,       "*", args)
    let addd       (args:Arguments) = Fun (_addd,       "+", args)
    let concat     (args:Arguments) = Fun (_concat,     "+", args)
    let logicalAnd (args:Arguments) = Fun (_logicalAnd, "&", args)
    let logicalOr  (args:Arguments) = Fun (_logicalOr,  "|", args)
    let shiftLeft  (args:Arguments) = Fun (_shiftLeft,  "<<", args)
    let shiftRight (args:Arguments) = Fun (_shiftRight, ">>", args)
    let neg        (args:Arguments) = Fun (_neg,        "!", args)
    let sin        (args:Arguments) = Fun (_sin,        "sin", args)

    [<AutoOpen>]
    module FunctionImpl =
        let _add (args:Arguments) = args.ExpectGteN(2).Select(evalArg).Cast<int>().Reduce(( + ))
        let _sub (args:Arguments) = args.ExpectGteN(2).Select(evalArg).Cast<int>().Reduce(( - ))
        let _mul (args:Arguments) = args.ExpectGteN(2).Select(evalArg).Cast<int>().Reduce(( * ))

        let _div (args:Arguments) = args.ExpectGteN(2) .Select(evalArg >> toDouble) .Reduce(( / ))

        let _equal (args:Arguments) = args.ExpectGteN(2) .Select(evalArg) .Pairwise() .All(fun (x, y) -> isEqual x y)
        let _notEqual (args:Arguments) =
            let xs = args.Expect2().Select(evalArg).ToArray()
            xs[0] <> xs[1]

        let private toDoublePairwise (args:Arguments) = args.ExpectGteN(2).Select(evalArg >> toDouble).Pairwise()
        let _gt  (args:Arguments) = toDoublePairwise(args).All(fun (x, y) -> x > y)
        let _lt  (args:Arguments) = toDoublePairwise(args).All(fun (x, y) -> x < y)
        let _gte (args:Arguments) = toDoublePairwise(args).All(fun (x, y) -> x >= y)
        let _lte (args:Arguments) = toDoublePairwise(args).All(fun (x, y) -> x <= y)

        let _muld       (args:Arguments) = args.ExpectGteN(2).Select(evalArg >> toDouble)   .Reduce(( * ))
        let _addd       (args:Arguments) = args.ExpectGteN(2).Select(evalArg >> toDouble)   .Reduce(( + ))
        let _concat     (args:Arguments) = args.ExpectGteN(2).Select(evalArg).Cast<string>().Reduce(( + ))
        let _logicalAnd (args:Arguments) = args.ExpectGteN(2).Select(evalArg).Cast<bool>()  .Reduce(( && ))
        let _logicalOr  (args:Arguments) = args.ExpectGteN(2).Select(evalArg).Cast<bool>()  .Reduce(( || ))
        let _shiftLeft  (args:Arguments) = args.ExpectGteN(2).Select(evalArg >> toint)      .Reduce((<<<))
        let _shiftRight (args:Arguments) = args.ExpectGteN(2).Select(evalArg >> toint)      .Reduce((>>>))

        let _neg (args:Arguments) = args.Select(evalArg).Cast<bool>().Expect1() |> not
        let _sin (args:Arguments) = args.Select(evalArg >> toDouble) .Expect1() |> Math.Sin

[<AutoOpen>]
module StatementModule =
    type Statement<'T> =
        | Assign of expr:Expression<'T> * target:PLCTag<'T>
        member x.Do() =
            match x with
            | Assign (expr, target) -> target.Value <- expr.Evaluate()
        override x.ToString() =
            match x with
            | Assign (expr, target) -> $"assign({expr.ToString()}, {target.ToString()})"

