namespace PLC.CodeGen.Common

open System.Diagnostics
open Dual.Common.Core.FS
open Engine.Core

[<AutoOpen>]
module FlatExpressionModule =
    type Op =
        | And
        | Or
        | Neg

        | RisingAfter
        | FallingAfter

        | OpUnit // Logical XOR 는 function 인 '<>' 로 구현됨
        | OpCompare of operator: string
        | OpArithmetic of operator: string

        member x.ToText() = sprintf "%A" x

        member x.Negate() =
            match x with
            | And -> Or
            | Or -> And
            | Neg -> OpUnit
            | OpUnit -> Neg
            | OpCompare op ->
                match op with
                | ">" -> "<="
                | ">=" -> "<"
                | "<" -> ">="
                | "<=" -> ">"
                | "==" -> "!="
                | "!=" -> "=="
                | _ -> failwithlog "ERROR"
                |> OpCompare
            | OpArithmetic _ -> failwith "ERROR: Negation not supported for Arithmetic operator."
            | _ -> failwith "ERROR"

    [<AbstractClass>]
    type BoolLiteralValue() =
        interface IExpressionizableTerminal with
            member x.ToText() = x.ToText()
        interface IType with
            member x.DataType = typedefof<bool>
        interface ITerminal with
            member x.Variable = None
            member x.Literal = Some(x:>IExpressionizableTerminal)
        abstract ToText: unit -> string
        default x.ToText() = "TRUE"

    type TrueValue() =
        inherit BoolLiteralValue()
        override x.ToText() = "TRUE"

    type FalseValue() =
        inherit BoolLiteralValue()
        override x.ToText() = "FALSE"

    [<DebuggerDisplay("{ToText()}")>]
    type FlatExpression =
        /// pulse identifier 및 negation 여부 (pulse coil 은 지원하지 않을 예정)
        ///
        /// pulse : None 이면 pulse 없음, Some true 이면 rising edge, Some false 이면 falling edge
        | FlatTerminal of terminal: IExpressionizableTerminal * pulse: bool option * negated: bool

        /// N-ary Expressions : And / Or 및 terms
        | FlatNary of Op * FlatExpression list

        interface IFlatExpression

        interface IType with
            member x.DataType = x.DataType

        member x.DataType =
            match x with
            | FlatTerminal(terminal, _pulse, _neg) -> terminal.DataType
            | FlatNary(_op, arg0::_) -> arg0.DataType
            | _ -> failwithlog "ERROR"

        member x.ToText() =
            match x with
            | FlatTerminal(value, _pulse, neg) -> sprintf "%s%s" (if neg then "!" else "") (value.ToText())
            | FlatNary(op, terms) ->
                let termsStr = terms |> Seq.map (fun t -> t.ToText()) |> String.concat ", "
                sprintf "%s(%s)" (op.ToText()) termsStr

        member x.Negate() =
            match x with
            | FlatTerminal(value, pulse, neg) -> FlatTerminal(value, pulse, not neg)
            | FlatNary(op, [ FlatTerminal(t, p, n) ]) when op = Neg || op = OpUnit ->
                let negated = if op = Neg then n else not n
                FlatTerminal(t, p, negated)

            | FlatNary(_op, [ FlatTerminal(_t, _p, _n) ]) -> failwithlog "ERROR"

            | FlatNary(op, terms) ->
                let opNeg = op.Negate()
                let termsNeg = terms |> map (fun t -> t.Negate())
                FlatNary(opNeg, termsNeg)

    let rec flattenExpressionT (expression: IExpression<'T>) : IFlatExpression =
        match expression with
        | :? Expression<'T> as express ->
            match express with
            | DuTerminal(DuVariable t) -> FlatTerminal(t, None, false)
            | DuTerminal(DuLiteral b) -> FlatTerminal(b, None, false)

            (* rising/falling/negation 은 function 으로 구현되어 있으며,
               해당 function type 에 따라서 risng/falling/negation 의 contact/coil 을 생성한다.
               (Terminal<'T> 이 generic 이어서 DuTag 에 bool type 으로 제한 할 수 없음.
                Terminal<'T>.Evaluate() 가 bool type 으로 제한됨 )
             *)
            | DuFunction { Name = (FunctionNameRising | FunctionNameFalling) as n
                           Arguments = [ (:? Expression<bool> as arg) ] } ->
                let positivePulse = n = FunctionNameRising |> Some
                match arg with
                | DuTerminal(DuVariable v) -> FlatTerminal(v, positivePulse, false)
                | DuTerminal(DuLiteral b) -> FlatTerminal(b, positivePulse, false)
                | DuFunction ({ Name = "!"
                                Arguments = (:? Expression<bool> as arg0)::[]} as _f) ->
                    match arg0 with
                    | DuTerminal(DuVariable v) -> FlatTerminal(v, positivePulse, true)
                    | DuTerminal(DuLiteral b) -> FlatTerminal(b, positivePulse, true)
                    | _ -> failwithlog "ERROR"
                    //FlatTerminal(b, (n = FunctionNameRising), false)

                | _ -> failwithlog "ERROR"
            | DuFunction fs ->
                let op =
                    match fs.Name with
                    | "&&" -> Op.And
                    | "||" -> Op.Or
                    | "!" -> Op.Neg
                    | FunctionNameRisingAfter -> Op.RisingAfter
                    | FunctionNameFallingAfter -> Op.FallingAfter

                    | IsOpC _ -> // XGK 일때만 유효
                        Op.OpCompare fs.Name

                    | IsOpA _ // -> Op.OpArithmetic fs.Name
                    | _ -> failwithlog "ERROR"

                let flatArgs = fs.Arguments |> map flattenExpression |> List.cast<FlatExpression>
                FlatNary(op, flatArgs)

        //| :? DuTerminal(DuVariable (v:TypedValueStorage<'T'>)) -> FlatTerminal(v, None, false)

        | _ -> failwithlog "Not yet for non boolean expression"

    // <kwak> IExpression<'T> vs IExpression : 강제 변환
    and flattenExpression (expression: IExpression) : IFlatExpression =
        match expression with
        | :? IExpression<bool> as exp -> flattenExpressionT exp
        | :? IExpression<int8> as exp -> flattenExpressionT exp
        | :? IExpression<uint8> as exp -> flattenExpressionT exp
        | :? IExpression<int16> as exp -> flattenExpressionT exp
        | :? IExpression<uint16> as exp -> flattenExpressionT exp
        | :? IExpression<int32> as exp -> flattenExpressionT exp
        | :? IExpression<uint32> as exp -> flattenExpressionT exp
        | :? IExpression<int64> as exp -> flattenExpressionT exp
        | :? IExpression<uint64> as exp -> flattenExpressionT exp
        | :? IExpression<single> as exp -> flattenExpressionT exp
        | :? IExpression<double> as exp -> flattenExpressionT exp
        | :? IExpression<string> as exp -> flattenExpressionT exp
        | :? IExpression<char> as exp -> flattenExpressionT exp

        | _ -> failwithlog "NOT yet"


    /// expression 이 차지하는 가로, 세로 span 의 width 와 height 를 반환한다.
    let precalculateSpan (expr: FlatExpression) =
        let rec helper (expr: FlatExpression) : int * int =
            match expr with
            | FlatTerminal _ -> 1, 1
            | FlatNary(And, ands) ->
                let spanXYs = ands |> map helper
                let spanX = spanXYs |> map fst |> List.sum
                let spanY = spanXYs |> map snd |> List.max
                spanX, spanY
            | FlatNary(Or, ors) ->
                let spanXYs = ors |> map helper
                let spanX = spanXYs |> map fst |> List.max
                let spanY = spanXYs |> map snd |> List.sum
                spanX, spanY
            | FlatNary(Neg, [ neg ]) -> helper neg

            | FlatNary(risingOrFallingAfter, args) when risingOrFallingAfter = RisingAfter || risingOrFallingAfter = FallingAfter ->
                let spanXYs = args |> map helper
                let spanX = (spanXYs |> map fst |> List.sum) + 1
                let spanY = spanXYs |> map snd |> List.max
                spanX, spanY

            | _ -> failwithlog "ERROR"

        helper expr

    /// 우측으로 바로 function block 을 붙일 수 있는지 검사.
    /// false 반환 시, hLine (hypen) 을 적어도 하나 추가해야 function blcok 을 붙일 수 있다.
    (*
        false 반환 case
            - toplevel 이 OR function
            - toplevel 이 AND 이고, AND 의 마지막이 OR function
     *)
    let rec isFunctionBlockConnectable (expr: FlatExpression) =
        match expr with
        | (FlatTerminal _ | FlatNary(Neg, _)) -> true
        | FlatNary(And, ands) -> ands |> List.last |> isFunctionBlockConnectable
        | FlatNary(Or, _) -> false
        | _ -> failwithlog "ERROR"
