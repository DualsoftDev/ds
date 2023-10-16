namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open System
open Dual.Common.Core.FS

[<AutoOpen>]
module ExpressionExtension =

    let inline binaryOp f left right = f [left; right]
    let inline unaryOp  f exp = f [exp]
    
    let (<&&>) left right = binaryOp fLogicalAnd left right
    let (<||>) left right = binaryOp fLogicalOr left right
    let (!!) exp = unaryOp fLogicalNot exp
    let (<==) storage exp  = DuAssign(exp, storage)
    let (<=^) rising exp   = DuAssign(exp, rising)
    let (<=!^) falling exp = DuAssign(exp, falling)
  
    let inline coilOp op sets rsts (coil, comment) = 
        (op sets rsts coil) |> withExpressionComment comment
        
    let (--|) (sets_rsts, coil_comment) = coilOp (fun sets rsts coil -> coil <== (sets <&&> (!! rsts))) sets_rsts coil_comment
    let (==|) (sets_rsts, coil_comment) = coilOp (fun sets rsts coil -> coil <== ((sets <||> var2expr coil) <&&> (!! rsts))) sets_rsts coil_comment

    /// Create One Scan Relay Coils Statement
    let (--^) (sets: Expression<bool>, rsts: Expression<bool>) (rising: TypedValueStorage<bool>, risingRelay: TypedValueStorage<bool>, risingTemp : TypedValueStorage<bool>, comment:string) =
        [
            rising      <== (sets <&&> !!rsts <&&> !!(var2expr risingRelay)) |> withExpressionComment comment
            risingTemp  <== (sets) |> withExpressionComment comment
            risingRelay <== (var2expr rising <||> var2expr risingRelay <&&> var2expr risingTemp <&&> !!rsts) |> withExpressionComment comment
        ]

    /// Create Timer Coil Statement
    let (--@) (rungInCondition: IExpression<bool>) (timerCoil: TimerStruct, preset:CountUnitType, comment:string) =
        timerCoil.PRE.Value <- preset
        TimerStatement.CreateTONUsingStructure(timerCoil, Some rungInCondition, None)
        |> withExpressionComment comment
    /// Create Counter Coil Statement
    let (--%) (rungInCondition: IExpression<bool>) (counterCoil: CTRStruct, preset:CountUnitType, comment:string) =
        counterCoil.PRE.Value <- preset
        CounterStatement.CreateCTRUsingStructure(counterCoil, Some rungInCondition)
        |> withExpressionComment comment

    let private tryTags2LogicalAndOrExpr (fLogical: IExpression list -> Expression<bool>) (FList(ts:#TypedValueStorage<bool> list)) : Expression<bool> option =
        match ts with
        | [] -> None    //failwithlog "tags2AndExpr: Empty list"
        | t :: [] -> Some (var2expr t)
        | _ -> ts.Select(var2expr)
                |> List.cast<IExpression>
                |> fLogical
                |> Some


    /// Tag<'T> (들)로부터 AND Expression<'T> 생성
    let tryToAnd xs = tryTags2LogicalAndOrExpr fLogicalAnd xs
    /// Tag<'T> (들)로부터 OR  Expression<'T> 생성
    let tryToOr xs  = tryTags2LogicalAndOrExpr fLogicalOr xs

    /// Tag<'T> (들)로부터 AND Expression<'T> 생성
    let toAnd xs = tryToAnd xs |> Option.get
    /// Tag<'T> (들)로부터 OR  Expression<'T> 생성
    let toOr xs = tryToOr xs |> Option.get

    [<Extension>]
    type ExpressionExt =
        [<Extension>] static member ToAnd (xs:#TypedValueStorage<bool> seq)       = xs |> toAnd
        [<Extension>] static member ToAnd (xs:Expression<bool> seq) = xs.Reduce(<&&>)

        [<Extension>] static member ToOr  (xs:#TypedValueStorage<bool> seq)       = xs |> toOr
        [<Extension>] static member ToOr  (xs:Expression<bool> seq) = xs.Reduce(<||>)




