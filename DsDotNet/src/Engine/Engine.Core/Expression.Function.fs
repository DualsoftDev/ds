namespace rec Engine.Core
open System
open System.Linq
open System.Runtime.CompilerServices
open Engine.Common.FS
open ExpressionPrologModule.ExpressionPrologSubModule

[<AutoOpen>]
module ExpressionFunctionModule =

    /// Expression<'T> 를 IExpression 으로 casting
    let internal iexpr any = (box any) :?> IExpression

    let createBinaryExpression (opnd1:IExpression) (op:string) (opnd2:IExpression) : IExpression =
        verifyAllExpressionSameType [opnd1; opnd2] |> ignore
        let t1 = opnd1.DataType
        let t2 = opnd2.DataType
        if t1 <> t2 then
            failwith "ERROR: Type mismatch"
        let t = t1.Name
        let args = [opnd1; opnd2]

        match op with
        | "+" when t = "String" -> concat args
        | "+" -> add args
        | "-" -> sub args
        | "*" -> mul args
        | "/" -> div args
        | _ -> failwith "NOT Yet"
        |> iexpr


    let createCustomFunctionExpression (funName:string) (args:Args) : IExpression =
        verifyAllExpressionSameType args |> ignore
        match funName with
        | ("+" | "add") -> add args
        | ("-" | "sub") -> sub args
        | ("*" | "mul") -> mul args
        | ("/" | "div") -> div args

        | (">" | "gt") -> gt args

        | "Int"  -> Int  args |> iexpr
        | "Bool" -> Bool args |> iexpr

        | "sin"  -> sin  args |> iexpr
        | "cos" -> cos args |> iexpr
        | "tan" -> tan args |> iexpr
        | _ -> failwith "NOT yet"

    [<AutoOpen>]
    module FunctionModule =
        /// Create function
        let private cf (f:Args->'T) (name:string) (args:Args) =
            Function { f=f; name=name; args=args}

        (*
             .f  | Single       | single
             .   | Double       | double
             y   | SByte        | sbyte
             uy  | Byte         | byte
             s   | Int16        | int16
             us  | UInt16       | uint16
             -   | Int32        | int32
             u   | UInt32       | uint32
             L   | Int64        | int64
             UL  | UInt64       | uint64
        *)
        let add (args:Args) =
            match args[0].DataType.Name with
            | "Single" -> cf _add   "+" args |> iexpr
            | "Double" -> cf _addd  "+" args |> iexpr
            | "SByte"  -> cf _addy  "+" args |> iexpr
            | "Byte"   -> cf _adduy "+" args |> iexpr
            | "Int16"  -> cf _adds  "+" args |> iexpr
            | "UInt16" -> cf _addus "+" args |> iexpr
            | "Int32"  -> cf _add   "+" args |> iexpr
            | "UInt32" -> cf _addu  "+" args |> iexpr
            | "Int64"  -> cf _addL  "+" args |> iexpr
            | "UInt64" -> cf _addUL "+" args |> iexpr
            | _        -> failwith "ERROR"

        let sub (args:Args) =
            match args[0].DataType.Name with
            | "Single" -> cf _sub   "-" args |> iexpr
            | "Double" -> cf _subd  "-" args |> iexpr
            | "SByte"  -> cf _suby  "-" args |> iexpr
            | "Byte"   -> cf _subuy "-" args |> iexpr
            | "Int16"  -> cf _subs  "-" args |> iexpr
            | "UInt16" -> cf _subus "-" args |> iexpr
            | "Int32"  -> cf _sub   "-" args |> iexpr
            | "UInt32" -> cf _subu  "-" args |> iexpr
            | "Int64"  -> cf _subL  "-" args |> iexpr
            | "UInt64" -> cf _subUL "-" args |> iexpr
            | _        -> failwith "ERROR"

        let mul (args:Args) =
            match args[0].DataType.Name with
            | "Single" -> cf _mul   "*" args |> iexpr
            | "Double" -> cf _muld  "*" args |> iexpr
            | "SByte"  -> cf _muly  "*" args |> iexpr
            | "Byte"   -> cf _muluy "*" args |> iexpr
            | "Int16"  -> cf _muls  "*" args |> iexpr
            | "UInt16" -> cf _mulus "*" args |> iexpr
            | "Int32"  -> cf _mul   "*" args |> iexpr
            | "UInt32" -> cf _mulu  "*" args |> iexpr
            | "Int64"  -> cf _mulL  "*" args |> iexpr
            | "UInt64" -> cf _mulUL "*" args |> iexpr
            | _        -> failwith "ERROR"

        let div (args:Args) =
            match args[0].DataType.Name with
            | "Single" -> cf _div   "/" args |> iexpr
            | "Double" -> cf _divd  "/" args |> iexpr
            | "SByte"  -> cf _divy  "/" args |> iexpr
            | "Byte"   -> cf _divuy "/" args |> iexpr
            | "Int16"  -> cf _divs  "/" args |> iexpr
            | "UInt16" -> cf _divus "/" args |> iexpr
            | "Int32"  -> cf _div   "/" args |> iexpr
            | "UInt32" -> cf _divu  "/" args |> iexpr
            | "Int64"  -> cf _divL  "/" args |> iexpr
            | "UInt64" -> cf _divUL "/" args |> iexpr
            | _        -> failwith "ERROR"

        let abs (args:Args) =
            match args[0].DataType.Name with
            | "Single" -> cf _abs   "abs" args |> iexpr
            | "Double" -> cf _absd  "abs" args |> iexpr
            | "SByte"  -> cf _absy  "abs" args |> iexpr
            | "Byte"   -> cf _absuy "abs" args |> iexpr
            | "Int16"  -> cf _abss  "abs" args |> iexpr
            | "UInt16" -> cf _absus "abs" args |> iexpr
            | "Int32"  -> cf _abs   "abs" args |> iexpr
            | "UInt32" -> cf _absu  "abs" args |> iexpr
            | "Int64"  -> cf _absL  "abs" args |> iexpr
            | "UInt64" -> cf _absUL "abs" args |> iexpr
            | _        -> failwith "ERROR"

        let modulo (args:Args) =
            match args[0].DataType.Name with
            | "Single" -> cf _modulo   "%" args |> iexpr
            | "Double" -> cf _modulod  "%" args |> iexpr
            | "SByte"  -> cf _moduloy  "%" args |> iexpr
            | "Byte"   -> cf _modulouy "%" args |> iexpr
            | "Int16"  -> cf _modulos  "%" args |> iexpr
            | "UInt16" -> cf _modulous "%" args |> iexpr
            | "Int32"  -> cf _modulo   "%" args |> iexpr
            | "UInt32" -> cf _modulou  "%" args |> iexpr
            | "Int64"  -> cf _moduloL  "%" args |> iexpr
            | "UInt64" -> cf _moduloUL "%" args |> iexpr
            | _        -> failwith "ERROR"

        //let gt (args:Args) =
        //    match args[0].DataType.Name with
        //    | "Single"
        //    | "Double"
        //    | "SByte"
        //    | "Byte"
        //    | "Int16"
        //    | "UInt16"
        //    | "Int32"
        //    | "UInt32"
        //    | "Int64"
        //    | "UInt64"  ->
        //        let doubleArgs = args.Select(fun a -> a.BoxedEvaluatedValue |> toDouble
        //        cf _gt ">" doubleArgs


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
        let cos            args = cf _cos            "cos"    args
        let tan            args = cf _tan            "tan"    args
        let Bool           args = cf _convertBool    "Bool"   args
        let Int            args = cf _convertInt     "Int"    args

        let anD = logicalAnd
        //let absDouble = absd
        let oR = logicalOr
        let noT = logicalNot
        //let divDouble = divd
        let addString = concat


    [<AutoOpen>]
    module internal FunctionImpl =
        open ExpressionPrologSubModule

        let private evalArg (x:IExpression) = x.BoxedEvaluatedValue
        let private castTo<'T> (x:obj) = x :?> 'T
        let private evalToDouble x = x |> evalArg |> castTo<double>
        let private evalToFloat  x = x |> evalArg |> castTo<single>
        let private evalToByte   x = x |> evalArg |> castTo<byte>
        let private evalToSByte  x = x |> evalArg |> castTo<sbyte>
        let private evalToInt16  x = x |> evalArg |> castTo<int16>
        let private evalToUInt16 x = x |> evalArg |> castTo<uint16>
        let private evalToInt32  x = x |> evalArg |> castTo<int32>
        let private evalToUInt32 x = x |> evalArg |> castTo<uint32>
        let private evalToInt64  x = x |> evalArg |> castTo<int64>
        let private evalToUInt64 x = x |> evalArg |> castTo<uint64>

        let _addy    (args:Args) = args.ExpectGteN(2).Select(evalToSByte).Reduce(( + ))
        let _suby    (args:Args) = args.ExpectGteN(2).Select(evalToSByte).Reduce(( - ))
        let _muly    (args:Args) = args.ExpectGteN(2).Select(evalToSByte).Reduce(( * ))
        let _divy    (args:Args) = args.ExpectGteN(2) .Select(evalToSByte).Reduce(( / ))
        let _moduloy (args:Args) = args.ExpectGteN(2) .Select(evalToSByte).Reduce(( % ))

        let _adduy    (args:Args) = args.ExpectGteN(2).Select(evalToByte).Reduce(( + ))
        let _subuy    (args:Args) = args.ExpectGteN(2).Select(evalToByte).Reduce(( - ))
        let _muluy    (args:Args) = args.ExpectGteN(2).Select(evalToByte).Reduce(( * ))
        let _divuy    (args:Args) = args.ExpectGteN(2) .Select(evalToByte).Reduce(( / ))
        let _modulouy (args:Args) = args.ExpectGteN(2) .Select(evalToByte).Reduce(( % ))

        let _adds    (args:Args) = args.ExpectGteN(2).Select(evalToInt16).Reduce(( + ))
        let _subs    (args:Args) = args.ExpectGteN(2).Select(evalToInt16).Reduce(( - ))
        let _muls    (args:Args) = args.ExpectGteN(2).Select(evalToInt16).Reduce(( * ))
        let _divs    (args:Args) = args.ExpectGteN(2) .Select(evalToInt16).Reduce(( / ))
        let _modulos (args:Args) = args.ExpectGteN(2) .Select(evalToInt16).Reduce(( % ))

        let _addus    (args:Args) = args.ExpectGteN(2).Select(evalToUInt16).Reduce(( + ))
        let _subus    (args:Args) = args.ExpectGteN(2).Select(evalToUInt16).Reduce(( - ))
        let _mulus    (args:Args) = args.ExpectGteN(2).Select(evalToUInt16).Reduce(( * ))
        let _divus    (args:Args) = args.ExpectGteN(2) .Select(evalToUInt16).Reduce(( / ))
        let _modulous (args:Args) = args.ExpectGteN(2) .Select(evalToUInt16).Reduce(( % ))

        let _add     (args:Args) = args.ExpectGteN(2).Select(evalToInt32).Reduce(( + ))
        let _sub     (args:Args) = args.ExpectGteN(2).Select(evalToInt32).Reduce(( - ))
        let _mul     (args:Args) = args.ExpectGteN(2).Select(evalToInt32).Reduce(( * ))
        let _div     (args:Args) = args.ExpectGteN(2) .Select(evalToInt32).Reduce(( / ))
        let _modulo  (args:Args) = args.ExpectGteN(2) .Select(evalToInt32).Reduce(( % ))

        let _addd    (args:Args) = args.ExpectGteN(2).Select(evalToDouble).Reduce(( + ))
        let _subd    (args:Args) = args.ExpectGteN(2).Select(evalToDouble).Reduce(( - ))
        let _muld    (args:Args) = args.ExpectGteN(2).Select(evalToDouble).Reduce(( * ))
        let _divd    (args:Args) = args.ExpectGteN(2) .Select(evalToDouble).Reduce(( / ))
        let _modulod (args:Args) = args.ExpectGteN(2) .Select(evalToDouble).Reduce(( % ))

        let _addf    (args:Args) = args.ExpectGteN(2).Select(evalToFloat).Reduce(( + ))
        let _subf    (args:Args) = args.ExpectGteN(2).Select(evalToFloat).Reduce(( - ))
        let _mulf    (args:Args) = args.ExpectGteN(2).Select(evalToFloat).Reduce(( * ))
        let _divf    (args:Args) = args.ExpectGteN(2) .Select(evalToFloat).Reduce(( / ))
        let _modulof (args:Args) = args.ExpectGteN(2) .Select(evalToFloat).Reduce(( % ))

        let _addu    (args:Args) = args.ExpectGteN(2).Select(evalToUInt32).Reduce(( + ))
        let _subu    (args:Args) = args.ExpectGteN(2).Select(evalToUInt32).Reduce(( - ))
        let _mulu    (args:Args) = args.ExpectGteN(2).Select(evalToUInt32).Reduce(( * ))
        let _divu    (args:Args) = args.ExpectGteN(2) .Select(evalToUInt32).Reduce(( / ))
        let _modulou (args:Args) = args.ExpectGteN(2) .Select(evalToUInt32).Reduce(( % ))

        let _addL    (args:Args) = args.ExpectGteN(2).Select(evalToInt64).Reduce(( + ))
        let _subL    (args:Args) = args.ExpectGteN(2).Select(evalToInt64).Reduce(( - ))
        let _mulL    (args:Args) = args.ExpectGteN(2).Select(evalToInt64).Reduce(( * ))
        let _divL    (args:Args) = args.ExpectGteN(2) .Select(evalToInt64).Reduce(( / ))
        let _moduloL (args:Args) = args.ExpectGteN(2) .Select(evalToInt64).Reduce(( % ))

        let _addUL    (args:Args) = args.ExpectGteN(2).Select(evalToUInt64).Reduce(( + ))
        let _subUL    (args:Args) = args.ExpectGteN(2).Select(evalToUInt64).Reduce(( - ))
        let _mulUL    (args:Args) = args.ExpectGteN(2).Select(evalToUInt64).Reduce(( * ))
        let _divUL    (args:Args) = args.ExpectGteN(2) .Select(evalToUInt64).Reduce(( / ))
        let _moduloUL (args:Args) = args.ExpectGteN(2) .Select(evalToUInt64).Reduce(( % ))

        let _absy  (args:Args) = evalToSByte  (args.ExactlyOne()) |> Math.Abs
        let _absuy (args:Args) = evalToByte   (args.ExactlyOne()) |> Math.Abs
        let _abss  (args:Args) = evalToInt16  (args.ExactlyOne()) |> Math.Abs
        let _absus (args:Args) = evalToUInt16 (args.ExactlyOne()) |> Math.Abs
        let _abs   (args:Args) = evalToInt32  (args.ExactlyOne()) |> Math.Abs
        let _absd  (args:Args) = evalToDouble (args.ExactlyOne()) |> Math.Abs
        let _absf  (args:Args) = evalToFloat  (args.ExactlyOne()) |> Math.Abs
        let _absu  (args:Args) = evalToUInt32 (args.ExactlyOne()) |> Math.Abs
        let _absL  (args:Args) = evalToInt64  (args.ExactlyOne()) |> Math.Abs
        let _absUL (args:Args) = evalToUInt64 (args.ExactlyOne()) |> Math.Abs



        let _equal   (args:Args) = args.ExpectGteN(2) .Select(evalArg) .Pairwise() .All(fun (x, y) -> isEqual x y)
        let _notEqual (args:Args) = not <| _equal args
        let _equalString (args:Args) = args.ExpectGteN(2) .Select(evalArg).Cast<string>().Distinct().Count() = 1
        let _notEqualString (args:Args) = not <| _equalString args

        let private convertToDoublePair (args:Args) = args.ExpectGteN(2).Select(fun x -> x.BoxedEvaluatedValue |> toDouble).Pairwise()
        let _gt  (args:Args) = convertToDoublePair(args).All(fun (x, y) -> x > y)
        let _lt  (args:Args) = convertToDoublePair(args).All(fun (x, y) -> x < y)
        let _gte (args:Args) = convertToDoublePair(args).All(fun (x, y) -> x >= y)
        let _lte (args:Args) = convertToDoublePair(args).All(fun (x, y) -> x <= y)

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
        let _cos (args:Args) = args.Select(evalToDouble) .Expect1() |> Math.Cos
        let _tan (args:Args) = args.Select(evalToDouble) .Expect1() |> Math.Tan
        let _convertBool (args:Args) = args.Select(evalArg >> toBool) .Expect1()
        let _convertInt (args:Args) = args.Select(evalArg >> toInt) .Expect1()


    let private tagsToArguments (xs:Tag<'T> seq) = xs.Select(fun x -> Tag x) |> List.ofSeq
    [<Extension>]
    type FuncExt =

        [<Extension>] static member ToTags (xs:#Tag<'T> seq)    = xs.Cast<Tag<_>>()
        [<Extension>] static member ToExpr (x:Tag<bool>)   = Terminal (Tag x)
        [<Extension>] static member GetAnd (xs:Tag<'T> seq)  = xs |> tagsToArguments |> List.cast<IExpression> |> anD
        [<Extension>] static member GetOr  (xs:Tag<'T> seq)  = xs |> tagsToArguments |> List.cast<IExpression>|> oR
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
        let (<==)  (storage: IStorage<'T>) (exp: IExpression) = Assign(exp, storage)

