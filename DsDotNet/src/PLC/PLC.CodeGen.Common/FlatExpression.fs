namespace PLC.CodeGen.Common

open System.Diagnostics
open Engine.Common.FS
open Engine.Core

[<AutoOpen>]
module FlatExpressionModule =
    type Op =
        | And | Or | Neg | OpUnit
        | OpCompare of operator:string
        | OpArithematic of operator:string
    with
        member x.ToText() = sprintf "%A" x
        member x.Negate() =
            match x with
            | And -> Or
            | Or -> And
            | Neg -> OpUnit
            | OpUnit -> Neg

    type TrueValue() =
        interface IExpressionizableTerminal with
            member x.ToText() = "TRUE"
    type FalseValue() =
        interface IExpressionizableTerminal with
            member x.ToText() = "FALSE"

    [<DebuggerDisplay("{ToText()}")>]
    type FlatExpression =
        /// pulse identifier 및 negation 여부 (pulse coil 은 지원하지 않을 예정)
        | FlatTerminal  of tag:IExpressionizableTerminal * pulse:bool * negated:bool

        /// N-ary Expressions : And / Or 및 terms
        | FlatNary    of Op * FlatExpression list
        | FlatZero
    with
        interface IFlatExpression
        member x.ToText() =
            match x with
            | FlatTerminal(value, pulse, neg) -> sprintf "%s%s" (if neg then "!" else "") (value.ToText())
            | FlatNary(op, terms) ->
                let termsStr =
                    terms
                    |> Seq.map (fun t -> t.ToText())
                    |> String.concat ", "
                sprintf "%s(%s)" (op.ToText()) termsStr
            | FlatZero -> ""
        member x.Negate() =
            match x with
            | FlatTerminal(value, pulse, neg) -> FlatTerminal(value, pulse, not neg)
            | FlatNary(op, FlatTerminal(t, p, n)::[]) when op = Neg || op = OpUnit ->
                let negated = if op = Neg then n else not n
                FlatTerminal(t, p, negated)

            | FlatNary(op, FlatTerminal(t, p, n)::[]) -> failwith "ERROR"

            | FlatNary(op, terms) ->
                let opNeg = op.Negate()
                let termsNeg = terms |> map (fun t -> t.Negate())
                FlatNary(opNeg, termsNeg)
            | FlatZero -> FlatZero

    let rec flattenExpressionT (expression:IExpression<'T>) : IFlatExpression =
        match expression with
        | :? Expression<'T> as express ->
            match express with
            | DuTerminal (DuVariable t) -> FlatTerminal(t, false, false)
            | DuTerminal (DuLiteral b) -> FlatTerminal(b, false, false)

            (* rising/falling/negation 은 function 으로 구현되어 있으며,
               해당 function type 에 따라서 risng/falling/negation 의 contact/coil 을 생성한다.
               (Terminal<'T> 이 generic 이어서 DuTag 에 bool type 으로 제한 할 수 없음.
                Terminal<'T>.Evaluate() 가 bool type 으로 제한됨 )
             *)
            | DuFunction {FunctionBody = f; Name = n; Arguments = (:? Expression<bool> as arg)::[]}
                when n = FunctionNameRising || n = FunctionNameFalling ->
                    match arg with
                    | DuTerminal (DuVariable t) -> FlatTerminal(t, true, n = FunctionNameFalling)
                    | _ -> failwith "ERROR"
            | DuFunction fs ->
                let op =
                    match fs.Name with
                    | "&&" -> Op.And
                    | "||" -> Op.Or
                    | "!" -> Op.Neg
                    | (">" | "<" | ">=" | "<=" | "=" | "!=" ) -> Op.OpCompare fs.Name
                    | ("+" | "-" | "*" | "/" ) -> Op.OpArithematic fs.Name
                    | _ -> failwith "ERROR"
                let flatArgs =
                    fs.Arguments
                    |> map flattenExpression
                    |> List.cast<FlatExpression>
                FlatNary(op, flatArgs)
        | _ ->
            failwith "Not yet for non boolean expression"

    // <kwak> IExpression<'T> vs IExpression : 강제 변환
    and flattenExpression (expression:IExpression) : IFlatExpression =
        match expression with
        | :? IExpression<bool> as exp -> flattenExpressionT exp
        | :? IExpression<int> as exp -> flattenExpressionT exp
        | :? IExpression<uint> as exp -> flattenExpressionT exp
        | _ -> failwith "NOT yet"

    ///// expression 이 차지하는 가로, 세로 span 의 width 와 height 를 반환한다.
    //let precalculateSpan (expr:FlatExpression) =
    //    let rec helper (expr:FlatExpression): int*int =
    //        match expr with
    //        | FlatTerminal _ -> 1, 1
    //        | FlatNary(And, ands) ->
    //            let spanXYs = ands |> map helper
    //            let spanX = spanXYs |> map fst |> List.sum
    //            let spanY = spanXYs |> map snd |> List.max
    //            spanX, spanY
    //        | FlatNary(Or, ors) ->
    //            let spanXYs = ors |> map helper
    //            let spanX = spanXYs |> map fst |> List.max
    //            let spanY = spanXYs |> map snd |> List.sum
    //            spanX, spanY
    //        | FlatNary(Neg, neg::[]) ->
    //            helper neg
    //        | _ -> failwith "ERROR"
    //    helper expr
