[<AutoOpen>]
module rec Engine.Core.FunctionModule

open System
open System.Linq
open System.Runtime.CompilerServices
open System.Diagnostics
open System.Collections

//let add             (xs:Args) =  Function(_add           , xs)
//let addDouble       (xs:Args) =  Function(_addDouble     , xs)
//let addString       (xs:Args) =  Function(_addString     , xs)
//let sub             (xs:Args) =  Function(_sub           , xs)
//let subDouble       (xs:Args) =  Function(_subDouble     , xs)
//let mul             (xs:Args) =  Function(_mul           , xs)
//let mulDouble       (xs:Args) =  Function(_mulDouble     , xs)
//let div             (xs:Args) =  Function(_div           , xs)
//let divDouble       (xs:Args) =  Function(_divDouble     , xs)
//let modu            (xs:Args) =  Function(_modu          , xs)
//let moduDouble      (xs:Args) =  Function(_moduDouble    , xs)
//let equal           (xs:Args) =  Function(_equal         , xs)
//let equalString     (xs:Args) =  Function(_equalString   , xs)
//let notEqual        (xs:Args) =  Function(_notEqual      , xs)
//let notEqualString  (xs:Args) =  Function(_notEqualString, xs)
//let gt              (xs:Args) =  Function(_gt            , xs)
//let lt              (xs:Args) =  Function(_lt            , xs)
//let gte             (xs:Args) =  Function(_gte           , xs)
//let lte             (xs:Args) =  Function(_lte           , xs)
//let anD             (xs:Args) =  Function(_logicalAnd    , xs)
//let oR              (xs:Args) =  Function(_logicalOr     , xs)
//let noT             (xs:Args) =  Function(_logicalNot    , xs)
//let xorBit          (xs:Args) =  Function(_xorBit        , xs)
//let orBit           (xs:Args) =  Function(_orBit         , xs)
//let andBit          (xs:Args) =  Function(_andBit        , xs)
//let notBit          (xs:Args) =  Function(_notBit        , xs)
//let shiftLeft       (xs:Args) =  Function(_shiftLeft     , xs)
//let shiftRight      (xs:Args) =  Function(_shiftRight    , xs)
//let Bool            (xs:Args) =  Function(_convertBool   , xs)
//let Int             (xs:Args) =  Function(_convertInt    , xs)
//let String          (xs:Args) =  Function(_convertString , xs)
//let Single          (xs:Args) =  Function(_convertSingle , xs)
//let Double          (xs:Args) =  Function(_convertDouble , xs)
//let abs             (xs:Args) =  Function(_abs           , xs)
//let absDouble       (xs:Args) =  Function(_absDouble     , xs)
//let sin             (xs:Args) =  Function(_sin           , xs)
//let cos             (xs:Args) =  Function(_cos           , xs)
//let tan             (xs:Args) =  Function(_tan           , xs)


[<AutoOpen>]
[<DebuggerDisplay("{ToText()}")>]
type Statement<'T> =
    | Assign      of expression:Expression<'T> * target:Tag<'T>

    ////임시테스트
    //member x.TestForce() =
    //    match x with
    //    | Assign     (expr, target) ->  target.SetValue(true)


    member x.Do() =
        match x with
        | Assign     (expr, target) ->
                     ///  Target Y = Function (X)
                     target.SetValue(expr.Evaluate())

    member x.ToText() =
         match x with
         | Assign     (expr, target) -> $"assign({expr.ToText()}, {target.ToText()})"


let private tagsToArguments (xs:Tag<'T> seq) = xs.Select (Tag >> box) |> List.ofSeq

[<AutoOpen>]
[<Extension>]
type FuncExt =



    [<Extension>] static member ToTags (xs:#Tag<'T> seq)    = xs.Cast<Tag<_>>()
    [<Extension>] static member ToExpr (x:Tag<bool>)   = Terminal (Tag x)
    [<Extension>] static member GetAnd (xs:Tag<'T> seq)  = xs |> tagsToArguments |> anD
    [<Extension>] static member GetOr  (xs:Tag<'T> seq)  = xs |> tagsToArguments |> oR
    //[sets and]--|----- ! [rsts or] ----- (relay)
    //|relay|-----|
    [<Extension>] static member GetRelayExpr(sets:Tag<bool> seq, rsts:Tag<bool> seq, relay:Tag<bool>) =
                    (sets.GetAnd() <||> relay.ToExpr()) <&&> (!! rsts.GetOr())

    //[sets and]--|----- ! [rsts or] ----- (coil)
    [<Extension>] static member GetNoRelayExpr(sets:Tag<'T> seq, rsts:Tag<'T> seq) =
                    sets.GetAnd() <&&> (!! rsts.GetOr())

    //[sets and]--|-----  [rsts and] ----- (relay)
    //|relay|-----|
    [<Extension>] static member GetRelayExprReverseReset(sets:Tag<'T> seq, rsts:Tag<'T> seq, relay:Tag<bool>) =
                    (sets.GetAnd() <||> relay.ToExpr()) <&&> (rsts.GetOr())


[<AutoOpen>]
module ExpressionOperatorModule =
    /// boolean AND operator
    let (<&&>) (left: Expression<bool>) (right: Expression<bool>) = anD [ left; right ]
    /// boolean OR operator
    let (<||>) (left: Expression<bool>) (right: Expression<bool>) = oR [ left; right ]
    /// boolean NOT operator
    let (!!)   (exp: Expression<bool>) = noT [exp]
    /// Assign statement
    let (<==)  (storage: Tag<'T>) (exp: Expression<'T>) = Assign(exp, storage)

