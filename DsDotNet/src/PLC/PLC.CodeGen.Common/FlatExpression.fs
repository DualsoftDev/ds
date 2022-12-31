namespace PLC.CodeGen.Common

open System.Diagnostics
open Engine.Common.FS
open Engine.Core

[<AutoOpen>]
module FlatExpressionModule =
    type Op =
        | And | Or | Neg | OpUnit
    with
        member x.ToText() = sprintf "%A" x
        member x.Negate() =
            match x with
            | And -> Or
            | Or -> And
            | Neg -> OpUnit
            | OpUnit -> Neg

    type TrueValue() =
        interface IExpressionTerminal with
            member x.PLCTagName = "TRUE"
    type FalseValue() =
        interface IExpressionTerminal with
            member x.PLCTagName = "FALSE"

    [<DebuggerDisplay("{ToText()}")>]
    type FlatExpression =
        /// pulse identifier 및 negation 여부 (pulse coil 은 지원하지 않을 예정)
        | FlatTerminal  of tag:IExpressionTerminal * pulse:bool * negated:bool

        /// N-ary Expressions : And / Or 및 terms
        | FlatNary    of Op * FlatExpression list
        | FlatZero
    with
        interface IFlatExpression
        member x.ToText() =
            match x with
            | FlatTerminal(value, pulse, neg) -> sprintf "%s%s" (if neg then "!" else "") (value.PLCTagName)
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

    let rec flattenExpression (expression:IExpression) : IFlatExpression =
        let expr = expression :?> Expression<bool>
        let literalBool2Terminal (b:bool) : IExpressionTerminal = if b then TrueValue() else FalseValue()
        match expr with
        | DuTerminal (DuTag t) -> FlatTerminal(t, false, false)
        | DuTerminal (DuLiteral b) -> FlatTerminal( literalBool2Terminal b, false, false)
        | DuTerminal  _ -> failwith "ERROR"
        | DuFunction fs ->
            let op =
                match fs.Name with
                | "&&" -> Op.And
                | "||" -> Op.Or
                | "!" -> Op.Neg
                | _ -> failwith "ERROR"
            FlatNary(op, fs.Arguments |> map flattenExpression |> Seq.cast<FlatExpression> |> Seq.toList)

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
