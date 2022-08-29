namespace Dual.Core.Types

open System.Diagnostics
open System.Runtime.CompilerServices
open Dual.Common
open System
open Dual.Core.Prelude

type identifier = string

type IExpressionTerminal =
    inherit IText
    abstract member Equals:IExpressionTerminal -> bool

/// Operators
type Op =
    | And | Or | Neg | OpUnit
    with 
        member x.ToText() =
            match x with
            | And -> "&"
            | Or -> "|"
            | Neg -> "!"
            | OpUnit -> ""
        [<Obsolete("Use ToText() instead")>]
        override x.ToString() =
            logWarn "Use Op.ToText() instead:\r\n%s" Environment.StackTrace
            Debugger.Break()
            x.ToText()
        member x.IsXNeg() = match x with | Neg -> true | _ ->false

module internal OpM =
    /// 주어진 operator 의 negation 을 반환
    let negateOp (op:Op) =
        match op with
        | And -> Op.Or
        | Or -> Op.And
        | Neg -> Op.OpUnit
        | OpUnit -> Op.Neg


[<Extension>] // type OperatorExt =
type OperatorExt =
    /// 주어진 operator 의 negation 을 반환
    [<Extension>] static member Negate(op) = OpM.negateOp op

/// 비교 관련 Operators
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

/// 비교 관련 Operators 타입
type OpCompType =
    | NoType | INT | DINT | REAL | STRING 
    with
        member x.ToText =
            match x with
            | NoType -> "NoType"
            | INT -> "INT"
            | DINT -> "DINT"
            | REAL -> "REAL"
            | STRING -> "STRING"
        static member getOp(op:string) =
            match op.ToUpper() with
                | "NoType"-> NoType
                | "INT" -> INT
                | "DINT"  -> DINT
                | "REAL"  -> REAL
                | "STRING"  -> STRING
                | _ -> failwithlog (sprintf "Invalid op [%s] (Op ={Load, And, Or})" op)


type TrueValue() =
    interface IExpressionTerminal with
        member x.ToText() = "TRUE"
        member x.Equals t = t :? TrueValue
type FalseValue() =
    interface IExpressionTerminal with
        member x.ToText() = "FALSE"
        member x.Equals t = t :? FalseValue

type PulseTerminal(tag:IExpressionTerminal) =
    member val Tag = tag
    interface IExpressionTerminal with
        member x.ToText() = tag.ToText()
        member x.Equals t = t :? PulseTerminal && tag.Equals (t :?> PulseTerminal).Tag


/// 조건의 수식
[<DebuggerDisplay("{ToText()}")>]
type Expression = 
    /// Primitives
    | Terminal  of IExpressionTerminal
    /// Binary Expressions
    | Binary    of Expression * Op * Expression
    /// Unary Expressions
    | Unary    of Op * Expression
    /// 항등원. Identity.
    /// 기존 Expression option 으로 정의되던 부분은 Expression.Zero 로 정의하도록 한다.
    | Zero

    with
        member x.ToText() =
            let rec tt exp (upOp:Op option) =
                match exp with
                | Terminal(t) ->
                    sprintf "%s" (t.ToText())
                | Binary(l, op, r) when upOp.IsNone || upOp = Some(op) ->
                    // 상위 레벨의 operator 와 현 레벨의 operator 가 같은 경우, 괄호 생략
                    sprintf "%s %s %s" (tt l (Some op)) (op.ToText()) (tt r (Some op))
                | Binary(l, op, r) ->
                    sprintf "(%s %s %s)" (tt l (Some op)) (op.ToText()) (tt r (Some op))
                | Unary(op, r) ->
                    sprintf "%s%s" (op.ToText()) (tt r (Some op))
                | Zero ->
                    ""

            tt x None

        member x.IsNeg = match x with | Unary(o, _) -> o.IsXNeg() | _ -> false
        member x.IsNonZero = not <| DU.isUnionCase<@ Zero @> x
        [<Obsolete("Use ToText() instead")>]
        override x.ToString() =
            logWarn "Use ToText() instead:\r\n%s" Environment.StackTrace
            Debugger.Break()
            x.ToText()


   
module Expression =
    /// 주어진 expression 의 negation 을 반환
    let rec negate expr =
        match expr with
        | Unary(Neg, exp) ->  // 두번 negation = 원본
            exp
        | Terminal(id) -> Unary(Op.Neg, Terminal(id))
        | Binary(left, op, right) ->
            let l = negate left
            let r = negate right
            Binary(l, OpM.negateOp op, r)
        | Zero -> Zero
        | _ ->
            failwithlogf "negate : Unknown expression %A" expr
    /// expression 의 negation 부분을 최대한 선 적용한 새로운 expression 반환 : 진리값은 동일해야 한다.
    let rec flattenNegate (expr:Expression) =
        match expr with
        | Terminal(_)
        | Unary(Neg, Terminal(_)) ->
            expr
        | Unary(Neg, Unary(Neg, exp)) ->
            exp
        | Unary(Neg, exp) ->
            negate exp
        | Binary(l, op, r) ->
            let left = flattenNegate l
            let right = flattenNegate r
            Binary(left, op, right)
        | Zero -> Zero
        | _ as exp -> exp


    /// 주어진 expression 의 terminal 만 변환 함수를 받아서 변환한 새로운 expression 을 반환
    let rec transformTerminal (transformer:IExpressionTerminal->IExpressionTerminal) (expr:Expression) =
        let f = transformTerminal transformer
        match expr with
        | Terminal(t)      -> t |> transformer |> Terminal
        | Unary(op, u)     -> Unary(op, u |> f)
        | Binary(l, op, r) -> Binary(l |> f, op, r |> f)
        | Zero             -> Zero



    /// 주어진 expression 의 값을 평가
    let rec evaluate (terminalEvaluator:IExpressionTerminal->bool) (expr:Expression) =
        let eval expr = evaluate terminalEvaluator expr
        let failed expr = failwithlogf "evaluate : Failed on expression %A" expr

        match expr with
        | Terminal(t)      -> t |> terminalEvaluator
        | Unary(o, u) -> 
            match o, u with
            | Neg, Terminal(t) -> not <| terminalEvaluator t
            | Neg, _           -> not <| eval u
            | _                -> failed expr
        | Binary(l, o ,r) -> 
            match o with
            | And -> eval l && eval r
            | Or  -> eval l || eval r
            | _ -> failed expr
        | Zero             -> false

    /// 주어진 expression 의 값을 평가
    [<Obsolete>]
    let rec evaluateObsolete expr (terminalEvaluator:Expression -> bool) =
        let eval expr = evaluateObsolete expr terminalEvaluator
        let failed expr = failwithlogf "evaluate : Failed on expression %A" expr

        match expr with
        | Binary(l, o ,r) -> 
            match o with
            | And -> eval l && eval r
            | Or  -> eval l || eval r
            | _   -> failed expr
        | Unary(o, u) -> 
            match o, u with
            | Neg, Terminal(_) -> not <| terminalEvaluator expr
            | Neg, _           -> not <| eval u
            | _                -> failed expr
        | Terminal(_) ->
            terminalEvaluator expr
        | _ ->
            failed expr

[<AutoOpen>]
module PulseTerminal =
    let toPulse tag = Terminal(PulseTerminal(tag))

