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
    let private evalArg (x:IExpression) = x.BoxedEvaluatedValue
    let private castTo<'T> (x:obj) = x :?> 'T
    let private evalTo<'T> (x:IExpression) = x |> evalArg |> castTo<'T>

    let createBinaryExpression (opnd1:IExpression) (op:string) (opnd2:IExpression) : IExpression =
        let t1 = opnd1.DataType
        let t2 = opnd2.DataType

        match op with
        | (   "+" | "+" | "-" | "*" | "/"
            | ">" | ">=" | "<" | "<=" | "=" | "=" ) ->
            verifyAllExpressionSameType [opnd1; opnd2]
            if t1 <> t2 then
                failwith "ERROR: Type mismatch"
        | _ ->
            ()

        let t = t1.Name
        let args = [opnd1; opnd2]

        match op with
        | "+" when t = "String" -> fConcat args
        | "+" -> fAdd args
        | "-" -> fSub args
        | "*" -> fMul args
        | "/" -> fDiv args

        | ">"  -> fGt  args
        | ">=" -> fGte args
        | "<"  -> fLt  args
        | "<=" -> fLte args
        | "=" when t = "String" -> fEqualString args
        | "="  -> fEqual args

        | ("<<<" | "<<") -> fShiftLeft  args
        | (">>>" | ">>") -> fShiftRight args

        | ("&&&" | "&") ->  fBitwiseAnd args
        | ("|||" | "|") ->  fBitwiseOr  args
        | ("~~~" | "~") ->  fBitwiseNot args

        | _ -> failwith $"NOT Yet {op}"
        |> iexpr

    let createUnaryExpression (op:string) (opnd:IExpression) : IExpression =
        match op with
        | ("~" | "~~~" ) -> fBitwiseNot [opnd]

    let createCustomFunctionExpression (funName:string) (args:Args) : IExpression =
        verifyAllExpressionSameType args
        let t = args[0].DataType.Name

        match funName with
        | ("+" | "add") -> fAdd args
        | ("-" | "sub") -> fSub args
        | ("*" | "mul") -> fMul args
        | ("/" | "div") -> fDiv args

        | (">"  | "gt")  -> fGt args
        | (">=" | "gte") -> fGte args
        | ("<"  | "lt")  -> fLt args
        | ("<=" | "lte") -> fLte args

        | ("="  | "equal") when t = "String" -> fEqualString args
        | ("="  | "equal") -> fEqual args
        | ("!=" | "notEqual") when t = "String" -> fNotEqualString args
        | ("!=" | "notEqual") -> fNotEqual args

        | ("<<" | "<<<" | "shiftLeft") -> fShiftLeft args
        | (">>" | ">>>" | "shiftRight") -> fShiftLeft args

        | ("&&" | "and") -> fLogicalAnd args
        | ("||" | "or")  -> fLogicalOr args
        | ("!"  | "not") -> fLogicalNot args

        | ("&" | "&&&") -> fBitwiseAnd args
        | ("|" | "|||") -> fBitwiseOr args
        | ("~" | "~~~") -> fBitwiseNot args

        | "toBool" -> fCastBool    args |> iexpr
        | ("toSByte" | "toInt8")     -> fCastInt8     args |> iexpr
        | ("toByte"  | "toUInt8")    -> fCastUInt8    args |> iexpr
        | ("toShort" | "toInt16")    -> fCastInt16    args |> iexpr
        | ("toUShort"| "toUInt16")   -> fCastInt16    args |> iexpr
        | ("toInt"   | "toInt32")    -> fCastInt32    args |> iexpr
        | ("toUInt"  | "toUInt32")   -> fCastUInt32   args |> iexpr
        | ("toLong"  | "toInt64")    -> fCastInt64    args |> iexpr
        | ("toULong" | "toUInt64")   -> fCastUInt64   args |> iexpr

        | ("toSingle"| "toFloat" | "toFloat32") -> fCastFloat32 args |> iexpr
        | ("toDouble"| "toFloat64" ) -> fCastFloat64  args |> iexpr

        | "sin" -> fSin args |> iexpr
        | "cos" -> fCos args |> iexpr
        | "tan" -> fTan args |> iexpr
        | _ -> failwith $"NOT yet: {funName}"

    [<AutoOpen>]
    module FunctionModule =
        /// Create function
        let private cf (f:Args->'T) (name:string) (args:Args) =
            Function { FunctionBody=f; Name=name; Arguments=args}

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
        let fAdd (args:Args) : IExpression =
            match args[0].DataType.Name with
            | "Single" -> cf _addf  "+" args
            | "Double" -> cf _addd  "+" args
            | "SByte"  -> cf _addy  "+" args
            | "Byte"   -> cf _adduy "+" args
            | "Int16"  -> cf _adds  "+" args
            | "UInt16" -> cf _addus "+" args
            | "Int32"  -> cf _add   "+" args
            | "UInt32" -> cf _addu  "+" args
            | "Int64"  -> cf _addL  "+" args
            | "UInt64" -> cf _addUL "+" args
            | _        -> failwith "ERROR"

        let fSub (args:Args) : IExpression =
            match args[0].DataType.Name with
            | "Single" -> cf _subf  "-" args
            | "Double" -> cf _subd  "-" args
            | "SByte"  -> cf _suby  "-" args
            | "Byte"   -> cf _subuy "-" args
            | "Int16"  -> cf _subs  "-" args
            | "UInt16" -> cf _subus "-" args
            | "Int32"  -> cf _sub   "-" args
            | "UInt32" -> cf _subu  "-" args
            | "Int64"  -> cf _subL  "-" args
            | "UInt64" -> cf _subUL "-" args
            | _        -> failwith "ERROR"

        let fMul (args:Args) : IExpression =
            match args[0].DataType.Name with
            | "Single" -> cf _mulf  "*" args
            | "Double" -> cf _muld  "*" args
            | "SByte"  -> cf _muly  "*" args
            | "Byte"   -> cf _muluy "*" args
            | "Int16"  -> cf _muls  "*" args
            | "UInt16" -> cf _mulus "*" args
            | "Int32"  -> cf _mul   "*" args
            | "UInt32" -> cf _mulu  "*" args
            | "Int64"  -> cf _mulL  "*" args
            | "UInt64" -> cf _mulUL "*" args
            | _        -> failwith "ERROR"

        let fDiv (args:Args) : IExpression =
            match args[0].DataType.Name with
            | "Single" -> cf _divf  "/" args
            | "Double" -> cf _divd  "/" args
            | "SByte"  -> cf _divy  "/" args
            | "Byte"   -> cf _divuy "/" args
            | "Int16"  -> cf _divs  "/" args
            | "UInt16" -> cf _divus "/" args
            | "Int32"  -> cf _div   "/" args
            | "UInt32" -> cf _divu  "/" args
            | "Int64"  -> cf _divL  "/" args
            | "UInt64" -> cf _divUL "/" args
            | _        -> failwith "ERROR"

        let fAbs (args:Args) : IExpression =
            match args[0].DataType.Name with
            | "Single" -> cf _absf  "abs" args
            | "Double" -> cf _absd  "abs" args
            | "SByte"  -> cf _absy  "abs" args
            | "Byte"   -> cf _absuy "abs" args
            | "Int16"  -> cf _abss  "abs" args
            | "UInt16" -> cf _absus "abs" args
            | "Int32"  -> cf _abs   "abs" args
            | "UInt32" -> cf _absu  "abs" args
            | "Int64"  -> cf _absL  "abs" args
            | "UInt64" -> cf _absUL "abs" args
            | _        -> failwith "ERROR"

        let fMod (args:Args) : IExpression =
            match args[0].DataType.Name with
            | "Single" -> cf _modulof  "%" args
            | "Double" -> cf _modulod  "%" args
            | "SByte"  -> cf _moduloy  "%" args
            | "Byte"   -> cf _modulouy "%" args
            | "Int16"  -> cf _modulos  "%" args
            | "UInt16" -> cf _modulous "%" args
            | "Int32"  -> cf _modulo   "%" args
            | "UInt32" -> cf _modulou  "%" args
            | "Int64"  -> cf _moduloL  "%" args
            | "UInt64" -> cf _moduloUL "%" args
            | _        -> failwith "ERROR"


        let fShiftLeft (args:Args) : IExpression =
            match args[0].DataType.Name with
            | "SByte"  -> cf _shiftLeftInt8    "<<<" args
            | "Byte"   -> cf _shiftLeftUInt8   "<<<" args
            | "Int16"  -> cf _shiftLeftInt16   "<<<" args
            | "UInt16" -> cf _shiftLeftUInt16  "<<<" args
            | "Int32"  -> cf _shiftLeftInt32   "<<<" args
            | "UInt32" -> cf _shiftLeftUInt32  "<<<" args
            | "Int64"  -> cf _shiftLeftInt64   "<<<" args
            | "UInt64" -> cf _shiftLeftUInt64  "<<<" args
            | _        -> failwith "ERROR"

        let fShiftRight (args:Args) : IExpression =
            match args[0].DataType.Name with
            | "SByte"  -> cf _shiftRightInt8    ">>>" args
            | "Byte"   -> cf _shiftRightUInt8   ">>>" args
            | "Int16"  -> cf _shiftRightInt16   ">>>" args
            | "UInt16" -> cf _shiftRightUInt16  ">>>" args
            | "Int32"  -> cf _shiftRightInt32   ">>>" args
            | "UInt32" -> cf _shiftRightUInt32  ">>>" args
            | "Int64"  -> cf _shiftRightInt64   ">>>" args
            | "UInt64" -> cf _shiftRightUInt64  ">>>" args
            | _        -> failwith "ERROR"


        let fConcat         args = cf _concat         "+"      args

        let fEqual          args: Expression<bool> = cf _equal          "="  args
        let fNotEqual       args: Expression<bool> = cf _notEqual       "!=" args
        let fGt             args: Expression<bool> = cf _gt             ">"  args
        let fLt             args: Expression<bool> = cf _lt             "<"  args
        let fGte            args: Expression<bool> = cf _gte            ">=" args
        let fLte            args: Expression<bool> = cf _lte            "<=" args
        let fEqualString    args: Expression<bool> = cf _equalString    "="  args
        let fNotEqualString args: Expression<bool> = cf _notEqualString "!=" args
        let fLogicalAnd     args: Expression<bool> = cf _logicalAnd     "&&" args
        let fLogicalOr      args: Expression<bool> = cf _logicalOr      "||" args
        let fLogicalNot     args: Expression<bool> = cf _logicalNot     "!"  args
        let fBitwiseOr      args = cf _orBit          "orBit"  args
        let fBitwiseAnd     args = cf _andBit         "andBit" args
        let fBitwiseNot     args = cf _notBit         "notBit" args
        let fBitwiseXor     args = cf _xorBit         "xorBit" args
        let fSin            args = cf _sin            "sin"    args
        let fCos            args = cf _cos            "cos"    args
        let fTan            args = cf _tan            "tan"    args


        let fCastBool       args = cf _castToBool     "bool"   args
        let fCastUInt8      args = cf _castToUInt8    "byte"   args
        let fCastInt8       args = cf _castToInt8     "sbyte"  args
        let fCastInt16      args = cf _castToInt16    "int16"  args
        let fCastUInt16     args = cf _castToUInt16   "uint16" args
        let fCastInt32      args = cf _castToInt32    "int32"  args
        let fCastUInt32     args = cf _castToUInt32   "Uint32" args
        let fCastInt64      args = cf _castToInt64    "int64"  args
        let fCastUInt64     args = cf _castToUInt64   "Uint64" args
        let fCastFloat32    args = cf _castToFloat32   "float"  args
        let fCastFloat64    args = cf _castToFloat64   "double" args

        let anD = fLogicalAnd
        //let absDouble = absd
        let oR = fLogicalOr
        let noT = fLogicalNot
        //let divDouble = divd
        let addString = fConcat


    [<AutoOpen>]
    module internal FunctionImpl =
        open ExpressionPrologSubModule
        [<Extension>] // type SeqExt =
        type SeqExt =
            [<Extension>] static member ExpectGteN(xs:'a seq, n) = expectGteN n xs; xs
            [<Extension>] static member Expect1(xs:'a seq) = expect1 xs
            [<Extension>] static member Expect2(xs:'a seq) = expect2 xs
            [<Extension>]
            static member ExpectTyped2<'U, 'V>(Array(xs:IExpression [])) =
                let arg0 = xs[0] |> evalTo<'U>
                let arg1 = xs[1] |> evalTo<'V>
                arg0, arg1


        let _addy    (args:Args) = args.ExpectGteN(2).Select(evalTo<int8>).Reduce(( + ))
        let _suby    (args:Args) = args.ExpectGteN(2).Select(evalTo<int8>).Reduce(( - ))
        let _muly    (args:Args) = args.ExpectGteN(2).Select(evalTo<int8>).Reduce(( * ))
        let _divy    (args:Args) = args.ExpectGteN(2) .Select(evalTo<int8>).Reduce(( / ))
        let _moduloy (args:Args) = args.ExpectGteN(2) .Select(evalTo<int8>).Reduce(( % ))

        let _adduy    (args:Args) = args.ExpectGteN(2).Select(evalTo<uint8>).Reduce(( + ))
        let _subuy    (args:Args) = args.ExpectGteN(2).Select(evalTo<uint8>).Reduce(( - ))
        let _muluy    (args:Args) = args.ExpectGteN(2).Select(evalTo<uint8>).Reduce(( * ))
        let _divuy    (args:Args) = args.ExpectGteN(2) .Select(evalTo<uint8>).Reduce(( / ))
        let _modulouy (args:Args) = args.ExpectGteN(2) .Select(evalTo<uint8>).Reduce(( % ))

        let _adds    (args:Args) = args.ExpectGteN(2).Select(evalTo<int16>).Reduce(( + ))
        let _subs    (args:Args) = args.ExpectGteN(2).Select(evalTo<int16>).Reduce(( - ))
        let _muls    (args:Args) = args.ExpectGteN(2).Select(evalTo<int16>).Reduce(( * ))
        let _divs    (args:Args) = args.ExpectGteN(2) .Select(evalTo<int16>).Reduce(( / ))
        let _modulos (args:Args) = args.ExpectGteN(2) .Select(evalTo<int16>).Reduce(( % ))

        let _addus    (args:Args) = args.ExpectGteN(2).Select(evalTo<uint16>).Reduce(( + ))
        let _subus    (args:Args) = args.ExpectGteN(2).Select(evalTo<uint16>).Reduce(( - ))
        let _mulus    (args:Args) = args.ExpectGteN(2).Select(evalTo<uint16>).Reduce(( * ))
        let _divus    (args:Args) = args.ExpectGteN(2) .Select(evalTo<uint16>).Reduce(( / ))
        let _modulous (args:Args) = args.ExpectGteN(2) .Select(evalTo<uint16>).Reduce(( % ))

        let _add     (args:Args) = args.ExpectGteN(2).Select(evalTo<int32>).Reduce(( + ))
        let _sub     (args:Args) = args.ExpectGteN(2).Select(evalTo<int32>).Reduce(( - ))
        let _mul     (args:Args) = args.ExpectGteN(2).Select(evalTo<int32>).Reduce(( * ))
        let _div     (args:Args) = args.ExpectGteN(2) .Select(evalTo<int32>).Reduce(( / ))
        let _modulo  (args:Args) = args.ExpectGteN(2) .Select(evalTo<int32>).Reduce(( % ))

        let _addd    (args:Args) = args.ExpectGteN(2).Select(evalTo<double>).Reduce(( + ))
        let _subd    (args:Args) = args.ExpectGteN(2).Select(evalTo<double>).Reduce(( - ))
        let _muld    (args:Args) = args.ExpectGteN(2).Select(evalTo<double>).Reduce(( * ))
        let _divd    (args:Args) = args.ExpectGteN(2) .Select(evalTo<double>).Reduce(( / ))
        let _modulod (args:Args) = args.ExpectGteN(2) .Select(evalTo<double>).Reduce(( % ))

        let _addf    (args:Args) = args.ExpectGteN(2).Select(evalTo<single>).Reduce(( + ))
        let _subf    (args:Args) = args.ExpectGteN(2).Select(evalTo<single>).Reduce(( - ))
        let _mulf    (args:Args) = args.ExpectGteN(2).Select(evalTo<single>).Reduce(( * ))
        let _divf    (args:Args) = args.ExpectGteN(2) .Select(evalTo<single>).Reduce(( / ))
        let _modulof (args:Args) = args.ExpectGteN(2) .Select(evalTo<single>).Reduce(( % ))

        let _addu    (args:Args) = args.ExpectGteN(2).Select(evalTo<uint32>).Reduce(( + ))
        let _subu    (args:Args) = args.ExpectGteN(2).Select(evalTo<uint32>).Reduce(( - ))
        let _mulu    (args:Args) = args.ExpectGteN(2).Select(evalTo<uint32>).Reduce(( * ))
        let _divu    (args:Args) = args.ExpectGteN(2) .Select(evalTo<uint32>).Reduce(( / ))
        let _modulou (args:Args) = args.ExpectGteN(2) .Select(evalTo<uint32>).Reduce(( % ))

        let _addL    (args:Args) = args.ExpectGteN(2).Select(evalTo<int64>).Reduce(( + ))
        let _subL    (args:Args) = args.ExpectGteN(2).Select(evalTo<int64>).Reduce(( - ))
        let _mulL    (args:Args) = args.ExpectGteN(2).Select(evalTo<int64>).Reduce(( * ))
        let _divL    (args:Args) = args.ExpectGteN(2) .Select(evalTo<int64>).Reduce(( / ))
        let _moduloL (args:Args) = args.ExpectGteN(2) .Select(evalTo<int64>).Reduce(( % ))

        let _addUL    (args:Args) = args.ExpectGteN(2).Select(evalTo<uint64>).Reduce(( + ))
        let _subUL    (args:Args) = args.ExpectGteN(2).Select(evalTo<uint64>).Reduce(( - ))
        let _mulUL    (args:Args) = args.ExpectGteN(2).Select(evalTo<uint64>).Reduce(( * ))
        let _divUL    (args:Args) = args.ExpectGteN(2) .Select(evalTo<uint64>).Reduce(( / ))
        let _moduloUL (args:Args) = args.ExpectGteN(2) .Select(evalTo<uint64>).Reduce(( % ))

        let _absy  (args:Args) = evalTo<int8> (args.ExactlyOne()) |> Math.Abs
        let _absuy (args:Args) = evalTo<uint8> (args.ExactlyOne()) |> Math.Abs
        let _abss  (args:Args) = evalTo<int16 > (args.ExactlyOne()) |> Math.Abs
        let _absus (args:Args) = evalTo<uint16> (args.ExactlyOne()) |> Math.Abs
        let _abs   (args:Args) = evalTo<int32 > (args.ExactlyOne()) |> Math.Abs
        let _absd  (args:Args) = evalTo<double> (args.ExactlyOne()) |> Math.Abs
        let _absf  (args:Args) = evalTo<single > (args.ExactlyOne()) |> Math.Abs
        let _absu  (args:Args) = evalTo<uint32> (args.ExactlyOne()) |> Math.Abs
        let _absL  (args:Args) = evalTo<int64 > (args.ExactlyOne()) |> Math.Abs
        let _absUL (args:Args) = evalTo<uint64> (args.ExactlyOne()) |> Math.Abs



        let _equal   (args:Args) = args.ExpectGteN(2) .Select(evalArg) .Pairwise() .All(fun (x, y) -> isEqual x y)
        let _notEqual (args:Args) = not <| _equal args
        let _equalString (args:Args) = args.ExpectGteN(2) .Select(evalArg).Cast<string>().Distinct().Count() = 1
        let _notEqualString (args:Args) = not <| _equalString args

        let private convertToDoublePair (args:Args) = args.ExpectGteN(2).Select(fun x -> x.BoxedEvaluatedValue |> toFloat64).Pairwise()
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

        let _shiftLeftInt8    (args:Args) = let n, shift = args.ExpectTyped2<int8,   int>() in n <<< shift
        let _shiftLeftUInt8   (args:Args) = let n, shift = args.ExpectTyped2<uint8,  int>() in n <<< shift
        let _shiftLeftInt16   (args:Args) = let n, shift = args.ExpectTyped2<int16,  int>() in n <<< shift
        let _shiftLeftUInt16  (args:Args) = let n, shift = args.ExpectTyped2<uint16, int>() in n <<< shift
        let _shiftLeftInt32   (args:Args) = let n, shift = args.ExpectTyped2<int32,  int>() in n <<< shift
        let _shiftLeftUInt32  (args:Args) = let n, shift = args.ExpectTyped2<uint32, int>() in n <<< shift
        let _shiftLeftInt64   (args:Args) = let n, shift = args.ExpectTyped2<int64,  int>() in n <<< shift
        let _shiftLeftUInt64  (args:Args) = let n, shift = args.ExpectTyped2<uint64, int>() in n <<< shift

        let _shiftRightInt8   (args:Args) = let n, shift = args.ExpectTyped2<int8,   int>() in n >>> shift
        let _shiftRightUInt8  (args:Args) = let n, shift = args.ExpectTyped2<uint8,  int>() in n >>> shift
        let _shiftRightInt16  (args:Args) = let n, shift = args.ExpectTyped2<int16,  int>() in n >>> shift
        let _shiftRightUInt16 (args:Args) = let n, shift = args.ExpectTyped2<uint16, int>() in n >>> shift
        let _shiftRightInt32  (args:Args) = let n, shift = args.ExpectTyped2<int32,  int>() in n >>> shift
        let _shiftRightUInt32 (args:Args) = let n, shift = args.ExpectTyped2<uint32, int>() in n >>> shift
        let _shiftRightInt64  (args:Args) = let n, shift = args.ExpectTyped2<int64,  int>() in n >>> shift
        let _shiftRightUInt64 (args:Args) = let n, shift = args.ExpectTyped2<uint64, int>() in n >>> shift


        let _sin (args:Args) = args.Select(evalArg >> toFloat64).Expect1() |> Math.Sin
        let _cos (args:Args) = args.Select(evalArg >> toFloat64).Expect1() |> Math.Cos
        let _tan (args:Args) = args.Select(evalArg >> toFloat64).Expect1() |> Math.Tan

        let _castToUInt8  (args:Args)  = args.Select(evalArg >> toUInt8)  .Expect1()
        let _castToInt8   (args:Args)  = args.Select(evalArg >> toInt8)   .Expect1()
        let _castToInt16  (args:Args)  = args.Select(evalArg >> toInt16)  .Expect1()
        let _castToUInt16 (args:Args)  = args.Select(evalArg >> toUInt16) .Expect1()
        let _castToInt32  (args:Args)  = args.Select(evalArg >> toInt32)  .Expect1()
        let _castToUInt32 (args:Args)  = args.Select(evalArg >> toUInt32) .Expect1()
        let _castToInt64  (args:Args)  = args.Select(evalArg >> toInt64)  .Expect1()
        let _castToUInt64 (args:Args)  = args.Select(evalArg >> toUInt64) .Expect1()

        let _castToBool (args:Args)    = args.Select(evalArg >> toBool) .Expect1()
        let _castToFloat32 (args:Args) = args.Select(evalArg >> toFloat32) .Expect1()
        let _castToFloat64 (args:Args) = args.Select(evalArg >> toFloat64) .Expect1()

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

