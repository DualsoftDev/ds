namespace PLC.CodeGen.Common

open System.Diagnostics
open System.Runtime.CompilerServices
open Engine.Common.FS
open System
open Engine.Core

[<AutoOpen>]
module FlatExpressionModule =
    type Op =
        | And | Or | Neg | OpUnit
    with
        member x.ToText() = ""

    type TrueValue() =
        interface IExpressionTerminal with
            member x.ToText() = "TRUE"
    type FalseValue() =
        interface IExpressionTerminal with
            member x.ToText() = "FALSE"

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
            | FlatTerminal(value, pulse, neg) -> sprintf "%s%s" (if neg then "!" else "") (value.ToText())
            | FlatNary(op, terms) ->
                let termsStr =
                    terms
                    |> Seq.map (fun t -> t.ToText())
                    |> String.concat ", "
                sprintf "%s(%s)" (op.ToText()) termsStr
            | FlatZero -> ""

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

    //type IExpression with
    //    member x.Flatten() =
    //        match x with
    type Statement with
        member x.Flatten() =
            match x with
            | DuAssign (expr, target) -> ()
            | DuVarDecl (expr, target) -> ()
            | DuTimer timerStatement -> ()
            | DuCounter counterStatement -> ()
            | DuCopy (condition, source, target) -> ()

