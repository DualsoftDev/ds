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
            | ">" | ">=" | "<" | "<=" | "=" | "="
            | "&&" | "||"
            | "&" | "|" | "&&&" | "|||" ) ->
            verifyAllExpressionSameType [opnd1; opnd2]
            match op with
            | "&&" | "||" -> if t1 <> typedefof<bool> then failwith $"{op} expects bool.  Type mismatch"
            | _ -> ()
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
        | ("!=" | "<>")  -> fNotEqual args

        | ("<<<" | "<<") -> fShiftLeft  args
        | (">>>" | ">>") -> fShiftRight args

        | ("&&&" | "&") ->  fBitwiseAnd args
        | ("|||" | "|") ->  fBitwiseOr  args
        | ("^^^" | "^") ->  fBitwiseXor args
        | ("~~~" | "~") ->  failwith "Not binary operation" //fBitwiseNot args

        | "&&"  -> fLogicalAnd  args
        | "||"  -> fLogicalOr  args


        | _ -> failwith $"NOT Yet {op}"
        |> iexpr

    let createUnaryExpression (op:string) (opnd:IExpression) : IExpression =
        match op with
        | ("~" | "~~~" ) -> fBitwiseNot [opnd]
        | "!"  -> fLogicalNot [opnd]
        | _ ->
            failwith $"NOT Yet {op}"

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
        | ("||" | "or")  -> fLogicalOr  args
        | ("!"  | "not") -> fLogicalNot args

        | ("&" | "&&&") -> fBitwiseAnd  args
        | ("|" | "|||") -> fBitwiseOr   args
        | ("^" | "^^^") -> fBitwiseXor  args
        | ("~" | "~~~") -> fBitwiseNot  args

        | ("bool"   | "toBool") -> fCastBool    args |> iexpr
        | ("sbyte"  | "toSByte" | "toInt8")     -> fCastInt8   args |> iexpr
        | ("byte"   | "toByte"  | "toUInt8")    -> fCastUInt8  args |> iexpr
        | ("short"  | "toShort" | "toInt16")    -> fCastInt16  args |> iexpr
        | ("ushort" | "toUShort"| "toUInt16")   -> fCastUInt16 args |> iexpr
        | ("int"    | "toInt"   | "toInt32")    -> fCastInt32  args |> iexpr
        | ("uint"   | "toUInt"  | "toUInt32")   -> fCastUInt32 args |> iexpr
        | ("long"   | "toLong"  | "toInt64")    -> fCastInt64  args |> iexpr
        | ("ulong"  | "toULong" | "toUInt64")   -> fCastUInt64 args |> iexpr

        | ("single" | "float" | "float32" | "toSingle"| "toFloat" | "toFloat32") -> fCastFloat32 args |> iexpr
        | ("double" | "float64" | "toDouble"| "toFloat64" ) -> fCastFloat64  args |> iexpr

        | "sin" -> fSin args |> iexpr
        | "cos" -> fCos args |> iexpr
        | "tan" -> fTan args |> iexpr
        | _ -> failwith $"NOT yet: {funName}"

    [<AutoOpen>]
    module FunctionModule =
        /// Create function expression
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
            | "SByte"  -> cf _addInt8   "+"  args
            | "Byte"   -> cf _addUInt8  "+"  args
            | "Int16"  -> cf _addInt16  "+"  args
            | "UInt16" -> cf _addUInt16 "+"  args
            | "Int32"  -> cf _addInt32  "+"  args
            | "UInt32" -> cf _addUInt32 "+"  args
            | "Int64"  -> cf _addInt64  "+"  args
            | "UInt64" -> cf _addUInt64 "+"  args
            | "Single" -> cf _addFloat32 "+" args
            | "Double" -> cf _addFloat64 "+" args
            | _        -> failwith "ERROR"

        let fSub (args:Args) : IExpression =
            match args[0].DataType.Name with
            | "SByte"  -> cf _subInt8    "-" args
            | "Byte"   -> cf _subUInt8   "-" args
            | "Int16"  -> cf _subInt16   "-" args
            | "UInt16" -> cf _subUInt16  "-" args
            | "Int32"  -> cf _subInt32   "-" args
            | "UInt32" -> cf _subUInt32  "-" args
            | "Int64"  -> cf _subInt64   "-" args
            | "UInt64" -> cf _subUInt64  "-" args
            | "Single" -> cf _subFloat32 "-" args
            | "Double" -> cf _subFloat64 "-" args
            | _        -> failwith "ERROR"

        let fMul (args:Args) : IExpression =
            match args[0].DataType.Name with
            | "SByte"  -> cf _mulInt8    "*" args
            | "Byte"   -> cf _mulUInt8   "*" args
            | "Int16"  -> cf _mulInt16   "*" args
            | "UInt16" -> cf _mulUInt16  "*" args
            | "Int32"  -> cf _mulInt32   "*" args
            | "UInt32" -> cf _mulUInt32  "*" args
            | "Int64"  -> cf _mulInt64   "*" args
            | "UInt64" -> cf _mulUInt64  "*" args
            | "Single" -> cf _mulFloat32 "*" args
            | "Double" -> cf _mulFloat64 "*" args
            | _        -> failwith "ERROR"

        let fDiv (args:Args) : IExpression =
            match args[0].DataType.Name with
            | "SByte"  -> cf _divInt8    "/" args
            | "Byte"   -> cf _divUInt8   "/" args
            | "Int16"  -> cf _divInt16   "/" args
            | "UInt16" -> cf _divUInt16  "/" args
            | "Int32"  -> cf _divInt32   "/" args
            | "UInt32" -> cf _divUInt32  "/" args
            | "Int64"  -> cf _divInt64   "/" args
            | "UInt64" -> cf _divUInt64  "/" args
            | "Single" -> cf _divFloat32 "/" args
            | "Double" -> cf _divFloat64 "/" args
            | _        -> failwith "ERROR"

        let fAbs (args:Args) : IExpression =
            match args[0].DataType.Name with
            | "SByte"  -> cf _absInt8    "abs" args
            | "Byte"   -> cf _absUInt8   "abs" args
            | "Int16"  -> cf _absInt16   "abs" args
            | "UInt16" -> cf _absUInt16  "abs" args
            | "Int32"  -> cf _absInt32   "abs" args
            | "UInt32" -> cf _absUInt32  "abs" args
            | "Int64"  -> cf _absInt64   "abs" args
            | "UInt64" -> cf _absUInt64  "abs" args
            | "Single" -> cf _absFloat32 "abs" args
            | "Double" -> cf _absFloat64 "abs" args
            | _        -> failwith "ERROR"

        let fMod (args:Args) : IExpression =
            match args[0].DataType.Name with
            | "SByte"  -> cf _modInt8    "%" args
            | "Byte"   -> cf _modUInt8   "%" args
            | "Int16"  -> cf _modInt16   "%" args
            | "UInt16" -> cf _modUInt16  "%" args
            | "Int32"  -> cf _modInt32   "%" args
            | "UInt32" -> cf _modUInt32  "%" args
            | "Int64"  -> cf _modInt64   "%" args
            | "UInt64" -> cf _modUInt64  "%" args
            | "Single" -> cf _modFloat32 "%" args
            | "Double" -> cf _modFloat64 "%" args
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

        let fBitwiseAnd (args:Args) : IExpression =
            match args[0].DataType.Name with
            | "SByte"  -> cf _bitwiseAndInt8    "&&&" args
            | "Byte"   -> cf _bitwiseAndUInt8   "&&&" args
            | "Int16"  -> cf _bitwiseAndInt16   "&&&" args
            | "UInt16" -> cf _bitwiseAndUInt16  "&&&" args
            | "Int32"  -> cf _bitwiseAndInt32   "&&&" args
            | "UInt32" -> cf _bitwiseAndUInt32  "&&&" args
            | "Int64"  -> cf _bitwiseAndInt64   "&&&" args
            | "UInt64" -> cf _bitwiseAndUInt64  "&&&" args
            | _        -> failwith "ERROR"

        let fBitwiseOr (args:Args) : IExpression =
            match args[0].DataType.Name with
            | "SByte"  -> cf _bitwiseOrInt8    "|||" args
            | "Byte"   -> cf _bitwiseOrUInt8   "|||" args
            | "Int16"  -> cf _bitwiseOrInt16   "|||" args
            | "UInt16" -> cf _bitwiseOrUInt16  "|||" args
            | "Int32"  -> cf _bitwiseOrInt32   "|||" args
            | "UInt32" -> cf _bitwiseOrUInt32  "|||" args
            | "Int64"  -> cf _bitwiseOrInt64   "|||" args
            | "UInt64" -> cf _bitwiseOrUInt64  "|||" args
            | _        -> failwith "ERROR"

        let fBitwiseNot (args:Args) : IExpression =
            match args[0].DataType.Name with
            | "SByte"  -> cf _bitwiseNotInt8    "~~~" args
            | "Byte"   -> cf _bitwiseNotUInt8   "~~~" args
            | "Int16"  -> cf _bitwiseNotInt16   "~~~" args
            | "UInt16" -> cf _bitwiseNotUInt16  "~~~" args
            | "Int32"  -> cf _bitwiseNotInt32   "~~~" args
            | "UInt32" -> cf _bitwiseNotUInt32  "~~~" args
            | "Int64"  -> cf _bitwiseNotInt64   "~~~" args
            | "UInt64" -> cf _bitwiseNotUInt64  "~~~" args
            | _        -> failwith "ERROR"

        let fBitwiseXor (args:Args) : IExpression =
            match args[0].DataType.Name with
            | "SByte"  -> cf _bitwiseXorInt8    "^^^" args
            | "Byte"   -> cf _bitwiseXorUInt8   "^^^" args
            | "Int16"  -> cf _bitwiseXorInt16   "^^^" args
            | "UInt16" -> cf _bitwiseXorUInt16  "^^^" args
            | "Int32"  -> cf _bitwiseXorInt32   "^^^" args
            | "UInt32" -> cf _bitwiseXorUInt32  "^^^" args
            | "Int64"  -> cf _bitwiseXorInt64   "^^^" args
            | "UInt64" -> cf _bitwiseXorUInt64  "^^^" args
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
        let fSin            args = cf _sin            "sin"    args
        let fCos            args = cf _cos            "cos"    args
        let fTan            args = cf _tan            "tan"    args


        let fCastBool       args = cf _castToBool     "toBool"   args
        let fCastUInt8      args = cf _castToUInt8    "toByte"   args
        let fCastInt8       args = cf _castToInt8     "toSByte"  args
        let fCastInt16      args = cf _castToInt16    "toInt16"  args
        let fCastUInt16     args = cf _castToUInt16   "toUInt16" args
        let fCastInt32      args = cf _castToInt32    "toInt32"  args
        let fCastUInt32     args = cf _castToUInt32   "toUInt32" args
        let fCastInt64      args = cf _castToInt64    "toInt64"  args
        let fCastUInt64     args = cf _castToUInt64   "toUInt64" args
        let fCastFloat32    args = cf _castToFloat32  "toFloat32"  args
        let fCastFloat64    args = cf _castToFloat64  "toFloat64" args

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


        let _addInt8    (args:Args) = args.ExpectGteN(2).Select(evalTo<int8>)    .Reduce( + )
        let _addUInt8   (args:Args) = args.ExpectGteN(2).Select(evalTo<uint8>)   .Reduce( + )
        let _addInt16   (args:Args) = args.ExpectGteN(2).Select(evalTo<int16>)   .Reduce( + )
        let _addUInt16  (args:Args) = args.ExpectGteN(2).Select(evalTo<uint16>)  .Reduce( + )
        let _addInt32   (args:Args) = args.ExpectGteN(2).Select(evalTo<int32>)   .Reduce( + )
        let _addUInt32  (args:Args) = args.ExpectGteN(2).Select(evalTo<uint32>)  .Reduce( + )
        let _addInt64   (args:Args) = args.ExpectGteN(2).Select(evalTo<int64>)   .Reduce( + )
        let _addUInt64  (args:Args) = args.ExpectGteN(2).Select(evalTo<uint64>)  .Reduce( + )
        let _addFloat32 (args:Args) = args.ExpectGteN(2).Select(evalTo<single>)  .Reduce( + )
        let _addFloat64 (args:Args) = args.ExpectGteN(2).Select(evalTo<double>)  .Reduce( + )


        let _subInt8    (args:Args) = args.ExpectGteN(2).Select(evalTo<int8>)    .Reduce( - )
        let _subUInt8   (args:Args) = args.ExpectGteN(2).Select(evalTo<uint8>)   .Reduce( - )
        let _subInt16   (args:Args) = args.ExpectGteN(2).Select(evalTo<int16>)   .Reduce( - )
        let _subUInt16  (args:Args) = args.ExpectGteN(2).Select(evalTo<uint16>)  .Reduce( - )
        let _subInt32   (args:Args) = args.ExpectGteN(2).Select(evalTo<int32>)   .Reduce( - )
        let _subUInt32  (args:Args) = args.ExpectGteN(2).Select(evalTo<uint32>)  .Reduce( - )
        let _subInt64   (args:Args) = args.ExpectGteN(2).Select(evalTo<int64>)   .Reduce( - )
        let _subUInt64  (args:Args) = args.ExpectGteN(2).Select(evalTo<uint64>)  .Reduce( - )
        let _subFloat32 (args:Args) = args.ExpectGteN(2).Select(evalTo<single>)  .Reduce( - )
        let _subFloat64 (args:Args) = args.ExpectGteN(2).Select(evalTo<double>)  .Reduce( - )


        let _mulInt8    (args:Args) = args.ExpectGteN(2).Select(evalTo<int8>)    .Reduce( * )
        let _mulUInt8   (args:Args) = args.ExpectGteN(2).Select(evalTo<uint8>)   .Reduce( * )
        let _mulInt16   (args:Args) = args.ExpectGteN(2).Select(evalTo<int16>)   .Reduce( * )
        let _mulUInt16  (args:Args) = args.ExpectGteN(2).Select(evalTo<uint16>)  .Reduce( * )
        let _mulInt32   (args:Args) = args.ExpectGteN(2).Select(evalTo<int32>)   .Reduce( * )
        let _mulUInt32  (args:Args) = args.ExpectGteN(2).Select(evalTo<uint32>)  .Reduce( * )
        let _mulInt64   (args:Args) = args.ExpectGteN(2).Select(evalTo<int64>)   .Reduce( * )
        let _mulUInt64  (args:Args) = args.ExpectGteN(2).Select(evalTo<uint64>)  .Reduce( * )
        let _mulFloat32 (args:Args) = args.ExpectGteN(2).Select(evalTo<single>)  .Reduce( * )
        let _mulFloat64 (args:Args) = args.ExpectGteN(2).Select(evalTo<double>)  .Reduce( * )


        let _divInt8    (args:Args) = args.ExpectGteN(2).Select(evalTo<int8>)    .Reduce( / )
        let _divUInt8   (args:Args) = args.ExpectGteN(2).Select(evalTo<uint8>)   .Reduce( / )
        let _divInt16   (args:Args) = args.ExpectGteN(2).Select(evalTo<int16>)   .Reduce( / )
        let _divUInt16  (args:Args) = args.ExpectGteN(2).Select(evalTo<uint16>)  .Reduce( / )
        let _divInt32   (args:Args) = args.ExpectGteN(2).Select(evalTo<int32>)   .Reduce( / )
        let _divUInt32  (args:Args) = args.ExpectGteN(2).Select(evalTo<uint32>)  .Reduce( / )
        let _divInt64   (args:Args) = args.ExpectGteN(2).Select(evalTo<int64>)   .Reduce( / )
        let _divUInt64  (args:Args) = args.ExpectGteN(2).Select(evalTo<uint64>)  .Reduce( / )
        let _divFloat32 (args:Args) = args.ExpectGteN(2).Select(evalTo<single>)  .Reduce( / )
        let _divFloat64 (args:Args) = args.ExpectGteN(2).Select(evalTo<double>)  .Reduce( / )


        let _modInt8    (args:Args) = args.ExpectGteN(2).Select(evalTo<int8>)    .Reduce( % )
        let _modUInt8   (args:Args) = args.ExpectGteN(2).Select(evalTo<uint8>)   .Reduce( % )
        let _modInt16   (args:Args) = args.ExpectGteN(2).Select(evalTo<int16>)   .Reduce( % )
        let _modUInt16  (args:Args) = args.ExpectGteN(2).Select(evalTo<uint16>)  .Reduce( % )
        let _modInt32   (args:Args) = args.ExpectGteN(2).Select(evalTo<int32>)   .Reduce( % )
        let _modUInt32  (args:Args) = args.ExpectGteN(2).Select(evalTo<uint32>)  .Reduce( % )
        let _modInt64   (args:Args) = args.ExpectGteN(2).Select(evalTo<int64>)   .Reduce( % )
        let _modUInt64  (args:Args) = args.ExpectGteN(2).Select(evalTo<uint64>)  .Reduce( % )
        let _modFloat32 (args:Args) = args.ExpectGteN(2).Select(evalTo<single>)  .Reduce( % )
        let _modFloat64 (args:Args) = args.ExpectGteN(2).Select(evalTo<double>)  .Reduce( % )


        let _bitwiseAndInt8   (args:Args) = args.Expect2().Select(evalTo<int8>)  .Reduce(&&&)
        let _bitwiseAndUInt8  (args:Args) = args.Expect2().Select(evalTo<uint8>) .Reduce(&&&)
        let _bitwiseAndInt16  (args:Args) = args.Expect2().Select(evalTo<int16>) .Reduce(&&&)
        let _bitwiseAndUInt16 (args:Args) = args.Expect2().Select(evalTo<uint16>).Reduce(&&&)
        let _bitwiseAndInt32  (args:Args) = args.Expect2().Select(evalTo<int32>) .Reduce(&&&)
        let _bitwiseAndUInt32 (args:Args) = args.Expect2().Select(evalTo<uint32>).Reduce(&&&)
        let _bitwiseAndInt64  (args:Args) = args.Expect2().Select(evalTo<int64>) .Reduce(&&&)
        let _bitwiseAndUInt64 (args:Args) = args.Expect2().Select(evalTo<uint64>).Reduce(&&&)

        let _bitwiseOrInt8   (args:Args) = args.Expect2().Select(evalTo<int8>)   .Reduce(|||)
        let _bitwiseOrUInt8  (args:Args) = args.Expect2().Select(evalTo<uint8>)  .Reduce(|||)
        let _bitwiseOrInt16  (args:Args) = args.Expect2().Select(evalTo<int16>)  .Reduce(|||)
        let _bitwiseOrUInt16 (args:Args) = args.Expect2().Select(evalTo<uint16>) .Reduce(|||)
        let _bitwiseOrInt32  (args:Args) = args.Expect2().Select(evalTo<int32>)  .Reduce(|||)
        let _bitwiseOrUInt32 (args:Args) = args.Expect2().Select(evalTo<uint32>) .Reduce(|||)
        let _bitwiseOrInt64  (args:Args) = args.Expect2().Select(evalTo<int64>)  .Reduce(|||)
        let _bitwiseOrUInt64 (args:Args) = args.Expect2().Select(evalTo<uint64>) .Reduce(|||)

        let _bitwiseXorInt8   (args:Args) = args.Expect2().Select(evalTo<int8>)  .Reduce(^^^)
        let _bitwiseXorUInt8  (args:Args) = args.Expect2().Select(evalTo<uint8>) .Reduce(^^^)
        let _bitwiseXorInt16  (args:Args) = args.Expect2().Select(evalTo<int16>) .Reduce(^^^)
        let _bitwiseXorUInt16 (args:Args) = args.Expect2().Select(evalTo<uint16>).Reduce(^^^)
        let _bitwiseXorInt32  (args:Args) = args.Expect2().Select(evalTo<int32>) .Reduce(^^^)
        let _bitwiseXorUInt32 (args:Args) = args.Expect2().Select(evalTo<uint32>).Reduce(^^^)
        let _bitwiseXorInt64  (args:Args) = args.Expect2().Select(evalTo<int64>) .Reduce(^^^)
        let _bitwiseXorUInt64 (args:Args) = args.Expect2().Select(evalTo<uint64>).Reduce(^^^)

        let _bitwiseNotInt8   (args:Args) = args.Select(evalArg).Cast<int8>()  .Expect1() |> (~~~)
        let _bitwiseNotUInt8  (args:Args) = args.Select(evalArg).Cast<uint8>() .Expect1() |> (~~~)
        let _bitwiseNotInt16  (args:Args) = args.Select(evalArg).Cast<int16>() .Expect1() |> (~~~)
        let _bitwiseNotUInt16 (args:Args) = args.Select(evalArg).Cast<uint16>().Expect1() |> (~~~)
        let _bitwiseNotInt32  (args:Args) = args.Select(evalArg).Cast<int32>() .Expect1() |> (~~~)
        let _bitwiseNotUInt32 (args:Args) = args.Select(evalArg).Cast<uint32>().Expect1() |> (~~~)
        let _bitwiseNotInt64  (args:Args) = args.Select(evalArg).Cast<int64>() .Expect1() |> (~~~)
        let _bitwiseNotUInt64 (args:Args) = args.Select(evalArg).Cast<uint64>().Expect1() |> (~~~)



        let _absInt8    (args:Args) = evalTo<int8>   (args.ExactlyOne()) |> Math.Abs
        let _absUInt8   (args:Args) = evalTo<uint8>  (args.ExactlyOne()) |> Math.Abs
        let _absInt16   (args:Args) = evalTo<int16 > (args.ExactlyOne()) |> Math.Abs
        let _absUInt16  (args:Args) = evalTo<uint16> (args.ExactlyOne()) |> Math.Abs
        let _absInt32   (args:Args) = evalTo<int32 > (args.ExactlyOne()) |> Math.Abs
        let _absUInt32  (args:Args) = evalTo<uint32> (args.ExactlyOne()) |> Math.Abs
        let _absInt64   (args:Args) = evalTo<int64 > (args.ExactlyOne()) |> Math.Abs
        let _absUInt64  (args:Args) = evalTo<uint64> (args.ExactlyOne()) |> Math.Abs
        let _absFloat32 (args:Args) = evalTo<single> (args.ExactlyOne()) |> Math.Abs
        let _absFloat64 (args:Args) = evalTo<double> (args.ExactlyOne()) |> Math.Abs




        let _equal   (args:Args) = args.ExpectGteN(2).Select(evalArg).Pairwise().All(fun (x, y) -> isEqual x y)
        let _notEqual (args:Args) = not <| _equal args
        let _equalString (args:Args) = args.ExpectGteN(2) .Select(evalArg).Cast<string>().Distinct().Count() = 1
        let _notEqualString (args:Args) = not <| _equalString args

        let private convertToDoublePair (args:Args) = args.ExpectGteN(2).Select(fun x -> x.BoxedEvaluatedValue |> toFloat64).Pairwise()
        let _gt  (args:Args) = convertToDoublePair(args).All(fun (x, y) -> x > y)
        let _lt  (args:Args) = convertToDoublePair(args).All(fun (x, y) -> x < y)
        let _gte (args:Args) = convertToDoublePair(args).All(fun (x, y) -> x >= y)
        let _lte (args:Args) = convertToDoublePair(args).All(fun (x, y) -> x <= y)

        let _concat     (args:Args) = args.ExpectGteN(2).Select(evalArg).Cast<string>().Reduce( + )
        let _logicalAnd (args:Args) = args.ExpectGteN(2).Select(evalArg).Cast<bool>()  .Reduce( && )
        let _logicalOr  (args:Args) = args.ExpectGteN(2).Select(evalArg).Cast<bool>()  .Reduce( || )
        let _logicalNot (args:Args) = args.Select(evalArg).Cast<bool>().Expect1() |> not

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

        let _castToUInt8   (args:Args) = args.Select(evalArg >> toUInt8)   .Expect1()
        let _castToInt8    (args:Args) = args.Select(evalArg >> toInt8)    .Expect1()
        let _castToInt16   (args:Args) = args.Select(evalArg >> toInt16)   .Expect1()
        let _castToUInt16  (args:Args) = args.Select(evalArg >> toUInt16)  .Expect1()
        let _castToInt32   (args:Args) = args.Select(evalArg >> toInt32)   .Expect1()
        let _castToUInt32  (args:Args) = args.Select(evalArg >> toUInt32)  .Expect1()
        let _castToInt64   (args:Args) = args.Select(evalArg >> toInt64)   .Expect1()
        let _castToUInt64  (args:Args) = args.Select(evalArg >> toUInt64)  .Expect1()

        let _castToBool    (args:Args) = args.Select(evalArg >> toBool)    .Expect1()
        let _castToFloat32 (args:Args) = args.Select(evalArg >> toFloat32) .Expect1()
        let _castToFloat64 (args:Args) = args.Select(evalArg >> toFloat64) .Expect1()

    let private tagsToArguments (xs:Tag<'T> seq) = xs.Select(fun x -> Terminal (Tag x)) |> List.ofSeq
    [<Extension>]
    type FuncExt =

        [<Extension>] static member ToTags (xs:#Tag<'T> seq)    = xs.Cast<Tag<_>>()
        [<Extension>] static member ToExpr (x:Tag<bool>)   = Terminal (Tag x)
        [<Extension>] static member GetAnd (xs:Tag<bool> seq)  =
                                        if xs.length() = 1 
                                        then tag (xs.First())
                                        else xs |> tagsToArguments |> List.cast<IExpression> |> fLogicalAnd
        [<Extension>] static member GetOr  (xs:Tag<bool> seq)  =
                                        if xs.length() = 1 
                                        then tag (xs.First())
                                        else xs |> tagsToArguments |> List.cast<IExpression> |> fLogicalOr
                                          
        //[sets and]--|----- ! [rsts or] ----- (relay)
        //|relay|-----|
        [<Extension>] static member GetRelayExpr(sets:Tag<bool> seq, rsts:Tag<bool> seq, relay:Tag<bool>) =
                        (sets.GetAnd() <||> relay.ToExpr()) <&&> (!! rsts.GetOr())

        //[sets and]--|----- ! [rsts or] ----- (coil)
        [<Extension>] static member GetNoRelayExpr(sets:Tag<bool> seq, rsts:Tag<bool> seq) =
                        sets.GetAnd() <&&> (!! rsts.GetOr())

        //[sets and]--|-----  [rsts and] ----- (relay)
        //|relay|-----|
        [<Extension>] static member GetRelayExprReverseReset(sets:Tag<bool> seq, rsts:Tag<bool> seq, relay:Tag<bool>) =
                        (sets.GetAnd() <||> relay.ToExpr()) <&&> (rsts.GetOr())


    [<AutoOpen>]
    module ExpressionOperatorModule =
        /// boolean AND operator
        let (<&&>) (left: Expression<bool>) (right: Expression<bool>) = fLogicalAnd [ left; right ]
        /// boolean OR operator
        let (<||>) (left: Expression<bool>) (right: Expression<bool>) = fLogicalOr [ left; right ]
        /// boolean NOT operator
        let (!!)   (exp: Expression<bool>) = fLogicalNot [exp]
        /// Assign statement
        let (<==)  (storage: IStorage) (exp: IExpression) = Assign(exp, storage)

