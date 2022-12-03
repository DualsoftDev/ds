namespace rec Engine.Core
open System
open System.Linq
open System.Runtime.CompilerServices
open Engine.Common.FS

[<AutoOpen>]
module ExpressionFunctionModule =

    let createBinaryExpression (opnd1:obj) (op:string) (opnd2:obj) =
        let t1 = getTypeOfBoxedExpression opnd1
        let t2 = getTypeOfBoxedExpression opnd2
        if t1 <> t2 then
            failwith "ERROR: Type mismatch"

        let args = [box opnd1; opnd2]

        if t1 = typeof<byte> then
            match op with
            | "+" -> adduy args
            | "-" -> subuy args
            | "*" -> muluy args
            | "/" -> divuy args
            | _ -> failwith "NOT Yet"
            |> box
        elif t1 = typeof<sbyte> then
            match op with
            | "+" -> addy args
            | "-" -> suby args
            | "*" -> muly args
            | "/" -> divy args
            | _ -> failwith "NOT Yet"
            |> box
        elif t1 = typeof<int16> then
            match op with
            | "+" -> adds args
            | "-" -> subs args
            | "*" -> muls args
            | "/" -> divs args
            | _ -> failwith "NOT Yet"
            |> box
        elif t1 = typeof<uint16> then
            match op with
            | "+" -> addus args
            | "-" -> subus args
            | "*" -> mulus args
            | "/" -> divus args
            | _ -> failwith "NOT Yet"
            |> box
        elif t1 = typeof<int32> then
            match op with
            | "+" -> add args
            | "-" -> sub args
            | "*" -> mul args
            | "/" -> div args
            | _ -> failwith "NOT Yet"
            |> box
        elif t1 = typeof<uint32> then
            match op with
            | "+" -> addu args
            | "-" -> subu args
            | "*" -> mulu args
            | "/" -> divu args
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
        elif t1 = typeof<single> then
            match op with
            | "+" -> addf args
            | "-" -> subf args
            | "*" -> mulf args
            | "/" -> divf args
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


    let resolve (expr:Expression<'T>) = expr.Evaluate() |> unbox


    [<AutoOpen>]
    module FunctionModule =
        ///// boxed object 로부터 Expression<'T> 생성하고 이를 obj type 으로 반환
        //let expr (x:obj) =
        //    match x with
        //    | :? IExpression as e -> x

        //    (* Parser 에서 읽은 raw literal 의 변환 case *)
        //    | :? sbyte  as o -> Terminal (Literal o)
        //    | :? byte   as o -> Terminal (Literal o)
        //    | :? int16  as o -> Terminal (Literal o)
        //    | :? uint16 as o -> Terminal (Literal o)
        //    | :? int32  as o -> Terminal (Literal o)
        //    | :? uint32 as o -> Terminal (Literal o)
        //    | :? int64  as o -> Terminal (Literal o)
        //    | :? uint64 as o -> Terminal (Literal o)
        //    | :? single as o -> Terminal (Literal o)
        //    | :? bool   as o -> Terminal (Literal o)
        //    | :? double as o -> Terminal (Literal o)
        //    | :? char   as o -> Terminal (Literal o)
        //    | :? string as o -> Terminal (Literal o)

        //    | _ -> failwith "ERROR"


        /// Create function
        let private cf (f:Args->'T) (name:string) (args:Args) =
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


    let private tagsToArguments (xs:Tag<'T> seq) = xs.Select (Tag >> box) |> List.ofSeq
    [<Extension>]
    type FuncExt =

        [<Extension>] static member ToTags (xs:#Tag<'T> seq)    = xs.Cast<Tag<_>>()
        [<Extension>] static member ToExpr (x:Tag<bool>)   = Terminal (Tag x)
        [<Extension>] static member GetAnd (xs:Tag<'T> seq)  = xs |> tagsToArguments |> anD
        [<Extension>] static member GetOr  (xs:Tag<'T> seq)  = xs |> tagsToArguments |> oR
        //[sets and]--|----- ! [rsts or] ----- (relay)
        //|relay|-----|
        [<Extension>] static member GetRelayExpr(sets:Tag<bool> seq, rsts:Tag<bool> seq, relay:Tag<bool>) =
                        (sets.GetAnd() <||> relay.ToExpr()) <&&> (!! rsts.GetOr())

        //[sets and]--|----- ! [rsts or] ----- (coil)
        [<Extension>] static member GetNoRelayExpr(sets:Tag<'T> seq, rsts:Tag<'T> seq) =
                        sets.GetAnd() <&&> (!! rsts.GetOr())

        //[sets and]--|-----  [rsts and] ----- (relay)
        //|relay|-----|
        [<Extension>] static member GetRelayExprReverseReset(sets:Tag<'T> seq, rsts:Tag<'T> seq, relay:Tag<bool>) =
                        (sets.GetAnd() <||> relay.ToExpr()) <&&> (rsts.GetOr())


    [<AutoOpen>]
    module ExpressionOperatorModule =
        /// boolean AND operator
        let (<&&>) (left: Expression<bool>) (right: Expression<bool>) = anD [ left; right ]
        /// boolean OR operator
        let (<||>) (left: Expression<bool>) (right: Expression<bool>) = oR [ left; right ]
        /// boolean NOT operator
        let (!!)   (exp: Expression<bool>) = noT [exp]
        /// Assign statement
        let (<==)  (storage: IStorage<'T>) (exp: Expression<'T>) = Assign(exp, storage)

