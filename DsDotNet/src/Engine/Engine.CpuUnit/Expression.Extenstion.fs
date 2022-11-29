namespace rec Engine.Cpu

open System
open System.Linq
open System.Runtime.CompilerServices
open System.Diagnostics
open System.Collections
open Engine.Cpu


[<AutoOpen>]
module FunctionModule =

    let add             (xs:Args) =  Function(_add           , xs)
    let addDouble       (xs:Args) =  Function(_addDouble     , xs)
    let addString       (xs:Args) =  Function(_addString     , xs)
    let sub             (xs:Args) =  Function(_sub           , xs)
    let subDouble       (xs:Args) =  Function(_subDouble     , xs)
    let mul             (xs:Args) =  Function(_mul           , xs)
    let mulDouble       (xs:Args) =  Function(_mulDouble     , xs)
    let div             (xs:Args) =  Function(_div           , xs)
    let divDouble       (xs:Args) =  Function(_divDouble     , xs)
    let modu            (xs:Args) =  Function(_modu          , xs)
    let moduDouble      (xs:Args) =  Function(_moduDouble    , xs)
    let equal           (xs:Args) =  Function(_equal         , xs)
    let equalString     (xs:Args) =  Function(_equalString   , xs)
    let notEqual        (xs:Args) =  Function(_notEqual      , xs)
    let notEqualString  (xs:Args) =  Function(_notEqualString, xs)
    let gt              (xs:Args) =  Function(_gt            , xs)
    let lt              (xs:Args) =  Function(_lt            , xs)
    let gte             (xs:Args) =  Function(_gte           , xs)
    let lte             (xs:Args) =  Function(_lte           , xs)
    let anD             (xs:Args) =  Function(_logicalAnd    , xs)
    let oR              (xs:Args) =  Function(_logicalOr     , xs)
    let noT             (xs:Args) =  Function(_logicalNot    , xs)
    let xorBit          (xs:Args) =  Function(_xorBit        , xs)
    let orBit           (xs:Args) =  Function(_orBit         , xs)
    let andBit          (xs:Args) =  Function(_andBit        , xs)
    let notBit          (xs:Args) =  Function(_notBit        , xs)
    let shiftLeft       (xs:Args) =  Function(_shiftLeft     , xs)
    let shiftRight      (xs:Args) =  Function(_shiftRight    , xs)
    let Bool            (xs:Args) =  Function(_convertBool   , xs)
    let Int             (xs:Args) =  Function(_convertInt    , xs)
    let String          (xs:Args) =  Function(_convertString , xs)
    let Single          (xs:Args) =  Function(_convertSingle , xs)
    let Double          (xs:Args) =  Function(_convertDouble , xs)
    let abs             (xs:Args) =  Function(_abs           , xs)
    let absDouble       (xs:Args) =  Function(_absDouble     , xs)
    let sin             (xs:Args) =  Function(_sin           , xs)
    let cos             (xs:Args) =  Function(_cos           , xs)
    let tan             (xs:Args) =  Function(_tan           , xs)

    [<AutoOpen>]
    [<Extension>]
    type FuncExt =

        [<Extension>] static member Evaluate (x:Expression<'T>) = evaluate x
        [<Extension>] static member CreateFunc (xs:IEnumerable, funcName:string) = Function(funcName, [xs])
        [<Extension>] static member ToTags (xs:DsStatusTag<'T> seq)  = xs.Cast<Tag<_>>()
        [<Extension>] static member ToTags (xs:DsDotBit<'T> seq)     = xs.Cast<Tag<_>>()
        [<Extension>] static member ToTags (xs:ActionTag<_> seq)    = xs.Cast<Tag<_>>()
        [<Extension>] static member DoAnd (xs:Tag<'T> seq)  = xs.Select(fun f-> (f|>box)) |> anD
        [<Extension>] static member DoOr  (xs:Tag<'T> seq)  = xs.Select(fun f-> (f|>box)) |> oR
        //[sets and]--|----- ! [rsts or] ----- (relay)
        //|relay|-----|
        [<Extension>] static member GetRelayExpr(sets:Tag<'T> seq, rsts:Tag<'T> seq, relay:Tag<'T>) =
                        let relaySet = anD[sets.DoAnd();relay]
                        let relayRst = noT[rsts.DoOr()]
                        anD[relaySet;relayRst]

        //[sets and]--|----- ! [rsts or] ----- (coil)
        [<Extension>] static member GetNoRelayExpr(sets:Tag<'T> seq, rsts:Tag<'T> seq) =
                        let relaySet = anD[sets.DoAnd()]
                        let relayRst = noT[rsts.DoOr()]
                        anD[relaySet;relayRst]

        //[sets and]--|-----  [rsts and] ----- (relay)
        //|relay|-----|
        [<Extension>] static member GetRelayExprReverseReset(sets:Tag<'T> seq, rsts:Tag<'T> seq, relay:Tag<'T>) =
                        let relaySet = anD[sets.DoAnd();relay]
                        let relayRst = rsts.DoAnd()
                        anD[relaySet;relayRst]


[<AutoOpen>]
module StatementModule =
    [<DebuggerDisplay("{ToText()}")>]
    type Statement<'T> =
        | Assign      of expression:Expression<'T>   * target:Tag<'T>
        member x.Do() =
            match x with
            | Assign     (expr, target) ->
                         ///  Target Y = Function (X)
                         target.Data <- evaluate(expr) |> ToData
                         ()
        member x.ToText() =
             match x with
             | Assign     (expr, target) -> $"assign({expr.ToText()}, {target.ToText()})"

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


