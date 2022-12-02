namespace rec Engine.Core
open System
open System.Linq
open System.Runtime.CompilerServices
open Engine.Common.FS
open System.Diagnostics

(*  expression: generic type <'T> 나 <_> 으로는 <obj> match 으로 간주됨
    Expression<'T> 객체에 대한 matching
    * :? Expression<int> as x -> 형태로 type 을 지정하면 matching 이 가능하다.
    * :? Expression<_> as x ->   형태로 type 을 지정하지 않으면, Expression<obj> 로 matching 시도해서 matching 이 불가능하다.
    * :? Expression<'T> as x ->  형태로 type 을 지정하지 않으면, Expression<obj> 로 matching 시도해서 matching 이 불가능하다.
    * matching 해서 수행해야 할 필요한 기능들은 non generic interface 인 IExpression 에 담아 두고, 이를 matching 한다.
*)

[<AutoOpen>]
module ExpressionModule =
    open SubModule


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

        interface ITag
        //memory bit masking 처리를 위해 일반 PlcTag와 DsMemory 구별 구현
        // <ahn> : obj -> 'T
        abstract SetValue:obj -> unit
        abstract GetValue:unit -> obj
        override x.CreateBoxedExpression() = Terminal(Terminal.Tag x)

    // todo: 임시 이름... 추후 Variable로
    type StorageVariable<'T>(name, initValue:'T) =
        inherit TypedValueStorage<'T>(name, initValue)

        interface IVariable
        override x.CreateBoxedExpression() = Terminal(Terminal.Variable x)

    type Terminal<'T> =
        | Tag of Tag<'T>
        | Variable of StorageVariable<'T>
        | Literal of 'T

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

    let getTypeOfBoxedExpression (exp:obj) = (exp :?> IExpression).DataType

    let value (x:'T) = Terminal (Literal x)
    let tag (t: Tag<'T>) = Terminal (Tag t)

    let expr (x:obj) =
        match x with
        | :? IExpression as e -> x
        | :? IExpressionCreatable as c -> c.CreateBoxedExpression()
        | :? sbyte  as o -> Terminal (Literal o)
        | :? byte   as o -> Terminal (Literal o)
        | :? int16  as o -> Terminal (Literal o)
        | :? uint16 as o -> Terminal (Literal o)
        | :? int32  as o -> Terminal (Literal o)
        | :? uint32 as o -> Terminal (Literal o)
        | :? int64  as o -> Terminal (Literal o)
        | :? uint64 as o -> Terminal (Literal o)
        | :? single as o -> Terminal (Literal o)
        | :? bool   as o -> Terminal (Literal o)
        | :? double as o -> Terminal (Literal o)
        | :? char   as o -> Terminal (Literal o)
        | :? string as o -> Terminal (Literal o)
        | _ -> failwith "ERROR"


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
        | (:? bool | :? string | :? int | :? double | :? single) -> x
        | :? IExpression as exp -> exp.BoxedEvaluatedValue
        | _ ->
            failwith "error"

    let resolve (expr:Expression<'T>) = expr.Evaluate() |> unbox



    [<AutoOpen>]
    module FunctionModule =
        /// Create function
        let private cf (f:Args->'T) (name:string) (args:Args) =
            let args = args |> map expr
            Function { f=f; name=name; args=args}

        let add            args = cf _add            "+"    args
        let abs            args = cf _abs            "abs"    args
        let absd           args = cf _absd           "absD"   args
        let sub            args = cf _sub            "-"      args
        let mul            args = cf _mul            "*"      args
        let div            args = cf _div            "/"      args
        let modulo         args = cf _modulo         "%"      args
        let equal          args = cf _equal          "="      args
        let notEqual       args = cf _notEqual       "!="     args
        let gt             args = cf _gt             ">"      args
        let lt             args = cf _lt             "<"      args
        let gte            args = cf _gte            ">="     args
        let lte            args = cf _lte            "<="     args
        let equalString    args = cf _equalString    "=T"     args
        let notEqualString args = cf _notEqualString "!=T"    args
        let muld           args = cf _muld           "*"      args
        let addd           args = cf _addd           "+"      args
        let subd           args = cf _subd           "-D"     args
        let divd           args = cf _divd           "/D"     args
        let modulod        args = cf _modulo         "%D"     args
        let concat         args = cf _concat         "+"      args
        let logicalAnd     args = cf _logicalAnd     "&"      args
        let logicalOr      args = cf _logicalOr      "|"      args
        let logicalNot     args = cf _logicalNot     "!"      args
        let orBit          args = cf _orBit          "orBit"  args
        let andBit         args = cf _andBit         "andBit" args
        let notBit         args = cf _notBit         "notBit" args
        let xorBit         args = cf _xorBit         "xorBit" args
        let shiftLeft      args = cf _shiftLeft      "<<"     args
        let shiftRight     args = cf _shiftRight     ">>"     args
        let sin            args = cf _sin            "sin"    args
        let Bool           args = cf _convertBool    "Bool"   args
        let Int            args = cf _convertInt     "Int"    args

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

    type Terminal<'T> with
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

        member x.ToText() =
            match x with
            | Tag t -> $"({t.Name}={t.Value})"
            | Variable t -> $"({t.Name}={t.Value})"
            | Literal v -> $"{v}"

    type Expression<'T> with
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
            | Terminal b -> b.ToText()
            | Function fs ->
                let text = fwdSerializeFunctionNameAndBoxedArguments fs.name fs.args withParenthesys
                text


