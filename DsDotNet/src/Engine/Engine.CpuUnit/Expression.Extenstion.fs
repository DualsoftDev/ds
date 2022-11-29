[<AutoOpen>]
module rec Engine.Cpu.FunctionModule

open System
open System.Linq
open System.Runtime.CompilerServices
open System.Diagnostics
open System.Collections
open Engine.Cpu


let verifyFunction (expr:Expression<'T>)  =
    match expr with
    | ConstValue v -> failwith "not function"
    | Variable   t -> failwith "not function"
    | Function (name, args) -> expr
                               //if args.Any() |>not 
                               //then failwith $"Empty Arguments of function : {name}"
                               //else expr
 
let add             (xs:Args) =  Function(_add           , xs) |> verifyFunction
let addDouble       (xs:Args) =  Function(_addDouble     , xs) |> verifyFunction
let addString       (xs:Args) =  Function(_addString     , xs) |> verifyFunction
let sub             (xs:Args) =  Function(_sub           , xs) |> verifyFunction
let subDouble       (xs:Args) =  Function(_subDouble     , xs) |> verifyFunction
let mul             (xs:Args) =  Function(_mul           , xs) |> verifyFunction
let mulDouble       (xs:Args) =  Function(_mulDouble     , xs) |> verifyFunction
let div             (xs:Args) =  Function(_div           , xs) |> verifyFunction
let divDouble       (xs:Args) =  Function(_divDouble     , xs) |> verifyFunction
let modu            (xs:Args) =  Function(_modu          , xs) |> verifyFunction
let moduDouble      (xs:Args) =  Function(_moduDouble    , xs) |> verifyFunction
let equal           (xs:Args) =  Function(_equal         , xs) |> verifyFunction
let equalString     (xs:Args) =  Function(_equalString   , xs) |> verifyFunction
let notEqual        (xs:Args) =  Function(_notEqual      , xs) |> verifyFunction
let notEqualString  (xs:Args) =  Function(_notEqualString, xs) |> verifyFunction
let gt              (xs:Args) =  Function(_gt            , xs) |> verifyFunction
let lt              (xs:Args) =  Function(_lt            , xs) |> verifyFunction
let gte             (xs:Args) =  Function(_gte           , xs) |> verifyFunction
let lte             (xs:Args) =  Function(_lte           , xs) |> verifyFunction
let anD             (xs:Args) =  Function(_logicalAnd    , xs) |> verifyFunction
let oR              (xs:Args) =  Function(_logicalOr     , xs) |> verifyFunction
let noT             (xs:Args) =  Function(_logicalNot    , xs) |> verifyFunction
let xorBit          (xs:Args) =  Function(_xorBit        , xs) |> verifyFunction
let orBit           (xs:Args) =  Function(_orBit         , xs) |> verifyFunction
let andBit          (xs:Args) =  Function(_andBit        , xs) |> verifyFunction
let notBit          (xs:Args) =  Function(_notBit        , xs) |> verifyFunction
let shiftLeft       (xs:Args) =  Function(_shiftLeft     , xs) |> verifyFunction
let shiftRight      (xs:Args) =  Function(_shiftRight    , xs) |> verifyFunction
let Bool            (xs:Args) =  Function(_convertBool   , xs) |> verifyFunction
let Int             (xs:Args) =  Function(_convertInt    , xs) |> verifyFunction
let String          (xs:Args) =  Function(_convertString , xs) |> verifyFunction
let Single          (xs:Args) =  Function(_convertSingle , xs) |> verifyFunction
let Double          (xs:Args) =  Function(_convertDouble , xs) |> verifyFunction
let abs             (xs:Args) =  Function(_abs           , xs) |> verifyFunction
let absDouble       (xs:Args) =  Function(_absDouble     , xs) |> verifyFunction
let sin             (xs:Args) =  Function(_sin           , xs) |> verifyFunction
let cos             (xs:Args) =  Function(_cos           , xs) |> verifyFunction
let tan             (xs:Args) =  Function(_tan           , xs) |> verifyFunction


[<AutoOpen>]
[<DebuggerDisplay("{ToText()}")>]
type Statement<'T> = 
    | Assign      of expression:Expression<'T>   * target:Tag<'T>

    //임시테스트
    member x.TestForce() =
        match x with
        | Assign     (expr, target) ->  target.SetValue(true)
                    
                
    member x.Do() =
        match x with
        | Assign     (expr, target) -> 
                     ///  Target Y = Function (X)
                     target.SetValue(evaluate(expr)) 
                
    member x.ToText() =
         match x with
         | Assign     (expr, target) -> $"assign({expr.ToText()}, {target.ToText()})"



[<AutoOpen>]
[<Extension>]
type FuncExt =


    
    [<Extension>] static member Evaluate (x:Expression<'T>) = evaluate x
    [<Extension>] static member CreateFunc (xs:IEnumerable, funcName:string) = Function(funcName, [xs])
    [<Extension>] static member ToTags (xs:DsBit<'T> seq)    = xs.Cast<Tag<_>>()
    [<Extension>] static member ToTags (xs:DsDotBit<'T> seq) = xs.Cast<Tag<_>>()
    [<Extension>] static member ToTags (xs:PlcTag<'T> seq)   = xs.Cast<Tag<_>>()
    [<Extension>] static member ToExpr (x:Tag<bool>)   = x |> createTagExpr
    [<Extension>] static member GetAnd (xs:Tag<'T> seq)  = xs.Select(fun f-> (f|>box)) |> anD
    [<Extension>] static member GetOr  (xs:Tag<'T> seq)  = xs.Select(fun f-> (f|>box)) |> oR
    //[sets and]--|----- ! [rsts or] ----- (relay)
    //|relay|-----|
    [<Extension>] static member GetRelayExpr(sets:Tag<'T> seq, rsts:Tag<'T> seq, relay:Tag<bool>) =
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

