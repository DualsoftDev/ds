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

    type ExpressionType =
        | ExpTypeFunction
        | ExpTypeVariable
        | ExpTypeTag
        | ExpTypeLiteral
    /// Expression<'T> 을 boxed 에서 접근하기 위한 최소의 interface
    type IExpression =
        abstract DataType : System.Type
        abstract ExpressionType : ExpressionType
        abstract BoxedEvaluatedValue : obj
        /// Tag<'T> 나 Variable<'T> 객체 boxed 로 반환
        abstract GetBoxedRawObject: unit -> obj
        abstract ToText : withParenthesys:bool -> string
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

        member x.ExpressionType =
            match x with
            | Tag _ -> ExpTypeTag
            | Variable _ -> ExpTypeVariable
            | Literal _ -> ExpTypeLiteral

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

    type FunctionSpec<'T> = {
        f: Arguments -> 'T
        name: string
        args:Arguments
    }

    type Expression<'T> =
        | Terminal of Terminal<'T>
        | Function of FunctionSpec<'T>  //f:(Arguments -> 'T) * name:string * args:Arguments
        interface IExpression with
            member x.DataType = x.DataType
            member x.ExpressionType = x.ExpressionType
            member x.BoxedEvaluatedValue = x.Evaluate() |> box
            member x.GetBoxedRawObject() = x.GetBoxedRawObject()
            member x.ToText(withParenthesys) = x.ToText(withParenthesys)

        member x.DataType = typedefof<'T>
        member x.ExpressionType =
            match x with
            | Terminal b -> b.ExpressionType
            | Function _ -> ExpTypeFunction
        member x.GetBoxedRawObject() =
            match x with
            | Terminal b -> b.GetBoxedRawObject()
            | Function fs -> fs |> box

        member x.Evaluate() =
            match x with
            | Terminal b -> b.Evaluate()
            | Function fs -> fs.f (fs.args |> List.map evalArg)

        member x.ToText(withParenthesys:bool) =
            match x with
            | Terminal b -> b.ToString()
            | Function fs ->
                let text = fwdSerializeFunctionNameAndBoxedArguments fs.name fs.args withParenthesys
                text



    let getTypeOfBoxedExpression (exp:obj) = (exp :?> IExpression).DataType
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
        let add            (args:Args) = Function { f=_add;            name="+";      args=args}
        let abs            (args:Args) = Function { f=_abs;            name="abs";    args=args}
        let absd           (args:Args) = Function { f=_absd;           name="absD";   args=args}
        let sub            (args:Args) = Function { f=_sub;            name="-";      args=args}
        let mul            (args:Args) = Function { f=_mul;            name="*";      args=args}
        let div            (args:Args) = Function { f=_div;            name="/";      args=args}
        let modulo         (args:Args) = Function { f=_modulo;         name="%";      args=args}

        let equal          (args:Args) = Function { f=_equal;          name="=";      args=args}
        let notEqual       (args:Args) = Function { f=_notEqual;       name="!=";     args=args}
        let gt             (args:Args) = Function { f=_gt;             name=">";      args=args}
        let lt             (args:Args) = Function { f=_lt;             name="<";      args=args}
        let gte            (args:Args) = Function { f=_gte;            name=">=";     args=args}
        let lte            (args:Args) = Function { f=_lte;            name="<=";     args=args}
        let equalString    (args:Args) = Function { f=_equalString;    name="=T";     args=args}
        let notEqualString (args:Args) = Function { f=_notEqualString; name="!=T";    args=args}

        let muld           (args:Args) = Function { f=_muld;           name="*";      args=args}
        let addd           (args:Args) = Function { f=_addd;           name="+";      args=args}
        let subd           (args:Args) = Function { f=_subd;           name="-D";     args=args}
        let divd           (args:Args) = Function { f=_divd;           name="/D";     args=args}
        let modulod        (args:Args) = Function { f=_modulo;         name="%D";     args=args}
        let concat         (args:Args) = Function { f=_concat;         name="+";      args=args}
        let logicalAnd     (args:Args) = Function { f=_logicalAnd;     name="&";      args=args}
        let logicalOr      (args:Args) = Function { f=_logicalOr;      name="|";      args=args}
        let logicalNot     (args:Args) = Function { f=_logicalNot;     name="!";      args=args}
        let orBit          (args:Args) = Function { f=_orBit;          name="orBit";  args=args}
        let andBit         (args:Args) = Function { f=_andBit;         name="andBit"; args=args}
        let notBit         (args:Args) = Function { f=_notBit;         name="notBit"; args=args}
        let xorBit         (args:Args) = Function { f=_xorBit;         name="xorBit"; args=args}
        let shiftLeft      (args:Args) = Function { f=_shiftLeft;      name="<<";     args=args}
        let shiftRight     (args:Args) = Function { f=_shiftRight;     name=">>";     args=args}
        let sin            (args:Args) = Function { f=_sin;            name="sin";    args=args}
        let Bool           (args:Args) = Function { f=_convertBool;    name="Bool";   args=args}
        let Int            (args:Args) = Function { f=_convertInt;     name= "Int";   args=args}

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
                 | Assign     (expr, target) -> $"assign({expr.ToText(false)}, {target.ToText()})"

