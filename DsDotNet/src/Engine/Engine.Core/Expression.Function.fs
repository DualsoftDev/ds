namespace rec Engine.Core
open System
open System.Linq
open System.Runtime.CompilerServices
open Engine.Common.FS
open System.Collections.Generic

module private ExpressionHelperModule =
    let expectN (n:int) (xs:'a seq) = if xs.Count() <> n then failwith $"Wrong number of arguments: expect {n}"
    let expect1 xs = expectN 1 xs; xs.First()
    let expect2 xs = expectN 2 xs; Array.ofSeq xs
    let expectGteN (n:int) (xs:'a seq) =
        if xs.Count() < n then failwith $"Wrong number of arguments: expect at least {n} arguments"

    let evalArg (x:IExpression) = x.BoxedEvaluatedValue
    let castTo<'T> (x:obj) = x :?> 'T
    let evalTo<'T> (x:IExpression) = x |> evalArg |> castTo<'T>

    /// 모든 args 의 data type 이 동일한지 여부 반환
    let isAllExpressionSameType(args:Args) =
        args |> Seq.distinctBy(fun a -> a.DataType) |> Seq.length = 1
    let verifyAllExpressionSameType = isAllExpressionSameType >> verifyM "Type mismatch"
    let isThisOperatorRequireAllArgumentsSameType: (string -> bool)  =
        let hash =
            [   "+" ; "-" ; "*" ; "/" ; "%"
                ">" ; ">=" ; "<" ; "<=" ; "=" ; "!="
                "&&" ; "||"
                "&" ; "|" ; "&&&" ; "|||"
                "add"; "sub"; "mul"; "div"
                "gt"; "gte"; "lt"; "lte"
                "equal"; "notEqual"; "and"; "or"
            ] |> HashSet<string>
        fun (name:string) -> hash.Contains (name)
    let verifyArgumentsTypes operator args =
        if isThisOperatorRequireAllArgumentsSameType operator && not <| isAllExpressionSameType args then
            failwith $"Type mismatch for operator={operator}"
[<AutoOpen>]
module ExpressionFunctionModule =
    open ExpressionHelperModule

    /// Expression<'T> 를 IExpression 으로 casting
    let internal iexpr any = (box any) :?> IExpression

    let [<Literal>] FunctionNameRising  = "rising"
    let [<Literal>] FunctionNameFalling = "falling"

    let createBinaryExpression (opnd1:IExpression) (op:string) (opnd2:IExpression) : IExpression =
        let t1 = opnd1.DataType

        verifyArgumentsTypes op [opnd1; opnd2]
        match op with
        | "&&" | "||" -> if t1 <> typedefof<bool> then failwith $"{op} expects bool.  Type mismatch"
        | _ -> ()

        let t = t1.Name
        let args = [opnd1; opnd2]

        match op with
        | "+" when t = STRING -> fConcat args
        | "+" -> fAdd args
        | "-" -> fSub args
        | "*" -> fMul args
        | "/" -> fDiv args

        | ">"  -> fGt  args
        | ">=" -> fGte args
        | "<"  -> fLt  args
        | "<=" -> fLte args
        | "=" when t = STRING -> fEqualString args
        | "="  -> fEqual args
        | ("!=" | "<>")  -> fNotEqual args

        | ("<<<" | "<<") -> fShiftLeft  args
        | (">>>" | ">>") -> fShiftRight args

        | ("&&&" | "&") ->  fBitwiseAnd args
        | ("|||" | "|") ->  fBitwiseOr  args
        | ("^^^" | "^") ->  fBitwiseXor args
        | ("~~~" | "~") ->  failwithlog "Not binary operation" //fBitwiseNot args

        | "&&"  -> fLogicalAnd  args
        | "||"  -> fLogicalOr  args


        | _ -> failwith $"NOT Yet {op}"
        |> iexpr

    let createUnaryExpression (op:string) (opnd:IExpression) : IExpression =
        (* unary operator 처리.
           - '! $myTag' 처럼  괄호 없이도 사용가능한 것들만 정의한다.
           - 괄호도 허용하려면 createCustomFunctionExpression 에서도 정의해야 한다. '! ($myTag)'
         *)
        match op with
        | ("~" | "~~~" ) -> fBitwiseNot [opnd]
        | "!"  -> fLogicalNot [opnd]
        | _ ->
            failwith $"NOT Yet {op}"

    let createCustomFunctionExpression (funName:string) (args:Args) : IExpression =
        verifyArgumentsTypes funName args
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

        | ("="  | "equal") when t = STRING -> fEqualString args
        | ("="  | "equal") -> fEqual args
        | ("!=" | "notEqual") when t = STRING -> fNotEqualString args
        | ("!=" | "notEqual") -> fNotEqual args

        | ("<<" | "<<<" | "shiftLeft") -> fShiftLeft args
        | (">>" | ">>>" | "shiftRight") -> fShiftLeft args

        | ("&&" | "and") -> fLogicalAnd args
        | ("||" | "or")  -> fLogicalOr  args
        | ("!"  | "not") -> fLogicalNot args        // 따로 or 같이??? neg 는 contact 이나 coil 하나만 받아서 rung 생성하는 용도, not 은 expression 을 받아서 평가하는 용도

        | ("&" | "&&&") -> fBitwiseAnd  args
        | ("|" | "|||") -> fBitwiseOr   args
        | ("^" | "^^^") -> fBitwiseXor  args
        | ("~" | "~~~") -> fBitwiseNot  args

        | FunctionNameRising  -> fRising  args
        | FunctionNameFalling -> fFalling args
        //| "neg"     -> fNegate  args
        //| "set"     -> fSet     args
        //| "reset"   -> fReset   args


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

        (* Timer/Counter
          - 실제로 function/expression 은 아니지만, parsing 편의를 고려해 function 처럼 취급.
          - evaluate 등은 수행해서는 안된다.
        *)
        | (   "createXgiCTU" | "createXgiCTD" | "createXgiCTUD" | "createXgiCTR"
            | "createWinCTU" | "createWinCTD" | "createWinCTUD" | "createWinCTR"
            | "createAbCTU"  | "createAbCTD"  | "createAbCTUD"  | "createAbCTR" ) ->
                let psedoFunction (_args:Args):Counter = failwithlog "THIS IS PSEUDO FUNCTION.  SHOULD NOT BE EVALUATED!!!!"
                DuFunction { FunctionBody=psedoFunction; Name=funName; Arguments=args }
        | (   "createXgiTON" | "createXgiTOF" | "createXgiCRTO"
            | "createWinTON" | "createWinTOF" | "createWinCRTO"
            | "createAbTON"  | "createAbTOF"  | "createAbCRTO") ->
                let psedoFunction (_args:Args):Timer = failwithlog "THIS IS PSEUDO FUNCTION.  SHOULD NOT BE EVALUATED!!!!"
                DuFunction { FunctionBody=psedoFunction; Name=funName; Arguments=args }
        | "createTag" ->
                let psedoFunction (_args:Args):ITag = failwithlog "THIS IS PSEUDO FUNCTION.  SHOULD NOT BE EVALUATED!!!!"
                DuFunction { FunctionBody=psedoFunction; Name=funName; Arguments=args }

        | _ -> failwith $"NOT yet: {funName}"

    /// Create function expression
    let private cf (f:Args->'T) (name:string) (args:Args) =
        DuFunction { FunctionBody=f; Name=name; Arguments=args}

    [<AutoOpen>]
    module FunctionModule =
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
            | FLOAT32  -> cf _addFloat32 "+"  args
            | FLOAT64  -> cf _addFloat64 "+"  args
            | INT16    -> cf _addInt16   "+"  args
            | INT32    -> cf _addInt32   "+"  args
            | INT64    -> cf _addInt64   "+"  args
            | INT8     -> cf _addInt8    "+"  args
            | UINT16   -> cf _addUInt16  "+"  args
            | UINT32   -> cf _addUInt32  "+"  args
            | UINT64   -> cf _addUInt64  "+"  args
            | UINT8    -> cf _addUInt8   "+"  args
            | _        -> failwithlog "ERROR"

        let fSub (args:Args) : IExpression =
            match args[0].DataType.Name with
            | FLOAT32  -> cf _subFloat32 "-" args
            | FLOAT64  -> cf _subFloat64 "-" args
            | INT16    -> cf _subInt16   "-" args
            | INT32    -> cf _subInt32   "-" args
            | INT64    -> cf _subInt64   "-" args
            | INT8     -> cf _subInt8    "-" args
            | UINT16   -> cf _subUInt16  "-" args
            | UINT32   -> cf _subUInt32  "-" args
            | UINT64   -> cf _subUInt64  "-" args
            | UINT8    -> cf _subUInt8   "-" args
            | _        -> failwithlog "ERROR"

        let fMul (args:Args) : IExpression =
            match args[0].DataType.Name with
            | FLOAT32  -> cf _mulFloat32 "*" args
            | FLOAT64  -> cf _mulFloat64 "*" args
            | INT16    -> cf _mulInt16   "*" args
            | INT32    -> cf _mulInt32   "*" args
            | INT64    -> cf _mulInt64   "*" args
            | INT8     -> cf _mulInt8    "*" args
            | UINT16   -> cf _mulUInt16  "*" args
            | UINT32   -> cf _mulUInt32  "*" args
            | UINT64   -> cf _mulUInt64  "*" args
            | UINT8    -> cf _mulUInt8   "*" args
            | _        -> failwithlog "ERROR"

        let fDiv (args:Args) : IExpression =
            match args[0].DataType.Name with
            | FLOAT32  -> cf _divFloat32 "/" args
            | FLOAT64  -> cf _divFloat64 "/" args
            | INT16    -> cf _divInt16   "/" args
            | INT32    -> cf _divInt32   "/" args
            | INT64    -> cf _divInt64   "/" args
            | INT8     -> cf _divInt8    "/" args
            | UINT16   -> cf _divUInt16  "/" args
            | UINT32   -> cf _divUInt32  "/" args
            | UINT64   -> cf _divUInt64  "/" args
            | UINT8    -> cf _divUInt8   "/" args
            | _        -> failwithlog "ERROR"

        let fAbs (args:Args) : IExpression =
            match args[0].DataType.Name with
            | FLOAT32  -> cf _absFloat32 "abs" args
            | FLOAT64  -> cf _absFloat64 "abs" args
            | INT16    -> cf _absInt16   "abs" args
            | INT32    -> cf _absInt32   "abs" args
            | INT64    -> cf _absInt64   "abs" args
            | INT8     -> cf _absInt8    "abs" args
            | UINT16   -> cf _absUInt16  "abs" args
            | UINT32   -> cf _absUInt32  "abs" args
            | UINT64   -> cf _absUInt64  "abs" args
            | UINT8    -> cf _absUInt8   "abs" args
            | _        -> failwithlog "ERROR"

        let fMod (args:Args) : IExpression =
            match args[0].DataType.Name with
            | FLOAT32  -> cf _modFloat32 "%" args
            | FLOAT64  -> cf _modFloat64 "%" args
            | INT16    -> cf _modInt16   "%" args
            | INT32    -> cf _modInt32   "%" args
            | INT64    -> cf _modInt64   "%" args
            | INT8     -> cf _modInt8    "%" args
            | UINT16   -> cf _modUInt16  "%" args
            | UINT32   -> cf _modUInt32  "%" args
            | UINT64   -> cf _modUInt64  "%" args
            | UINT8    -> cf _modUInt8   "%" args
            | _        -> failwithlog "ERROR"


        let fShiftLeft (args:Args) : IExpression =
            match args[0].DataType.Name with
            | INT16    -> cf _shiftLeftInt16   "<<<" args
            | INT32    -> cf _shiftLeftInt32   "<<<" args
            | INT64    -> cf _shiftLeftInt64   "<<<" args
            | INT8     -> cf _shiftLeftInt8    "<<<" args
            | UINT16   -> cf _shiftLeftUInt16  "<<<" args
            | UINT32   -> cf _shiftLeftUInt32  "<<<" args
            | UINT64   -> cf _shiftLeftUInt64  "<<<" args
            | UINT8    -> cf _shiftLeftUInt8   "<<<" args
            | _        -> failwithlog "ERROR"

        let fShiftRight (args:Args) : IExpression =
            match args[0].DataType.Name with
            | INT16    -> cf _shiftRightInt16   ">>>" args
            | INT32    -> cf _shiftRightInt32   ">>>" args
            | INT64    -> cf _shiftRightInt64   ">>>" args
            | INT8     -> cf _shiftRightInt8    ">>>" args
            | UINT16   -> cf _shiftRightUInt16  ">>>" args
            | UINT32   -> cf _shiftRightUInt32  ">>>" args
            | UINT64   -> cf _shiftRightUInt64  ">>>" args
            | UINT8    -> cf _shiftRightUInt8   ">>>" args
            | _        -> failwithlog "ERROR"

        let fBitwiseAnd (args:Args) : IExpression =
            match args[0].DataType.Name with
            | INT16    -> cf _bitwiseAndInt16   "&&&" args
            | INT32    -> cf _bitwiseAndInt32   "&&&" args
            | INT64    -> cf _bitwiseAndInt64   "&&&" args
            | INT8     -> cf _bitwiseAndInt8    "&&&" args
            | UINT16   -> cf _bitwiseAndUInt16  "&&&" args
            | UINT32   -> cf _bitwiseAndUInt32  "&&&" args
            | UINT64   -> cf _bitwiseAndUInt64  "&&&" args
            | UINT8    -> cf _bitwiseAndUInt8   "&&&" args
            | _        -> failwithlog "ERROR"

        let fBitwiseOr (args:Args) : IExpression =
            match args[0].DataType.Name with
            | INT16    -> cf _bitwiseOrInt16   "|||" args
            | INT32    -> cf _bitwiseOrInt32   "|||" args
            | INT64    -> cf _bitwiseOrInt64   "|||" args
            | INT8     -> cf _bitwiseOrInt8    "|||" args
            | UINT16   -> cf _bitwiseOrUInt16  "|||" args
            | UINT32   -> cf _bitwiseOrUInt32  "|||" args
            | UINT64   -> cf _bitwiseOrUInt64  "|||" args
            | UINT8    -> cf _bitwiseOrUInt8   "|||" args
            | _        -> failwithlog "ERROR"

        let fBitwiseNot (args:Args) : IExpression =
            match args[0].DataType.Name with
            | INT16    -> cf _bitwiseNotInt16   "~~~" args
            | INT32    -> cf _bitwiseNotInt32   "~~~" args
            | INT64    -> cf _bitwiseNotInt64   "~~~" args
            | INT8     -> cf _bitwiseNotInt8    "~~~" args
            | UINT16   -> cf _bitwiseNotUInt16  "~~~" args
            | UINT32   -> cf _bitwiseNotUInt32  "~~~" args
            | UINT64   -> cf _bitwiseNotUInt64  "~~~" args
            | UINT8    -> cf _bitwiseNotUInt8   "~~~" args
            | _        -> failwithlog "ERROR"

        let fBitwiseXor (args:Args) : IExpression =
            match args[0].DataType.Name with
            | INT16    -> cf _bitwiseXorInt16   "^^^" args
            | INT32    -> cf _bitwiseXorInt32   "^^^" args
            | INT64    -> cf _bitwiseXorInt64   "^^^" args
            | INT8     -> cf _bitwiseXorInt8    "^^^" args
            | UINT16   -> cf _bitwiseXorUInt16  "^^^" args
            | UINT32   -> cf _bitwiseXorUInt32  "^^^" args
            | UINT64   -> cf _bitwiseXorUInt64  "^^^" args
            | UINT8    -> cf _bitwiseXorUInt8   "^^^" args
            | _        -> failwithlog "ERROR"


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


        let fRising         args: Expression<bool> = cf _rising      FunctionNameRising args
        let fFalling        args: Expression<bool> = cf _falling     FunctionNameFalling args

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

        let _rising (_args:Args) : bool = false//failwithlog "ERROR"   //args.Select(evalArg).Cast<bool>().Expect1() |> not
        let _falling (_args:Args) : bool = false// failwithlog "ERROR"  //args.Select(evalArg).Cast<bool>().Expect1() |> not


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

