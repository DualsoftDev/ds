namespace rec Engine.Core
open System
open System.Linq
open System.Runtime.CompilerServices
open Dual.Common.Core.FS
open System.Collections.Generic

module private ExpressionHelperModule =
    let expectN (n:int) (xs:'a seq) = if xs.Count() <> n then failwith $"Wrong number of arguments: expect {n}"
    let expect1 xs = expectN 1 xs; xs.First()
    let expect2 xs = expectN 2 xs; Array.ofSeq xs
    let expectGteN (n:int) (xs:'a seq) =
        if xs.Count() < n then
            failwith $"Wrong number of arguments: expect at least {n} arguments"
        else
            xs

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
    let PseudoFunction<'T> (_args:Args):'T = failwithlog "THIS IS PSEUDO FUNCTION.  SHOULD NOT BE EVALUATED!!!!"

    let [<Literal>] FunctionNameRising  = "rising"
    let [<Literal>] FunctionNameFalling = "falling"

    /// operator 별로, arguments 가 주어졌을 때, 이를 연산하여 IExpression 을 반환하는 함수를 반환하는 함수
    let getBinaryFunction (op:string) (opndType:Type) : (Args -> IExpression) =
        match op with
        | "&&" | "||" when opndType <> typedefof<bool> -> failwith $"{op} expects bool.  Type mismatch"
        | "+" when opndType = typeof<string> -> fConcat
        | "+"  -> fAdd
        | "-"  -> fSub
        | "*"  -> fMul
        | "/"  -> fDiv
        | "%"  -> fMod

        | ">"  -> fGt
        | ">=" -> fGte
        | "<"  -> fLt
        | "<=" -> fLte
        | "=" when opndType = typeof<string> -> fEqualString
        | "="  -> fEqual
        | "!=" | "<>" -> fNotEqual

        | "&&" -> fLogicalAnd
        | "||" -> fLogicalOr

        | ">>" | ">>>" -> fShiftRight
        | "<<" | "<<<" -> fShiftLeft


        | "&" | "&&&" -> fBitwiseAnd
        | "|" | "|||" -> fBitwiseOr
        | "^" | "^^^" -> fBitwiseXor
        | "~" | "~~~" -> failwith "Not binary operation"

        | _ -> failwith $"Undefined operator for getBinaryFunction({op})"

    let createBinaryExpression (opnd1:IExpression) (op:string) (opnd2:IExpression) : IExpression =
        let t1 = opnd1.DataType

        let args = [opnd1; opnd2]
        verifyArgumentsTypes op args
        getBinaryFunction op t1 args  |> iexpr

    let createUnaryExpression (op:string) (opnd:IExpression) : IExpression =
        (* unary operator 처리.
           - '! $myTag' 처럼  괄호 없이도 사용가능한 것들만 정의한다.
           - 괄호도 허용하려면 createCustomFunctionExpression 에서도 정의해야 한다. '! ($myTag)'
         *)
        match op with
        | ("~" | "~~~" ) -> fBitwiseNot [opnd]
        | "!"  -> fbLogicalNot [opnd]
        | _ ->
            failwith $"Undefined operator for createUnaryExpression({op})"

    let createCustomFunctionExpression (funName:string) (args:Args) : IExpression =
        verifyArgumentsTypes funName args
        let t = args[0].DataType.Name

        match funName with
        | ("+" | "add") -> fAdd args
        | ("-" | "sub") -> fSub args
        | ("*" | "mul") -> fMul args
        | ("/" | "div") -> fDiv args

        | (">"  | "gt")  -> fbGt args
        | (">=" | "gte") -> fbGte args
        | ("<"  | "lt")  -> fbLt args
        | ("<=" | "lte") -> fbLte args

        | ("="  | "equal") when t = STRING -> fbEqualString args
        | ("="  | "equal") -> fbEqual args
        | ("!=" | "<>" | "notEqual") when t = STRING -> fbNotEqualString args
        | ("!=" | "<>" | "^^" | "notEqual") -> fbNotEqual args

        | ("<<" | "<<<" | "shiftLeft") -> fShiftLeft args
        | (">>" | ">>>" | "shiftRight") -> fShiftLeft args

        | ("&&" | "and") -> fbLogicalAnd args
        | ("||" | "or")  -> fbLogicalOr  args
        | ("!"  | "not") -> fbLogicalNot args        // 따로 or 같이??? neg 는 contact 이나 coil 하나만 받아서 rung 생성하는 용도, not 은 expression 을 받아서 평가하는 용도

        | ("&" | "&&&") -> fBitwiseAnd  args
        | ("|" | "|||") -> fBitwiseOr   args
        | ("^" | "^^^") -> fBitwiseXor  args
        | ("~" | "~~~") -> fBitwiseNot  args

        | FunctionNameRising  -> fbRising  args
        | FunctionNameFalling -> fbFalling args
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
            | "createXgkCTU" | "createXgkCTD" | "createXgkCTUD" | "createXgkCTR"
            | "createWinCTU" | "createWinCTD" | "createWinCTUD" | "createWinCTR"
            | "createAbCTU"  | "createAbCTD"  | "createAbCTUD"  | "createAbCTR" ) ->
                DuFunction { FunctionBody=PseudoFunction<Counter>; Name=funName; Arguments=args }
        | (   "createXgiTON" | "createXgiTOF" | "createXgiCRTO"
            | "createXgkTON" | "createXgkTOF" | "createXgkCRTO"
            | "createWinTON" | "createWinTOF" | "createWinCRTO"
            | "createAbTON"  | "createAbTOF"  | "createAbCRTO") ->
                DuFunction { FunctionBody=PseudoFunction<Timer>; Name=funName; Arguments=args }
        | "createTag" ->
                DuFunction { FunctionBody=PseudoFunction<ITag>; Name=funName; Arguments=args }

        | _ -> failwith $"NOT yet: {funName}"

    /// Create function expression
    let private cf (f:Args->'T) (name:string) (args:Args) =
        DuFunction { FunctionBody=f; Name=name; Arguments=args}

    [<AutoOpen>]
    module internal FunctionImpl =
        [<Extension>] // type SeqExt =
        type SeqExt =
            [<Extension>] static member ExpectGteN(xs:'a seq, n) = expectGteN n xs
            [<Extension>] static member Expect1(xs:'a seq) = expect1 xs
            [<Extension>] static member Expect2(xs:'a seq) = expect2 xs
            [<Extension>]
            static member ExpectTyped2<'U, 'V>(Array(xs:IExpression [])) =
                let arg0 = xs[0] |> evalTo<'U>
                let arg1 = xs[1] |> evalTo<'V>
                arg0, arg1

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
        let castArgs<'T>  (args:Args) = args.Select(fun x-> x.BoxedEvaluatedValue :?> 'T)
        let castArg<'T>   (args:Args) = args.ExactlyOne().BoxedEvaluatedValue :?> 'T
        let shiftArgs<'T> (args:Args) = args.ExpectTyped2<'T, int>()

        let fAdd (args:Args) : IExpression =
            expectGteN 2 args |> ignore
            match args[0].DataType.Name with
            | FLOAT32  -> cf (castArgs<single> >> Seq.reduce(+)) "+"  args
            | FLOAT64  -> cf (castArgs<double> >> Seq.reduce(+)) "+"  args
            | INT16    -> cf (castArgs<int16>  >> Seq.reduce(+)) "+"  args
            | INT32    -> cf (castArgs<int32>  >> Seq.reduce(+)) "+"  args
            | INT64    -> cf (castArgs<int64>  >> Seq.reduce(+)) "+"  args
            | INT8     -> cf (castArgs<int8 >  >> Seq.reduce(+)) "+"  args
            | UINT16   -> cf (castArgs<uint16> >> Seq.reduce(+)) "+"  args
            | UINT32   -> cf (castArgs<uint32> >> Seq.reduce(+)) "+"  args
            | UINT64   -> cf (castArgs<uint64> >> Seq.reduce(+)) "+"  args
            | UINT8    -> cf (castArgs<uint8 > >> Seq.reduce(+)) "+"  args
            | _        -> failwithlog "ERROR"

        let fSub (args:Args) : IExpression =
            expectGteN 2 args |> ignore
            match args[0].DataType.Name with
            | FLOAT32  -> cf (castArgs<single> >> Seq.reduce(-)) "-"  args
            | FLOAT64  -> cf (castArgs<double> >> Seq.reduce(-)) "-"  args
            | INT16    -> cf (castArgs<int16>  >> Seq.reduce(-)) "-"  args
            | INT32    -> cf (castArgs<int32>  >> Seq.reduce(-)) "-"  args
            | INT64    -> cf (castArgs<int64>  >> Seq.reduce(-)) "-"  args
            | INT8     -> cf (castArgs<int8 >  >> Seq.reduce(-)) "-"  args
            | UINT16   -> cf (castArgs<uint16> >> Seq.reduce(-)) "-"  args
            | UINT32   -> cf (castArgs<uint32> >> Seq.reduce(-)) "-"  args
            | UINT64   -> cf (castArgs<uint64> >> Seq.reduce(-)) "-"  args
            | UINT8    -> cf (castArgs<uint8 > >> Seq.reduce(-)) "-"  args
            | _        -> failwithlog "ERROR"


        let fMul (args:Args) : IExpression =
            expectGteN 2 args |> ignore
            match args[0].DataType.Name with
            | FLOAT32  -> cf (castArgs<single> >> Seq.reduce(*)) "*"  args
            | FLOAT64  -> cf (castArgs<double> >> Seq.reduce(*)) "*"  args
            | INT16    -> cf (castArgs<int16>  >> Seq.reduce(*)) "*"  args
            | INT32    -> cf (castArgs<int32>  >> Seq.reduce(*)) "*"  args
            | INT64    -> cf (castArgs<int64>  >> Seq.reduce(*)) "*"  args
            | INT8     -> cf (castArgs<int8 >  >> Seq.reduce(*)) "*"  args
            | UINT16   -> cf (castArgs<uint16> >> Seq.reduce(*)) "*"  args
            | UINT32   -> cf (castArgs<uint32> >> Seq.reduce(*)) "*"  args
            | UINT64   -> cf (castArgs<uint64> >> Seq.reduce(*)) "*"  args
            | UINT8    -> cf (castArgs<uint8 > >> Seq.reduce(*)) "*"  args
            | _        -> failwithlog "ERROR"

        let fDiv (args:Args) : IExpression =
            expectGteN 2 args |> ignore
            match args[0].DataType.Name with
            | FLOAT32  -> cf (castArgs<single> >> Seq.reduce(/)) "/"  args
            | FLOAT64  -> cf (castArgs<double> >> Seq.reduce(/)) "/"  args
            | INT16    -> cf (castArgs<int16>  >> Seq.reduce(/)) "/"  args
            | INT32    -> cf (castArgs<int32>  >> Seq.reduce(/)) "/"  args
            | INT64    -> cf (castArgs<int64>  >> Seq.reduce(/)) "/"  args
            | INT8     -> cf (castArgs<int8 >  >> Seq.reduce(/)) "/"  args
            | UINT16   -> cf (castArgs<uint16> >> Seq.reduce(/)) "/"  args
            | UINT32   -> cf (castArgs<uint32> >> Seq.reduce(/)) "/"  args
            | UINT64   -> cf (castArgs<uint64> >> Seq.reduce(/)) "/"  args
            | UINT8    -> cf (castArgs<uint8 > >> Seq.reduce(/)) "/"  args
            | _        -> failwithlog "ERROR"

        let fAbs (args:Args) : IExpression =
            expect1 args |> ignore
            match args[0].DataType.Name with
            | FLOAT32  -> cf (castArg<single> >> Math.Abs) "abs"  args
            | FLOAT64  -> cf (castArg<double> >> Math.Abs) "abs"  args
            | INT16    -> cf (castArg<int16>  >> Math.Abs) "abs"  args
            | INT32    -> cf (castArg<int32>  >> Math.Abs) "abs"  args
            | INT64    -> cf (castArg<int64>  >> Math.Abs) "abs"  args
            | INT8     -> cf (castArg<int8 >  >> Math.Abs) "abs"  args
            | UINT16   -> cf (castArg<uint16> >> Math.Abs) "abs"  args
            | UINT32   -> cf (castArg<uint32> >> Math.Abs) "abs"  args
            | UINT64   -> cf (castArg<uint64> >> Math.Abs) "abs"  args
            | UINT8    -> cf (castArg<uint8 > >> Math.Abs) "abs"  args
            | _        -> failwithlog "ERROR"

        let fMod (args:Args) : IExpression =
            expectGteN 2 args |> ignore
            match args[0].DataType.Name with
            | FLOAT32  -> cf (castArgs<single> >> Seq.reduce(%)) "%"  args
            | FLOAT64  -> cf (castArgs<double> >> Seq.reduce(%)) "%"  args
            | INT16    -> cf (castArgs<int16>  >> Seq.reduce(%)) "%"  args
            | INT32    -> cf (castArgs<int32>  >> Seq.reduce(%)) "%"  args
            | INT64    -> cf (castArgs<int64>  >> Seq.reduce(%)) "%"  args
            | INT8     -> cf (castArgs<int8 >  >> Seq.reduce(%)) "%"  args
            | UINT16   -> cf (castArgs<uint16> >> Seq.reduce(%)) "%"  args
            | UINT32   -> cf (castArgs<uint32> >> Seq.reduce(%)) "%"  args
            | UINT64   -> cf (castArgs<uint64> >> Seq.reduce(%)) "%"  args
            | UINT8    -> cf (castArgs<uint8 > >> Seq.reduce(%)) "%"  args
            | _        -> failwithlog "ERROR"


        let fShiftLeft (args:Args) : IExpression =
            expect2 args |> ignore
            match args[0].DataType.Name with
            | INT16    -> cf (fun xs -> shiftArgs<int16>  xs ||> (<<<)) "<<<"  args
            | INT32    -> cf (fun xs -> shiftArgs<int32>  xs ||> (<<<)) "<<<"  args
            | INT64    -> cf (fun xs -> shiftArgs<int64>  xs ||> (<<<)) "<<<"  args
            | INT8     -> cf (fun xs -> shiftArgs<int8 >  xs ||> (<<<)) "<<<"  args
            | UINT16   -> cf (fun xs -> shiftArgs<uint16> xs ||> (<<<)) "<<<"  args
            | UINT32   -> cf (fun xs -> shiftArgs<uint32> xs ||> (<<<)) "<<<"  args
            | UINT64   -> cf (fun xs -> shiftArgs<uint64> xs ||> (<<<)) "<<<"  args
            | UINT8    -> cf (fun xs -> shiftArgs<uint8 > xs ||> (<<<)) "<<<"  args
            | _        -> failwithlog "ERROR"

        let fShiftRight (args:Args) : IExpression =
            expect2 args |> ignore
            match args[0].DataType.Name with
            | INT16    -> cf (fun xs -> shiftArgs<int16>  xs ||> (>>>)) ">>>"  args
            | INT32    -> cf (fun xs -> shiftArgs<int32>  xs ||> (>>>)) ">>>"  args
            | INT64    -> cf (fun xs -> shiftArgs<int64>  xs ||> (>>>)) ">>>"  args
            | INT8     -> cf (fun xs -> shiftArgs<int8 >  xs ||> (>>>)) ">>>"  args
            | UINT16   -> cf (fun xs -> shiftArgs<uint16> xs ||> (>>>)) ">>>"  args
            | UINT32   -> cf (fun xs -> shiftArgs<uint32> xs ||> (>>>)) ">>>"  args
            | UINT64   -> cf (fun xs -> shiftArgs<uint64> xs ||> (>>>)) ">>>"  args
            | UINT8    -> cf (fun xs -> shiftArgs<uint8 > xs ||> (>>>)) ">>>"  args
            | _        -> failwithlog "ERROR"


        let fBitwiseAnd (args:Args) : IExpression =
            expectGteN 2 args |> ignore
            match args[0].DataType.Name with
            | INT16    -> cf (castArgs<int16>  >> Seq.reduce(&&&)) "&&&"  args
            | INT32    -> cf (castArgs<int32>  >> Seq.reduce(&&&)) "&&&"  args
            | INT64    -> cf (castArgs<int64>  >> Seq.reduce(&&&)) "&&&"  args
            | INT8     -> cf (castArgs<int8 >  >> Seq.reduce(&&&)) "&&&"  args
            | UINT16   -> cf (castArgs<uint16> >> Seq.reduce(&&&)) "&&&"  args
            | UINT32   -> cf (castArgs<uint32> >> Seq.reduce(&&&)) "&&&"  args
            | UINT64   -> cf (castArgs<uint64> >> Seq.reduce(&&&)) "&&&"  args
            | UINT8    -> cf (castArgs<uint8 > >> Seq.reduce(&&&)) "&&&"  args
            | _        -> failwithlog "ERROR"

        let fBitwiseOr (args:Args) : IExpression =
            expectGteN 2 args |> ignore
            match args[0].DataType.Name with
            | INT16    -> cf (castArgs<int16>  >> Seq.reduce(|||)) "|||"  args
            | INT32    -> cf (castArgs<int32>  >> Seq.reduce(|||)) "|||"  args
            | INT64    -> cf (castArgs<int64>  >> Seq.reduce(|||)) "|||"  args
            | INT8     -> cf (castArgs<int8 >  >> Seq.reduce(|||)) "|||"  args
            | UINT16   -> cf (castArgs<uint16> >> Seq.reduce(|||)) "|||"  args
            | UINT32   -> cf (castArgs<uint32> >> Seq.reduce(|||)) "|||"  args
            | UINT64   -> cf (castArgs<uint64> >> Seq.reduce(|||)) "|||"  args
            | UINT8    -> cf (castArgs<uint8 > >> Seq.reduce(|||)) "|||"  args
            | _        -> failwithlog "ERROR"

        let fBitwiseXor (args:Args) : IExpression =
            expectGteN 2 args |> ignore
            match args[0].DataType.Name with
            | INT16    -> cf (castArgs<int16>  >> Seq.reduce(^^^)) "^^^"  args
            | INT32    -> cf (castArgs<int32>  >> Seq.reduce(^^^)) "^^^"  args
            | INT64    -> cf (castArgs<int64>  >> Seq.reduce(^^^)) "^^^"  args
            | INT8     -> cf (castArgs<int8 >  >> Seq.reduce(^^^)) "^^^"  args
            | UINT16   -> cf (castArgs<uint16> >> Seq.reduce(^^^)) "^^^"  args
            | UINT32   -> cf (castArgs<uint32> >> Seq.reduce(^^^)) "^^^"  args
            | UINT64   -> cf (castArgs<uint64> >> Seq.reduce(^^^)) "^^^"  args
            | UINT8    -> cf (castArgs<uint8 > >> Seq.reduce(^^^)) "^^^"  args
            | _        -> failwithlog "ERROR"

        let fBitwiseNot (args:Args) : IExpression =
            expect1 args |> ignore
            match args[0].DataType.Name with
            | INT16    -> cf (castArg<int16>  >> (~~~)) "~~~"  args
            | INT32    -> cf (castArg<int32>  >> (~~~)) "~~~"  args
            | INT64    -> cf (castArg<int64>  >> (~~~)) "~~~"  args
            | INT8     -> cf (castArg<int8 >  >> (~~~)) "~~~"  args
            | UINT16   -> cf (castArg<uint16> >> (~~~)) "~~~"  args
            | UINT32   -> cf (castArg<uint32> >> (~~~)) "~~~"  args
            | UINT64   -> cf (castArg<uint64> >> (~~~)) "~~~"  args
            | UINT8    -> cf (castArg<uint8 > >> (~~~)) "~~~"  args
            | _        -> failwithlog "ERROR"


        let fConcat         args = cf _concat         "+"      args

        let fEqual          args: IExpression = cf _equal          "="  args
        let fNotEqual       args: IExpression = cf _notEqual       "!=" args
        let fGt             args: IExpression = cf _gt             ">"  args
        let fLt             args: IExpression = cf _lt             "<"  args
        let fGte            args: IExpression = cf _gte            ">=" args
        let fLte            args: IExpression = cf _lte            "<=" args
        let fEqualString    args: IExpression = cf _equalString    "="  args
        let fNotEqualString args: IExpression = cf _notEqualString "!=" args
        let fLogicalAnd     args: IExpression = cf _logicalAnd     "&&" args
        let fLogicalOr      args: IExpression = cf _logicalOr      "||" args
        let fLogicalNot     args: IExpression = cf _logicalNot     "!"  args
        let fRising         args: IExpression = cf _rising      FunctionNameRising args
        let fFalling        args: IExpression = cf _falling     FunctionNameFalling args

        (* FB: Functions that returns Expression<Bool> *)
        let fbEqual          args: Expression<bool> = cf _equal          "="  args
        let fbNotEqual       args: Expression<bool> = cf _notEqual       "!=" args
        let fbGt             args: Expression<bool> = cf _gt             ">"  args
        let fbLt             args: Expression<bool> = cf _lt             "<"  args
        let fbGte            args: Expression<bool> = cf _gte            ">=" args
        let fbLte            args: Expression<bool> = cf _lte            "<=" args
        let fbEqualString    args: Expression<bool> = cf _equalString    "="  args
        let fbNotEqualString args: Expression<bool> = cf _notEqualString "!=" args
        let fbLogicalAnd     args: Expression<bool> = cf _logicalAnd     "&&" args
        let fbLogicalOr      args: Expression<bool> = cf _logicalOr      "||" args
        let fbLogicalNot     args: Expression<bool> = cf _logicalNot     "!"  args

        (* FB: Functions that returns Expression<Bool> *)
        let fbRising        args: Expression<bool> = cf _rising      FunctionNameRising args
        let fbFalling       args: Expression<bool> = cf _falling     FunctionNameFalling args

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


