namespace rec Engine.Core
open System
open System.Linq
open System.Runtime.CompilerServices
open Engine.Common.FS
open System.Diagnostics

[<AutoOpen>]
module ExpressionModule =



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
            | :? double as n -> Some (int n)
            | _ -> None
        let (|Bool|_|) (x:obj) =
            match x with
            | :? bool as n -> Some n
            | Integer n when n <> 0 -> Some true
            | _ -> None

        let (|PLCTag|_|) (x:Terminal<'T>) =
            match x with
            | Tag t -> Some t
            | _ -> None

        let toBool   = (|Bool|_|)     >> Option.get
        let toDouble = (|Double|_|)   >> Option.get
        let toInt    = (|Integer|_|)  >> Option.get
        let toTag x  = (|PLCTag|_|) x |> Option.get
        let toString (x:obj) = Convert.ToString x

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


    /// Expression<'T> 로 생성할 수 있는 interface
    type IExpressionCreatable    =
        abstract CreateBoxedExpression: unit -> obj        // Terminal<'T>
        //abstract Name   : string
        //abstract Value  : obj with get,set
        //abstract ToText   : unit -> string

    /// Expression<'T> 을 boxed 에서 접근하기 위한 최소의 interface
    type IExpression =
        abstract Type : System.Type
        abstract BoxedEvaluatedValue : obj
        /// Tag<'T> 나 Variable<'T> 객체 boxed 로 반환
        abstract GetBoxedRawObject: unit -> obj
        abstract ToText   : unit -> string
    //    abstract ToJson   : unit -> ExpressionJson


    [<AbstractClass>]
    [<DebuggerDisplay("{Name}")>]
    type TypedValueStorage<'T>(name, initValue:'T) =
        member x.ToText() = $"({name}={(x.Value.ToString())})"
        member _.Name: string = name
        member val Value = initValue with get, set

        interface IExpressionCreatable with
            member x.CreateBoxedExpression() = x.CreateBoxedExpression()
        abstract CreateBoxedExpression: unit -> obj

        interface INamed with
            member x.Name with get() = x.Name and set(v) = failwith "ERROR: not supported"



    [<AbstractClass>]
    type Tag<'T>(name, initValue:'T) =
        inherit TypedValueStorage<'T>(name, initValue)

        //memory bit masking 처리를 위해 일반 PlcTag와 DsMemory 구별 구현
        // <ahn> : obj -> 'T
        abstract SetValue:obj -> unit
        abstract GetValue:unit -> obj
        override x.CreateBoxedExpression() = Terminal(Terminal.Tag x)

    // todo: 임시 이름... 추후 Variable로
    type StorageVariable<'T>(name, initValue:'T) =
        inherit TypedValueStorage<'T>(name, initValue)
        override x.CreateBoxedExpression() = Terminal(Terminal.Variable x)

    type Terminal<'T> =
        | Tag of Tag<'T>
        | Variable of StorageVariable<'T>
        | Literal of 'T


        member x.GetBoxedRawObject() =
            match x with
            | Tag t -> t |> box
            | Variable v -> v
            | Literal v -> v |> box

        member x.Evaluate() =
            match x with
            | Tag t -> t.Value
            | Variable v -> v.Value
            | Literal v -> v

        override x.ToString() =
            match x with
            | Tag t -> $"({t.Name}={t.Value})"
            | Variable t -> $"({t.Name}={t.Value})"
            | Literal v -> $"{v}"



    type Arguments = obj list
    type Args      = Arguments

    type Expression<'T> =
        | Terminal of Terminal<'T>
        | Function of f:(Arguments -> 'T) * name:string * args:Arguments
        interface IExpression with
            member x.Type = x.Type
            member x.BoxedEvaluatedValue = x.Evaluate() |> box
            member x.GetBoxedRawObject() = x.GetBoxedRawObject()
            member x.ToText() = x.ToText()

        member x.Type = typedefof<'T>
        member x.GetBoxedRawObject() =
            match x with
            | Terminal b -> b.GetBoxedRawObject()
            | Function _ -> null

        member x.Evaluate() =
            match x with
            | Terminal b -> b.Evaluate()
            | Function (f, n, args) -> f (args |> List.map evalArg)

        member x.ToText() =
            match x with
            | Terminal b -> b.ToString()
            | Function (f, n, args) -> fwdSerializeFunctionExpression n args



    let getTypeOfBoxedExpression (exp:obj) = (exp :?> IExpression).Type
    let value (x:'T) = Terminal (Literal x)
    let tag (t: Tag<'T>) = Terminal (Tag t)

    /// storage:obj --> 실제는 Tag<'T> or StorageVariable<'T> type 객체 boxed
    let createExpressionFromBoxedStorage (storage:obj) =
        let t = storage :?> IExpressionCreatable
        t.CreateBoxedExpression()

    //let binaryExpression (opnd1:Expression<'T>) (op:string) (opnd2:Expression<'T>) =
    let createBinaryExpression (opnd1:obj) (op:string) (opnd2:obj) =
        let t1 = getTypeOfBoxedExpression opnd1
        let t2 = getTypeOfBoxedExpression opnd2
        if t1 <> t2 then
            failwith "ERROR: Type mismatch"

        let args = [box opnd1; opnd2]

        if t1 = typeof<int> then
            match op with
            | "+" -> add args
            | "-" -> sub args
            | "*" -> mul args
            | "/" -> div args
            | _ -> failwith "NOT Yet"
            |> box
        elif t1 = typeof<double> then
            match op with
            | "+" -> addd args
            | "-" -> subd args
            | "*" -> muld args
            | "/" -> divd args
            | _ -> failwith "NOT Yet"
            |> box
        elif t1 = typeof<string> then
            match op with
            | "+" -> concat args
            | _ -> failwith "ERROR"
            |> box
        else
            failwith "ERROR"

    let createCustomFunctionExpression (funName:string) (args:Args) =
        match funName with
        | "Int" -> Int args |> box
        | "Bool" -> Bool args |> box
        | "sin" -> sin args |> box
        //| "cos" -> cos args |> box
        //| "tan" -> tan args |> box
        | _ -> failwith "NOT yet"

    let evaluateBoxedExpression (boxedExpr:obj) =
        let expr = boxedExpr :?> IExpression
        expr.BoxedEvaluatedValue


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
        | :? Expression<_> as exp ->
            tracefn "Generic expression"
            exp.Evaluate()
        | _ ->
            failwith "error"

    let resolve (expr:Expression<'T>) = expr.Evaluate() |> unbox



    [<AutoOpen>]
    module FunctionModule =
        let add        (args:Arguments) = Function (_add,        "+", args)
        let abs        (args:Arguments) = Function (_abs,        "abs", args)
        let absd       (args:Arguments) = Function (_absd,       "absD", args)
        let sub        (args:Arguments) = Function (_sub,        "-", args)
        let mul        (args:Arguments) = Function (_mul,        "*", args)
        let div        (args:Arguments) = Function (_div,        "/", args)
        let modulo     (args:Arguments) = Function (_modulo,     "%", args)

        let equal      (args:Arguments) = Function (_equal,      "=", args)
        let notEqual   (args:Arguments) = Function (_notEqual,   "!=", args)
        let gt         (args:Arguments) = Function (_gt,         ">", args)
        let lt         (args:Arguments) = Function (_lt,         "<", args)
        let gte        (args:Arguments) = Function (_gte,        ">=", args)
        let lte        (args:Arguments) = Function (_lte,        "<=", args)
        let equalString    (args:Arguments) = Function (_equalString,      "=T", args)
        let notEqualString (args:Arguments) = Function (_notEqualString,   "!=T", args)

        let muld       (args:Arguments) = Function (_muld,       "*", args)
        let addd       (args:Arguments) = Function (_addd,       "+", args)
        let subd       (args:Arguments) = Function (_subd,       "-D", args)
        let divd       (args:Arguments) = Function (_divd,       "/D", args)
        let modulod    (args:Arguments) = Function (_modulo,     "%D", args)
        let concat     (args:Arguments) = Function (_concat,     "+", args)
        let logicalAnd (args:Arguments) = Function (_logicalAnd, "&", args)
        let logicalOr  (args:Arguments) = Function (_logicalOr,  "|", args)
        let logicalNot (args:Arguments) = Function (_logicalNot, "!", args)
        let orBit      (args:Arguments) = Function (_orBit,      "orBit", args)
        let andBit     (args:Arguments) = Function (_andBit,     "andBit", args)
        let notBit     (args:Arguments) = Function (_notBit,     "notBit", args)
        let xorBit     (args:Arguments) = Function (_xorBit,     "xorBit", args)
        let shiftLeft  (args:Arguments) = Function (_shiftLeft,  "<<", args)
        let shiftRight (args:Arguments) = Function (_shiftRight, ">>", args)
        let sin        (args:Arguments) = Function (_sin,        "sin", args)
        let Bool       (args:Arguments) = Function (_convertBool, "Bool", args)
        let Int        (args:Arguments) = Function (_convertInt, "Int", args)

        let anD = logicalAnd
        let absDouble = absd
        let oR = logicalOr
        let noT = logicalNot
        let divDouble = divd
        let addString = concat

        [<AutoOpen>]
        module internal FunctionImpl =
            let _add  (args:Arguments) = args.ExpectGteN(2).Select(evalArg).Cast<int>().Reduce(( + ))
            let _abs  (args:Arguments) = args.Select(evalArg).Cast<int>().Head() |> Math.Abs
            let _absd (args:Arguments) = args.Select(evalArg >> toDouble).Head() |> Math.Abs
            let _sub  (args:Arguments) = args.ExpectGteN(2).Select(evalArg).Cast<int>().Reduce(( - ))
            let _mul  (args:Arguments) = args.ExpectGteN(2).Select(evalArg).Cast<int>().Reduce(( * ))

            let _div  (args:Arguments) = args.ExpectGteN(2) .Select(evalArg).Cast<int>().Reduce(( / ))
            let _divd (args:Arguments) = args.ExpectGteN(2) .Select(evalArg >> toDouble).Reduce(( / ))
            let _modulo (args:Arguments) = args.ExpectGteN(2) .Select(evalArg).Cast<int>().Reduce(( % ))
            let _modulod (args:Arguments) = args.ExpectGteN(2) .Select(evalArg >> toDouble).Reduce(( % ))

            let _equal (args:Arguments) = args.ExpectGteN(2) .Select(evalArg) .Pairwise() .All(fun (x, y) -> isEqual x y)
            let _notEqual (args:Arguments) = not <| _equal args
            let _equalString (args:Arguments) = args.ExpectGteN(2) .Select(evalArg).Cast<string>().Distinct().Count() = 1
            let _notEqualString (args:Arguments) = not <| _equalString args

            let private toDoublePairwise (args:Arguments) = args.ExpectGteN(2).Select(evalArg >> toDouble).Pairwise()
            let _gt  (args:Arguments) = toDoublePairwise(args).All(fun (x, y) -> x > y)
            let _lt  (args:Arguments) = toDoublePairwise(args).All(fun (x, y) -> x < y)
            let _gte (args:Arguments) = toDoublePairwise(args).All(fun (x, y) -> x >= y)
            let _lte (args:Arguments) = toDoublePairwise(args).All(fun (x, y) -> x <= y)

            let _muld       (args:Arguments) = args.ExpectGteN(2).Select(evalArg >> toDouble)   .Reduce(( * ))
            let _addd       (args:Arguments) = args.ExpectGteN(2).Select(evalArg >> toDouble)   .Reduce(( + ))
            let _subd       (args:Arguments) = args.ExpectGteN(2).Select(evalArg >> toDouble)   .Reduce(( - ))
            let _concat     (args:Arguments) = args.ExpectGteN(2).Select(evalArg).Cast<string>().Reduce(( + ))
            let _logicalAnd (args:Arguments) = args.ExpectGteN(2).Select(evalArg).Cast<bool>()  .Reduce(( && ))
            let _logicalOr  (args:Arguments) = args.ExpectGteN(2).Select(evalArg).Cast<bool>()  .Reduce(( || ))
            let _logicalNot (args:Arguments) = args.Select(evalArg).Cast<bool>().Expect1() |> not
            let _xorBit     (args:Arguments) = args.Select(evalArg).Cast<int>()                 .Reduce (^^^)
            let _orBit      (args:Arguments) = args.Select(evalArg).Cast<int>()                 .Reduce (|||)
            let _andBit     (args:Arguments) = args.Select(evalArg).Cast<int>()                 .Reduce (&&&)
            let _notBit     (args:Arguments) = args.Select(evalArg).Cast<int>().Expect1()       |> (~~~)
            let _shiftLeft  (args:Arguments) = args.ExpectGteN(2).Select(evalArg >> toInt)      .Reduce((<<<))
            let _shiftRight (args:Arguments) = args.ExpectGteN(2).Select(evalArg >> toInt)      .Reduce((>>>))

            let _sin (args:Arguments) = args.Select(evalArg >> toDouble) .Expect1() |> Math.Sin
            let _convertBool (args:Arguments) = args.Select(evalArg >> toBool) .Expect1()
            let _convertInt (args:Arguments) = args.Select(evalArg >> toInt) .Expect1()
    [<AutoOpen>]
    module StatementModule =
        type Statement<'T> =
            | Assign of expr:Expression<'T> * target:Tag<'T>
            member x.Do() =
                match x with
                | Assign (expr, target) -> expr.Evaluate() |> target.SetValue
            member x.ToText() =
                 match x with
                 | Assign     (expr, target) -> $"assign({expr.ToText()}, {target.ToText()})"

