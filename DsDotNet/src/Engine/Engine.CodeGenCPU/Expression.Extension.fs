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
    
    /// logical AND for expression 
    let (<&&>) left right = binaryOp fbLogicalAnd left right
    /// logical OR for expression 
    let (<||>) left right = binaryOp fbLogicalOr left right
    /// logical NOT for expression 
    let (!!) exp = unaryOp fbLogicalNot exp
    /// storage 에 expression assign 하는 statement 생성
    let (<==) storage exp  = DuAssign(None, exp, storage)
    /// storage 에 expression rising 값을 assign 하는 statement 생성
    let (<=^) rising exp   = DuAssign(None, exp, rising)
    /// storage 에 expression falling 값을 assign 하는 statement 생성
    let (<=!^) falling exp = DuAssign(None, exp, falling)
  
    /// set 조건, reset 조건을 op 에 의해서 coil 에 assign 하는 CommentedStatement 생성
    let inline coilOp op sets rsts (coil, comment) = 
        (op sets rsts coil) |> withExpressionComment comment

    let inline coilMove op sets  (coil, comment) = 
        (op sets  coil) |> withExpressionComment comment

    let inline coilAdd op sets  (coil, comment) = 
        (op sets  coil) |> withExpressionComment comment

    let inline coilSub op sets  (coil, comment) = 
        (op sets  coil) |> withExpressionComment comment


    /// set 조건, reset 조건을 받아서 --> 추가적으로 coil 과 comment 를 받아서 CommentedStatement 생성하는 함수를 반환하는 curried function
    let (--|) (sets, rsts) = coilOp (fun s r c -> c <== (s <&&> (!! r))) sets rsts
    /// set 조건, 조건을 받아서 --> 추가적으로 자기 유지되는 reset coil 과 comment 를 받아서 CommentedStatement 생성하는 함수를 반환하는 curried function
    let (==|) (sets, rsts) = coilOp (fun s r c -> c <== ((s <||> var2expr c) <&&> (!! r))) sets rsts

    /// Create Add Statement   //test ahn  Add 함수로 변경 필요
    let (--+) (sets) = coilAdd(fun s c -> c <== s) sets
    /// Create Add Statement   //test ahn  Add 함수로 변경 필요
    let (---) (sets) = coilSub (fun s c -> c <== s) sets
    /// Create Copy Statement 
    let (-->) (sets, copyExpr) (target, comment:string) = 
        DuAction(DuCopy(sets, copyExpr, target))  |> withExpressionComment comment
                
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
        | [t] -> Some (var2expr t)
        | _ -> ts.Select(var2expr)
                |> List.cast<IExpression>
                |> fLogical
                |> Some


    /// Tag<'T> (들)로부터 AND Expression<'T> 생성
    let tryToAnd xs = tryTags2LogicalAndOrExpr fbLogicalAnd xs
    /// Tag<'T> (들)로부터 OR  Expression<'T> 생성
    let tryToOr xs  = tryTags2LogicalAndOrExpr fbLogicalOr xs

    /// Tag<'T> (들)로부터 AND Expression<'T> 생성
    let toAnd xs = tryToAnd xs |> Option.get
    /// Tag<'T> (들)로부터 OR  Expression<'T> 생성
    let toOr xs = tryToOr xs |> Option.get

    let onExpr() = RuntimeDS.System.OnTag().Expr
    let offExpr() = RuntimeDS.System.OffTag().Expr

    [<Extension>]
    type ExpressionExt =
        [<Extension>] static member ToAnd        (xs:#TypedValueStorage<bool> seq) =  xs |> toAnd 
        [<Extension>] static member ToAndElseOn  (xs:#TypedValueStorage<bool> seq) = if xs.any() then xs |> toAnd else onExpr()
        [<Extension>] static member ToAndElseOff (xs:#TypedValueStorage<bool> seq) = if xs.any() then xs |> toAnd else offExpr()
        [<Extension>] static member ToAndElseOn  (xs:Expression<bool> seq) = if xs.any() then xs.Reduce(<&&>) else onExpr()
        [<Extension>] static member ToAndElseOff (xs:Expression<bool> seq) = if xs.any() then xs.Reduce(<&&>) else offExpr()

        [<Extension>] static member ToOr        (xs:#TypedValueStorage<bool> seq) =  xs |> toOr 
        [<Extension>] static member ToOrElseOn  (xs:#TypedValueStorage<bool> seq) = if xs.any() then xs |> toOr else onExpr()
        [<Extension>] static member ToOrElseOff (xs:#TypedValueStorage<bool> seq) = if xs.any() then xs |> toOr else offExpr()
        [<Extension>] static member ToOrElseOn  (xs:Expression<bool> seq) = if xs.any() then xs.Reduce(<||>) else onExpr()
        [<Extension>] static member ToOrElseOff (xs:Expression<bool> seq) = if xs.any() then xs.Reduce(<||>) else offExpr()
        
