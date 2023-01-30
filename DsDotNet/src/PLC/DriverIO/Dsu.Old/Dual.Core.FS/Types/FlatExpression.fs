namespace Old.Dual.Core.Types

open System.Diagnostics
open System.Runtime.CompilerServices
open Old.Dual.Common


/// Ladder diagram 을 그리기 위해 expression 을 최대한 flat 하게 변경한 구조
[<DebuggerDisplay("{ToText()}")>]
type FlatExpression =
    /// identifier 및 negation 여부
    | FlatTerminal  of IExpressionTerminal * bool * bool

    /// N-ary Expressions : And / Or 및 terms
    | FlatNary    of Op * FlatExpression list
    | FlatZero
    with
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


module FlatExpressionM =
    /// 이항 연산자 기반의 expression 을 flat expression 으로 변경
    let flatten expr =
        let rec flattenHelper (expr:Expression) negate =
            match Expression.flattenNegate expr with
            | Terminal(ident) when (ident :? PulseTerminal) -> FlatTerminal(ident, true, negate)
            | Terminal(ident) -> FlatTerminal(ident, false, negate)
            //| Unary(Neg, Terminal(ident)) -> FlatTerminal(ident, not negate)
            | Unary(Neg, exp) -> flattenHelper exp true
            | Binary(l, op, r) ->
                let left = flattenHelper l false
                let right = flattenHelper r false
                match left, right with
                | FlatTerminal(_), FlatNary(fop, fexp) when op = fop -> FlatNary(op, left::fexp)
                | FlatNary(fop, fexp), FlatTerminal(_) when op = fop -> FlatNary(op, [yield! fexp; right])
                //| FlatTerminal(_), FlatTerminal(_) -> FlatNary(op, [left; right])
                | _ -> FlatNary(op, [left; right])
            | Zero -> FlatZero
            | _ -> failwithlog "Unknown"
        flattenHelper expr false

    /// FlatExpression 을 Expression 으로 변환
    let flat2binary expr =
        let mkBinary operator operands =
            operands |> Seq.reduce (fun x y -> Binary(x, operator, y))
        let rec flat2binaryHelper (expr:FlatExpression) =
            match expr with
            | FlatTerminal(ident, _, false) -> Terminal(ident)
            | FlatTerminal(ident, _, true) -> Unary(Neg, Terminal(ident))
            | FlatNary(op, opnds) -> opnds |> List.map flat2binaryHelper |> mkBinary op
            | FlatZero -> Expression.Zero
        flat2binaryHelper expr

    let createNary op childExprs = FlatNary(op, childExprs |> List.ofSeq)

    let tryGetOperatorAndTerms fexp =
        match fexp with
        | FlatNary(op, terms) -> Some (op, terms)
        | _ -> None

    let tryGetTerminals fexp =
        match fexp with
        | FlatTerminal(tag, pulse, neg) -> Some (tag, neg)
        | _ -> None
    let addTerms fexp terms =
        match fexp with
        | FlatNary(op, oldTerms) -> FlatNary(op, oldTerms@@terms |> List.ofSeq)
        | _ -> failwith "Can't add terms"


    let rec clone fexp =
        match fexp with
        | FlatTerminal(tag, pulse, neg) -> FlatTerminal(tag, pulse, neg)
        | FlatNary(op, terms) ->
            let terms = terms |> List.map clone
            FlatNary(op, terms)
        | FlatZero -> FlatZero
        
[<Extension>] // type FlatExpressionExt =
type FlatExpressionExt =
    /// 이항 연산자 기반의 expression 을 flat expression 으로 변경
    [<Extension>] static member Flatten(expr) = FlatExpressionM.flatten expr

    /// FlatExpression 을 이항 연산 Expression 으로 변환
    [<Extension>] static member Flat2binary(expr) = FlatExpressionM.flat2binary expr

    [<Extension>] static member ToNaryFlatExpression(op, expr) = FlatExpressionM.createNary op expr
    [<Extension>] static member TryGetOperatorAndTerms(expr)   = FlatExpressionM.tryGetOperatorAndTerms expr
    [<Extension>] static member TryGetOperator(expr)           = FlatExpressionM.tryGetOperatorAndTerms expr |> Option.map fst
    [<Extension>] static member TryGetTerms(expr)              = FlatExpressionM.tryGetOperatorAndTerms expr |> Option.map snd
    [<Extension>] static member TryGetTerminals(expr)          = FlatExpressionM.tryGetTerminals expr
    [<Extension>] static member AddTerms(expr, terms)          = FlatExpressionM.addTerms expr terms
    [<Extension>] static member Clone(expr)                    = FlatExpressionM.clone expr

