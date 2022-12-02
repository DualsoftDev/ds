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


    [<AbstractClass>]
    [<DebuggerDisplay("{Name}")>]
    type TypedValueStorage<'T>(name, initValue:'T) =
        member _.Name: string = name
        member val Value = initValue with get, set

        interface IStorage with
            member x.Value with get() = x.Value and set(v) = x.Value <- v :?> 'T
            member x.ToText() = x.ToText()
        interface IStorage<'T> with
            member x.Value with get() = x.Value and set(v) = x.Value <- v
        interface IExpressionCreatable with
            member x.CreateBoxedExpression() = x.CreateBoxedExpression()
        interface INamed with
            member x.Name with get() = x.Name and set(v) = failwith "ERROR: not supported"

        abstract CreateBoxedExpression: unit -> obj
        abstract ToText: unit -> string




    [<AbstractClass>]
    type Tag<'T>(name, initValue:'T) =
        inherit TypedValueStorage<'T>(name, initValue)

        interface ITag
        abstract SetValue:obj -> unit
        abstract GetValue:unit -> obj
        override x.CreateBoxedExpression() = Terminal(Terminal.Tag x)
        override x.ToText() = "%" + name

    // todo: 임시 이름... 추후 Variable로
    type StorageVariable<'T>(name, initValue:'T) =
        inherit TypedValueStorage<'T>(name, initValue)

        interface IVariable
        override x.CreateBoxedExpression() = Terminal(Terminal.Variable x)
        override x.ToText() = "$" + name

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

    let value (x:'T) =
        let t = x.GetType()
        if t.IsValueType || t = typedefof<string> then
            Terminal (Literal x)
        else
            failwith "ERROR: Value Type Error.  only allowed for primitive type"
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

    [<AutoOpen>]
    module FunctionModule =
        /// Create function
        let private cf (f:Args->'T) (name:string) (args:Args) =
            let args = args |> map expr
            Function { f=f; name=name; args=args}

        (*
            /* .f */    Single
            /* . */  | Double
            /* y */  | Sbyte
            /* uy */ | Byte
            /* s */  | Int16
            /* us */ | Uint16
            /* - */  | Int32
            /* u */  | Uint32
            /* L */  | Int64
            /* UL */ | Uint64
        *)

        let muly     args = cf _muly     "*"     args
        let addy     args = cf _addy     "+"     args
        let suby     args = cf _suby     "-"     args
        let divy     args = cf _divy     "/"     args
        let absy     args = cf _absy     "abs"   args
        let moduloy  args = cf _moduloy  "%"     args

        let muluy    args = cf _muluy    "*"     args
        let adduy    args = cf _adduy    "+"     args
        let subuy    args = cf _subuy    "-"     args
        let divuy    args = cf _divuy    "/"     args
        let absuy    args = cf _absuy    "abs"   args
        let modulouy args = cf _modulouy "%"     args

        let muls     args = cf _muls     "*"     args
        let adds     args = cf _adds     "+"     args
        let subs     args = cf _subs     "-"     args
        let divs     args = cf _divs     "/"     args
        let abss     args = cf _abss     "abs"   args
        let modulos  args = cf _modulos  "%"     args

        let mulus    args = cf _mulus    "*"     args
        let addus    args = cf _addus    "+"     args
        let subus    args = cf _subus    "-"     args
        let divus    args = cf _divus    "/"     args
        let absus    args = cf _absus    "abs"   args
        let modulous args = cf _modulous "%"     args

        let add      args = cf _add      "+"     args
        let sub      args = cf _sub      "-"     args
        let mul      args = cf _mul      "*"     args
        let div      args = cf _div      "/"     args
        let abs      args = cf _abs      "abs"   args
        let modulo   args = cf _modulo   "%"     args

        let muld     args = cf _muld     "*"     args
        let addd     args = cf _addd     "+"     args
        let subd     args = cf _subd     "-"     args
        let divd     args = cf _divd     "/"     args
        let absd     args = cf _absd     "abs"   args
        let modulod  args = cf _modulod  "%"     args

        let mulf     args = cf _mulf     "*"     args
        let addf     args = cf _addf     "+"     args
        let subf     args = cf _subf     "-"     args
        let divf     args = cf _divf     "/"     args
        let absf     args = cf _absf     "abs"   args
        let modulof  args = cf _modulof  "%"     args

        let mulu     args = cf _mulu     "*"     args
        let addu     args = cf _addu     "+"     args
        let subu     args = cf _subu     "-"     args
        let divu     args = cf _divu     "/"     args
        let absu     args = cf _absu     "abs"   args
        let modulou  args = cf _modulou  "%"     args


        let equal          args = cf _equal          "="      args
        let notEqual       args = cf _notEqual       "!="     args
        let gt             args = cf _gt             ">"      args
        let lt             args = cf _lt             "<"      args
        let gte            args = cf _gte            ">="     args
        let lte            args = cf _lte            "<="     args
        let equalString    args = cf _equalString    "=T"     args
        let notEqualString args = cf _notEqualString "!=T"    args
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
            open ExpressionPrologSubModule

            let private evalArg (x:obj) = (x :?> IExpression).BoxedEvaluatedValue
            let private evalToDouble x = x |> evalArg |> toDouble
            let private evalToFloat  x = x |> evalArg |> toFloat
            let private evalToByte   x = x |> evalArg |> toByte
            let private evalToSByte  x = x |> evalArg |> toSByte
            let private evalToUInt32 x = x |> evalArg |> toUInt32
            let private evalToInt16  x = x |> evalArg |> toInt16
            let private evalToUInt16 x = x |> evalArg |> toUInt16

            let _addy    (args:Args) = args.ExpectGteN(2).Select(evalToSByte).Reduce(( + ))
            let _suby    (args:Args) = args.ExpectGteN(2).Select(evalToSByte).Reduce(( - ))
            let _muly    (args:Args) = args.ExpectGteN(2).Select(evalToSByte).Reduce(( * ))
            let _divy    (args:Args) = args.ExpectGteN(2) .Select(evalToSByte).Reduce(( / ))
            let _absy    (args:Args) = args.Select(evalToSByte).Head() |> Math.Abs
            let _moduloy (args:Args) = args.ExpectGteN(2) .Select(evalToSByte).Reduce(( % ))

            let _adduy    (args:Args) = args.ExpectGteN(2).Select(evalToByte).Reduce(( + ))
            let _subuy    (args:Args) = args.ExpectGteN(2).Select(evalToByte).Reduce(( - ))
            let _muluy    (args:Args) = args.ExpectGteN(2).Select(evalToByte).Reduce(( * ))
            let _divuy    (args:Args) = args.ExpectGteN(2) .Select(evalToByte).Reduce(( / ))
            let _absuy    (args:Args) = args.Select(evalToByte).Head() |> Math.Abs
            let _modulouy (args:Args) = args.ExpectGteN(2) .Select(evalToByte).Reduce(( % ))

            let _adds    (args:Args) = args.ExpectGteN(2).Select(evalToInt16).Reduce(( + ))
            let _subs    (args:Args) = args.ExpectGteN(2).Select(evalToInt16).Reduce(( - ))
            let _muls    (args:Args) = args.ExpectGteN(2).Select(evalToInt16).Reduce(( * ))
            let _divs    (args:Args) = args.ExpectGteN(2) .Select(evalToInt16).Reduce(( / ))
            let _abss    (args:Args) = args.Select(evalToInt16).Head() |> Math.Abs
            let _modulos (args:Args) = args.ExpectGteN(2) .Select(evalToInt16).Reduce(( % ))

            let _addus    (args:Args) = args.ExpectGteN(2).Select(evalToUInt16).Reduce(( + ))
            let _subus    (args:Args) = args.ExpectGteN(2).Select(evalToUInt16).Reduce(( - ))
            let _mulus    (args:Args) = args.ExpectGteN(2).Select(evalToUInt16).Reduce(( * ))
            let _divus    (args:Args) = args.ExpectGteN(2) .Select(evalToUInt16).Reduce(( / ))
            let _absus    (args:Args) = args.Select(evalToUInt16).Head() |> Math.Abs
            let _modulous (args:Args) = args.ExpectGteN(2) .Select(evalToUInt16).Reduce(( % ))

            let _add     (args:Args) = args.ExpectGteN(2).Select(evalArg).Cast<int>().Reduce(( + ))
            let _sub     (args:Args) = args.ExpectGteN(2).Select(evalArg).Cast<int>().Reduce(( - ))
            let _mul     (args:Args) = args.ExpectGteN(2).Select(evalArg).Cast<int>().Reduce(( * ))
            let _div     (args:Args) = args.ExpectGteN(2) .Select(evalArg).Cast<int>().Reduce(( / ))
            let _abs     (args:Args) = args.Select(evalArg).Cast<int>().Head() |> Math.Abs
            let _modulo  (args:Args) = args.ExpectGteN(2) .Select(evalArg).Cast<int>().Reduce(( % ))

            let _addd    (args:Args) = args.ExpectGteN(2).Select(evalToDouble).Reduce(( + ))
            let _subd    (args:Args) = args.ExpectGteN(2).Select(evalToDouble).Reduce(( - ))
            let _muld    (args:Args) = args.ExpectGteN(2).Select(evalToDouble).Reduce(( * ))
            let _divd    (args:Args) = args.ExpectGteN(2) .Select(evalToDouble).Reduce(( / ))
            let _absd    (args:Args) = args.Select(evalToDouble).Head() |> Math.Abs
            let _modulod (args:Args) = args.ExpectGteN(2) .Select(evalToDouble).Reduce(( % ))

            let _addf    (args:Args) = args.ExpectGteN(2).Select(evalToFloat).Reduce(( + ))
            let _subf    (args:Args) = args.ExpectGteN(2).Select(evalToFloat).Reduce(( - ))
            let _mulf    (args:Args) = args.ExpectGteN(2).Select(evalToFloat).Reduce(( * ))
            let _divf    (args:Args) = args.ExpectGteN(2) .Select(evalToFloat).Reduce(( / ))
            let _absf    (args:Args) = args.Select(evalToFloat).Head() |> Math.Abs
            let _modulof (args:Args) = args.ExpectGteN(2) .Select(evalToFloat).Reduce(( % ))

            let _addu    (args:Args) = args.ExpectGteN(2).Select(evalToUInt32).Reduce(( + ))
            let _subu    (args:Args) = args.ExpectGteN(2).Select(evalToUInt32).Reduce(( - ))
            let _mulu    (args:Args) = args.ExpectGteN(2).Select(evalToUInt32).Reduce(( * ))
            let _divu    (args:Args) = args.ExpectGteN(2) .Select(evalToUInt32).Reduce(( / ))
            let _absu    (args:Args) = args.Select(evalToUInt32).Head() |> Math.Abs
            let _modulou (args:Args) = args.ExpectGteN(2) .Select(evalToUInt32).Reduce(( % ))


            let _equal   (args:Args) = args.ExpectGteN(2) .Select(evalArg) .Pairwise() .All(fun (x, y) -> isEqual x y)
            let _notEqual (args:Args) = not <| _equal args
            let _equalString (args:Args) = args.ExpectGteN(2) .Select(evalArg).Cast<string>().Distinct().Count() = 1
            let _notEqualString (args:Args) = not <| _equalString args

            let private toDoublePairwise (args:Args) = args.ExpectGteN(2).Select(evalToDouble).Pairwise()
            let _gt  (args:Args) = toDoublePairwise(args).All(fun (x, y) -> x > y)
            let _lt  (args:Args) = toDoublePairwise(args).All(fun (x, y) -> x < y)
            let _gte (args:Args) = toDoublePairwise(args).All(fun (x, y) -> x >= y)
            let _lte (args:Args) = toDoublePairwise(args).All(fun (x, y) -> x <= y)

            let _concat     (args:Args) = args.ExpectGteN(2).Select(evalArg).Cast<string>().Reduce(( + ))
            let _logicalAnd (args:Args) = args.ExpectGteN(2).Select(evalArg).Cast<bool>()  .Reduce(( && ))
            let _logicalOr  (args:Args) = args.ExpectGteN(2).Select(evalArg).Cast<bool>()  .Reduce(( || ))
            let _logicalNot (args:Args) = args.Select(evalArg).Cast<bool>().Expect1() |> not
            let _xorBit     (args:Args) = args.Select(evalArg).Cast<int>()                 .Reduce (^^^)
            let _orBit      (args:Args) = args.Select(evalArg).Cast<int>()                 .Reduce (|||)
            let _andBit     (args:Args) = args.Select(evalArg).Cast<int>()                 .Reduce (&&&)
            let _notBit     (args:Args) = args.Select(evalArg).Cast<int>().Expect1()       |> (~~~)
            let _shiftLeft  (args:Args) = args.ExpectGteN(2).Select(evalArg >> toInt)      .Reduce((<<<))
            let _shiftRight (args:Args) = args.ExpectGteN(2).Select(evalArg >> toInt)      .Reduce((>>>))

            let _sin (args:Args) = args.Select(evalToDouble) .Expect1() |> Math.Sin
            let _convertBool (args:Args) = args.Select(evalArg >> toBool) .Expect1()
            let _convertInt (args:Args) = args.Select(evalArg >> toInt) .Expect1()
    [<AutoOpen>]
    module StatementModule =
        type Statement<'T> =
            | Assign of expr:Expression<'T> * target:Tag<'T>
            member x.Do() =
                match x with
                | Assign (expr, target) -> expr.Evaluate() |> target.SetValue
            member x.ToText() =
                 match x with
                 | Assign     (expr, target) -> $"{target.ToText()} := {expr.ToText(false)}"

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
            | Tag t -> "%" + t.Name
            | Variable t -> "$" + t.Name
            | Literal v -> sprintf "%A" v

    type Expression<'T> with
        member x.GetBoxedRawObject() =
            match x with
            | Terminal b -> b.GetBoxedRawObject()
            | Function fs -> fs |> box

        member x.Evaluate() =
            match x with
            | Terminal b -> b.Evaluate()
            | Function fs -> fs.f fs.args

        member x.ToText(withParenthesys:bool) =
            match x with
            | Terminal b -> b.ToText()
            | Function fs ->
                let text = fwdSerializeFunctionNameAndBoxedArguments fs.name fs.args withParenthesys
                text


