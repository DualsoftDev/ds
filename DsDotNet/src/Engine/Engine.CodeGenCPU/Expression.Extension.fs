namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open System
open Engine.Common.FS

[<AutoOpen>]
module ExpressionExtension =

    //  Statement
    /// boolean AND operator
    let (<&&>) (left: Expression<bool>) (right: Expression<bool>) = fLogicalAnd [ left; right ]

    /// boolean OR operator
    let (<||>) (left: Expression<bool>) (right: Expression<bool>) = fLogicalOr [ left; right ]

    /// boolean NOT operator
    let (!!)   (exp: Expression<bool>) = fLogicalNot [exp]

    /// Assign statement
    let (<==)  (storage: IStorage) (exp: IExpression) = DuAssign(exp, storage)

    /// Assign rising statement
    let (<=^)  (rising: RisingCoil)   (exp: IExpression) = DuAssign(exp, rising)

    /// Assign falling statement
    let (<=!^) (falling: FallingCoil) (exp: IExpression) = DuAssign(exp, falling)

    /// Create Timer Coil Statement
    let (<=@)  (ts: TimerStruct) (sets: IExpression<bool> option, rsts:IExpression<bool> option) =
        TimerStatement.CreateTONUsingTag(ts, sets, rsts)

    /// Create Counter Coil Statement
    let (<=%)  (cs: CTRStruct) (sets: IExpression<bool> option) =
        CounterStatement.CreateCTRUsingTag(cs, sets)

    // Extenstion Comment Statement
    /// Create None Relay Coil Statement
    let (--|) (sets: Expression<bool>, rsts: Expression<bool>) (coil: TagBase<bool>, comment:string) =
        coil <== (sets <&&> (!! rsts))
        |> withExpressionComment comment

    /// Create Relay Coil Statement
    let (==|) (sets: Expression<bool>, rsts: Expression<bool>) (coil: TagBase<bool> , comment:string) =
        coil <== ((sets <||> var2expr coil) <&&> (!! rsts))
        |> withExpressionComment comment

    /// Create None Relay rising Pulse Coil Statement
    let (--^) (sets: Expression<bool>, rsts: Expression<bool>) (coil: TagBase<bool>, comment:string) =
        let rising:RisingCoil = {Storage = coil; HistoryFlag = HistoryFlag()}
        rising <=^ (sets <&&> (!! rsts))
        |> withExpressionComment comment

    /// Create None Relay falling Pulse Coil Statement
    let (--!^) (sets: Expression<bool>, rsts: Expression<bool>) (coil: TagBase<bool>, comment:string) =
        let falling:FallingCoil = {Storage = coil; HistoryFlag = HistoryFlag()}
        falling <=!^ (sets <&&> (!! rsts))
        |> withExpressionComment comment

    /// Create Timer Coil Statement
    let (--@) (rungInCondition: IExpression<bool>) (timerCoil: TimerStruct, preset:CountUnitType, comment:string) =
        timerCoil.PRE.Value <- preset
        timerCoil <=@ (Some rungInCondition, None)
        |> withExpressionComment comment

    /// Create Counter Coil Statement
    let (--%) (rungInCondition: IExpression<bool>) (counterCoil: CTRStruct, preset:CountUnitType, comment:string) =
        counterCoil.PRE.Value <- preset
        counterCoil <=% (Some rungInCondition)
        |> withExpressionComment comment

    let private tryTags2LogicalAndOrExpr (fLogical: IExpression list -> Expression<bool>) (FList(ts:#Tag<bool> list)) : Expression<bool> option =
        match ts with
        | [] -> None    //failwithlog "tags2AndExpr: Empty list"
        | t :: [] -> Some (var2expr t)
        | _ -> ts.Select(var2expr)
                |> List.ofSeq
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
        [<Extension>] static member ToAnd (xs:#Tag<bool> seq)       = xs |> toAnd
        [<Extension>] static member ToAnd (xs:Expression<bool> seq) = xs.Reduce(<&&>)

        [<Extension>] static member ToOr  (xs:#Tag<bool> seq)       = xs |> toOr
        [<Extension>] static member ToOr  (xs:Expression<bool> seq) = xs.Reduce(<||>)




