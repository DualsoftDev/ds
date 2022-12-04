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
        verifyAllExpressionSameType [opnd1; opnd2]
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

        | ">"  -> gt  args
        | ">=" -> gte args
        | "<"  -> lt  args
        | "<=" -> lte args
        | "=" when t = "String" -> equalString args
        | "="  -> equal args

        | _ -> failwith $"NOT Yet {op}"
        |> iexpr


    let createCustomFunctionExpression (funName:string) (args:Args) : IExpression =
        verifyAllExpressionSameType args
        let t = args[0].DataType.Name

        match funName with
        | ("+" | "add") -> add args
        | ("-" | "sub") -> sub args
        | ("*" | "mul") -> mul args
        | ("/" | "div") -> div args

        | (">" | "gt") -> gt args
        | ("=" | "equal") when t = "String" -> equalString args
        | ("=" | "equal") -> equal args

        | "bool" -> cast_bool    args |> iexpr
        | ("sbyte" | "int8")     -> cast_sbyte   args |> iexpr
        | ("byte"  | "uint8")    -> cast_byte    args |> iexpr
        | ("short" | "int16")    -> cast_int16   args |> iexpr
        | ("ushort"| "uint16")   -> cast_int16   args |> iexpr
        | ("int"   | "int32")    -> cast_int32   args |> iexpr
        | ("uint"  | "uint32")   -> cast_uint32  args |> iexpr
        | ("int"   | "int64")    -> cast_int64   args |> iexpr
        | ("uint"  | "uint64")   -> cast_uint64  args |> iexpr

        | ("single") -> cast_float   args |> iexpr
        | ("double" | "float") -> cast_double  args |> iexpr

        | "sin" -> sin args |> iexpr
        | "cos" -> cos args |> iexpr
        | "tan" -> tan args |> iexpr
        | _ -> failwith $"NOT yet: {funName}"

    [<AutoOpen>]
    module FunctionModule =
        /// Create function
        let private cf (f:Args->'T) (name:string) (args:Args) =
            Function { f=f; name=name; args=args}

        (*
             .f  | Single       | single
             .   | Double       | double    float (!! 헷갈림 주이)
             y   | SByte        | int8      sbyte
             uy  | Byte         | uint8     byte
             s   | Int16        | int16
             us  | UInt16       | uint16
             -   | Int32        | int32     int
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


        let cast_bool           args = cf _convertBool    "bool"   args
        let cast_byte           args = cf _convertByte    "byte"   args
        let cast_sbyte          args = cf _convertSByte   "sbyte"  args
        let cast_int16          args = cf _convertInt16   "int16"  args
        let cast_uint16         args = cf _convertUInt16  "uint16" args
        let cast_int32          args = cf _convertInt32   "int32"  args
        let cast_uint32         args = cf _convertUInt32  "Uint32" args
        let cast_int64          args = cf _convertInt64   "int64"  args
        let cast_uint64         args = cf _convertUInt64  "Uint64" args
        let cast_float          args = cf _convertFloat   "float"  args
        let cast_double         args = cf _convertDouble  "double" args

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
        let _shiftLeft  (args:Args) = args.ExpectGteN(2).Select(evalArg >> toInt32)      .Reduce((<<<))
        let _shiftRight (args:Args) = args.ExpectGteN(2).Select(evalArg >> toInt32)      .Reduce((>>>))

        let _sin (args:Args) = args.Select(evalToDouble) .Expect1() |> Math.Sin
        let _cos (args:Args) = args.Select(evalToDouble) .Expect1() |> Math.Cos
        let _tan (args:Args) = args.Select(evalToDouble) .Expect1() |> Math.Tan
        let _convertBool (args:Args) = args.Select(evalArg >> toBool) .Expect1()

        let _convertByte   (args:Args) = args.Select(evalArg >> toByte) .Expect1()
        let _convertSByte  (args:Args) = args.Select(evalArg >> toSByte) .Expect1()
        let _convertInt16  (args:Args) = args.Select(evalArg >> toInt16) .Expect1()
        let _convertUInt16 (args:Args) = args.Select(evalArg >> toUInt16) .Expect1()
        let _convertInt32  (args:Args) = args.Select(evalArg >> toInt32) .Expect1()
        let _convertUInt32 (args:Args) = args.Select(evalArg >> toUInt32) .Expect1()
        let _convertInt64  (args:Args) = args.Select(evalArg >> toInt64) .Expect1()
        let _convertUInt64 (args:Args) = args.Select(evalArg >> toUInt64) .Expect1()



        let _convertDouble (args:Args) = args.Select(evalArg >> toDouble) .Expect1()
        let _convertFloat  (args:Args) = args.Select(evalArg >> toFloat) .Expect1()


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

