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
        coil <== (sets <&&> (!! rsts)) |> withExpressionComment comment
    /// Create Relay Coil Statement                                                      
    let (==|) (sets: Expression<bool>, rsts: Expression<bool>) (coil: TagBase<bool> , comment:string) =
        coil <== (sets <||> tag2expr coil <&&> (!! rsts)) |> withExpressionComment comment
    /// Create None Relay rising Pulse Coil Statement
    let (--^) (sets: Expression<bool>, rsts: Expression<bool>) (coil: TagBase<bool>, comment:string) = 
        let rising:RisingCoil = {Storage = coil; HistoryFlag = HistoryFlag()}
        rising <=^ (sets <&&> (!! rsts)) |> withExpressionComment comment
    /// Create None Relay falling Pulse Coil Statement
    let (--!^) (sets: Expression<bool>, rsts: Expression<bool>) (coil: TagBase<bool>, comment:string) = 
        let falling:FallingCoil = {Storage = coil; HistoryFlag = HistoryFlag()}
        falling <=!^ (sets <&&> (!! rsts)) |> withExpressionComment comment
    /// Create Timer Coil Statement
    let (--@) (rungInCondition: IExpression<bool>) (timerCoil: DsTimer, preset:CountUnitType, comment:string) = 
        timerCoil.TimerStruct.PRE.Value <- preset
        timerCoil.TimerStruct <=@ (Some rungInCondition, None) |> withExpressionComment comment
    /// Create Counter Coil Statement
    let (--%) (rungInCondition: IExpression<bool>) (counterCoil: DsCounter, preset:CountUnitType, comment:string) = 
        counterCoil.CTRStruct.PRE.Value <- preset
        counterCoil.CTRStruct <=% (Some rungInCondition) |> withExpressionComment comment

    let private tags2LogicalAndOrExpr (fLogical: IExpression list -> Expression<bool>) (FList(ts:Tag<bool> list)) : Expression<bool> =
        match ts with
        | [] -> failwith "tags2AndExpr: Empty list"
        | t :: [] -> tag2expr t
        | _ -> ts.Select(tag2expr) 
                |> List.ofSeq 
                |> List.cast<IExpression>
                |> fLogical

    
    /// Tag<'T> (들)로부터 AND Expression<'T> 생성
    let toAnd = tags2LogicalAndOrExpr fLogicalAnd
    /// Tag<'T> (들)로부터 OR  Expression<'T> 생성
    let toOr  = tags2LogicalAndOrExpr fLogicalOr


    [<Extension>]
    type ExpressionExt =
        [<Extension>] static member ToAnd (xs:DsBit seq)             = xs.Cast<Tag<bool>>() |> toAnd
        [<Extension>] static member ToAnd (xs:PlcTag<bool> seq)      = xs.Cast<Tag<bool>>() |> toAnd
        [<Extension>] static member ToAnd (xs:Tag<bool> seq)         = xs |> toAnd
        [<Extension>] static member ToAnd (xs:Expression<bool> seq)  = xs.Reduce(<&&>)
        [<Extension>] static member ToOr  (xs:DsBit seq)             = xs.Cast<Tag<bool>>() |> toOr
        [<Extension>] static member ToOr  (xs:PlcTag<bool> seq)      = xs.Cast<Tag<bool>>() |> toOr
        [<Extension>] static member ToOr  (xs:Tag<bool> seq)         = xs |> toOr
        [<Extension>] static member ToOr  (xs:Expression<bool> seq)  = xs.Reduce(<||>)
                                            
                                               
       
    
