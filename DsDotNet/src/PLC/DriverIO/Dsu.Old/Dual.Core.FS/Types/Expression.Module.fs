namespace Old.Dual.Core.Types

open Old.Dual.Core
open Old.Dual.Common
open System.Runtime.CompilerServices
open System
open System.Diagnostics

/// 엔진의 정상적인 부분에서는 사용되어서는 안된다.
module internal PseudoTerminalM =
    /// 문자열로 임시 Expression 의 terminal 을 생성하고자 하는 경우에 사용.
    /// 엔진의 정상적인 부분에서는 사용되어서는 안된다.
    [<DebuggerDisplay("{ToText()}")>]
    type PseudoTerminal(str) =
        member val TagName = str with get, set
        member x.ToText() = x.TagName

        interface IExpressionTerminal with
            member x.ToText() = x.ToText()
            member x.Equals t = t :? PseudoTerminal && x.ToText() = t.ToText()
/// Expression 관련 함수
/// see ParserAlgo also
[<AutoOpen>]
module ExpressionM =

    /// AND expression 생성. <&&>
    let mkAnd x y =
        match x, y with
        | Zero, v -> v
        | v, Zero -> v
        | Terminal(t), v when (t :? TrueValue) -> v
        | v, Terminal(t) when (t :? TrueValue) -> v
        | Terminal(t), _ when (t :? FalseValue) -> Terminal(FalseValue())
        | _, Terminal(t) when (t :? FalseValue) -> Terminal(FalseValue())
        | x, y when (x = y) || x.ToText() = y.ToText() -> x
        | _ -> Binary(x, And, y)

    /// OR expression 생성. <||>
    let mkOr x y =
        match x, y with
        | Zero, v -> v
        | v, Zero -> v
        | Terminal(t), _ when (t :? TrueValue) -> x
        | _, Terminal(t) when (t :? TrueValue) -> y
        | Terminal(t), v when (t :? FalseValue) -> v
        | v, Terminal(t) when (t :? FalseValue) -> v
        | x, y when (x = y) || x.ToText() = y.ToText() -> x
        | _ -> Binary(x, Or, y)

    /// Negation expressin 생성. <!>
    let mkNeg x =
        match x with
        | Unary(Neg, exp) -> exp
        | Terminal(t) when (t :? TrueValue) -> Terminal(FalseValue())
        | Terminal(t) when (t :? FalseValue) -> Terminal(TrueValue())
        | Expression.Zero -> Expression.Zero
        | _ -> Unary(Neg, x)

    let (<&&>) x y = mkAnd x y
    let (<||>) x y = mkOr x y
    let ( ~~ ) x = mkNeg x

    let mkNegOpt x =
        match x with
        | Some(exp) -> Some(mkNeg exp)
        | None -> None

    /// Terminal expression 생성
    let mkTerminal id = Terminal(id)
    /// Option type x y 로부터 AND expression 생성
    let mkAndOpt x y =
        match x, y with
        | Some(x), Some(y) -> Some(mkAnd x y)
        | Some(x), None -> Some x
        | None, Some(y) -> Some y
        | None, None -> None
    /// Option type x y 로부터 OR expression 생성
    let mkOrOpt x y =
        match x, y with
        | Some(x), Some(y) -> Some(mkOr x y)
        | Some(x), None -> Some x
        | None, Some(y) -> Some y
        | None, None -> None

    /// 주어진 operands 들을 operator 를 적용시켜셔 binary expression 으로 만들어 반환
    let mkBinary operator operands =
        operands |> Seq.reduce (fun x y -> Binary(x, operator, y))

    /// 주어진 expression 에  포함된 모든 tag 수집
    let rec collectTerminals cond =
        seq {
            match cond with
            | Terminal(tag) ->
                yield tag
            | Binary(l, _op, r) ->
                yield! collectTerminals l
                yield! collectTerminals r
            | Unary(op ,r) ->
                yield! collectTerminals r
            | Zero -> ()
        }

    /// 주어진 expression 에 사용된 모든 tag 를 골라냄
    let getConditionTags (cond:Expression) = collectTerminals cond

    /// 주어진 expression 에 사용된 모든 tag 를 골라냄
    let getConditionTagsStrings (cond:Expression) =
        getConditionTags cond |> Seq.map(fun t -> t.ToText())


    /// 주어진 expression 에서 terminal (tag identifier) 를 반환
    let getTerminal x =
        match x with
        | Terminal(tag) -> Some tag
        | _ -> None

    let getTerminals x =
        let rec loop x =
            [
                match x with
                | Terminal(tag) -> yield Some tag
                | Binary(l, _op, r) ->
                    yield! loop l
                    yield! loop r
                | Unary(_op, xx) ->
                    yield! loop xx
                | _ ->
                    ()
            ]
        loop x |> List.choose id

    /// 주어진 expression 에서 terminal (tag identifier) 를 반환
    let getTerminalString x = getTerminal x |> Option.map(fun t -> t.ToText())

    /// 주어진 expression 에서 binary expression 반환: (left, operator, right) 의 tuple
    let getBinary x =
        match x with
        | Binary(l, op, r) -> Some(l, op, r)
        | _ -> None

    /// 주어진 expression 에서 unary expression 반환: (operator, expression) 의 tuple
    let getUnary x =
        match x with
        | Unary(op, exp) -> Some(op, exp)
        | _ -> None

    let rec collectTerminalsAndUnarys x =
        seq {
            match x with
            | Terminal(tag) ->
                yield Terminal(tag)
            | Binary(l, _op, r) ->
                yield! collectTerminalsAndUnarys l
                yield! collectTerminalsAndUnarys r
            | Unary(op ,r) ->
                yield Unary(op ,r)
            | Zero -> ()
        }

    /// Expression 복사
    let clone x =
        let rec cloneHelper x =
            match x with
            | Terminal(ident) -> Terminal(ident)
            | Binary(l, op, r) -> Binary(cloneHelper(l), op, cloneHelper(r))
            | Unary(op, expr) -> Unary(op, cloneHelper(expr))
            | Zero -> Zero
        cloneHelper x


    /// Expression minimize (by heuristic) // TODO
    let minimzie x =
        ()

    let remove (x:Expression) expr =
        let rec removeHelper (x:Expression) expr =
            match expr with
            | Terminal(t) ->
                match expr.ToText() = x.ToText() with
                | true -> Zero
                | false -> expr
            | Binary(l, op, r) ->
                match removeHelper x l, removeHelper x r with
                | Zero, Zero -> Zero
                | Zero, _ -> r
                | _, Zero -> l
                | _ as l, r -> Binary(l, op, r)
            | Unary(op, r) ->
                match expr.ToText() = x.ToText() with
                | true -> Zero
                | false ->
                    match removeHelper x r with
                    | Zero -> Zero
                    | _ -> expr
            | Zero -> Zero
        removeHelper x expr

    let replace oldValue newValue expr =
        let rec replaceHelper (oldValue:IExpressionTerminal) (newValue:IExpressionTerminal) expr =
            match expr with
            | Terminal(t) ->
                match t.Equals oldValue with
                | true -> newValue |> mkTerminal
                | false -> expr
            | Binary(l, op, r) ->
                let rl = replaceHelper oldValue newValue l
                let rr = replaceHelper oldValue newValue r
                Binary(rl, op, rr)
            | Unary(op, r) ->
                Unary(op, replaceHelper oldValue newValue r)
            | Zero -> Zero

        replaceHelper oldValue newValue expr




