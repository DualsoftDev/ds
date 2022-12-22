namespace PLC.CodeGen.Common

open Engine.Core

[<AutoOpen>]
module FakeTypesModule =
    type IExpressionTerminal =
        inherit IText

    type Op =
        | And | Or | Neg | OpUnit
        with
                member x.ToText() = ""
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

type OpComp =
    | GT | GE | EQ | LE | LT | NE
    with
        member x.ToText =
            match x with
            | GT -> "GT"  // <
            | GE -> "GE"  // <=
            | EQ -> "EQ"  // ==
            | LE -> "LE"  // >=
            | LT -> "LT"  // >
            | NE -> "NE"  // !=