namespace PLC.CodeGen.LS
open System.Linq

open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.Common

[<AutoOpen>]
module StatementExtensionModule =

    type ExpressionVisitor = DynamicDictionary -> IExpression list -> IExpression -> IExpression

    type Statement with
        /// Statement to XGx Statements. XGK/XGI 공용 Statement 확장
        member internal x.ToStatements (pack:DynamicDictionary) : unit =
            let statement = x
            let _prjParam, augs = pack.Unpack()

            match statement with
            | DuVarDecl _ -> failwith "ERROR: DuVarDecl in statement"
            | DuAssign(condition, exp, target) ->
                // todo : "sum = tag1 + tag2" 의 처리 : DuPLCFunction 하나로 만들고, 'OUT' output 에 sum 을 할당하여야 한다.
                match exp.FunctionName with
                | Some(IsArithmeticOrComparisionOperator op) ->
                    let exp = exp.FlattenArithmeticOperator(pack, Some target)
                    if exp.FunctionArguments.Any() then
                        let augFunc =
                            DuPLCFunction {
                                Condition = condition
                                FunctionName = op
                                Arguments = exp.FunctionArguments
                                OriginalExpression = exp
                                Output = target }
                        augs.Statements.Add augFunc
                | _ ->
                    let newExp = exp.CollectExpandedExpression(pack)
                    DuAssign(condition, newExp, target) |> augs.Statements.Add 

            | (DuTimer _ | DuCounter _ | DuPLCFunction _) ->
                augs.Statements.Add statement 

            | DuAction(DuCopy(condition, source, target)) ->
                let funcName = XgiConstants.FunctionNameMove
                DuPLCFunction {
                    Condition = Some condition
                    FunctionName = funcName
                    Arguments = [ condition; source ]
                    OriginalExpression = condition
                    Output = target
                } |> augs.Statements.Add


        /// statement 내부에 존재하는 모든 expression 을 visit 함수를 이용해서 변환한다.   visit 의 예: exp.MakeFlatten()
        /// visit: [상위로부터 부모까지의 expression 경로] -> 자신 expression -> 반환 expression : 아래의 FunctionToAssignStatement 샘플 참고
        member x.VisitExpression (pack:DynamicDictionary, visit:ExpressionVisitor) : Statement =
            let statement = x
            /// IExpression option 인 경우의 visitor
            let tryVisit (expPath:IExpression list) (exp:IExpression<bool> option) : IExpression<bool> option =
                exp |> map (fun exp -> visit pack expPath exp :?> IExpression<bool> ) 

            let visitTop exp = visit pack [] exp
            let tryVisitTop exp = tryVisit [] exp

            match statement with
            | DuAssign(condition, exp, tgt) -> DuAssign(tryVisitTop condition, visitTop exp, tgt)                

            | DuTimer ({ RungInCondition = rungIn; ResetCondition = reset } as tmr) ->
                DuTimer { tmr with
                            RungInCondition = tryVisitTop rungIn
                            ResetCondition  = tryVisitTop reset }
            | DuCounter ({UpCondition = up; DownCondition = down; ResetCondition = reset; LoadCondition = load} as ctr) ->
                DuCounter {ctr with
                            UpCondition    = tryVisitTop up 
                            DownCondition  = tryVisitTop down
                            ResetCondition = tryVisitTop reset
                            LoadCondition  = tryVisitTop load }
            | DuAction(DuCopy(condition, source, target)) ->
                let cond = (visitTop condition) :?> IExpression<bool>
                DuAction(DuCopy(cond, visitTop source, target))

            | DuPLCFunction ({Arguments = args} as functionParameters) ->
                let newArgs = args |> map (fun arg -> visitTop arg)
                DuPLCFunction { functionParameters with Arguments = newArgs }

            | DuVarDecl _ -> failwith "ERROR: DuVarDecl in statement"

        /// expression 의 parent 정보 없이 visit 함수를 이용해서 모든 expression 을 변환한다.
        member x.VisitExpression (pack:DynamicDictionary, visit:IExpression -> IExpression) : Statement =
            let statement = x
            let visit2 _pack _ (exp:IExpression) = visit exp
            statement.VisitExpression (pack, visit2)

        /// Expression 을 flattern 할 수 있는 형태로 변환 : e.g !(a>b) => (a<=b)
        member x.DistributeNegate(pack:DynamicDictionary) =
            let statement = x
            let visitor (exp:IExpression) : IExpression = exp.ApplyNegate()
            statement.VisitExpression(pack, visitor)


        /// XGI Timer/Counter 의 RungInCondition, ResetCondition 이 Non-terminal 인 경우, assign statement 로 변환한다.
        ///
        /// - 현재, 구현 편의상 XGI Timer/Counter 의 다릿발에는 boolean expression 만 수용하므로 사칙/비교 연산을 assign statement 로 변환한다.
        member x.AugmentXgiFunctionParameters (pack:DynamicDictionary) : Statement =
            let prjParam, _augs = pack.Unpack()
            let toAssignOndemand (exp:IExpression<bool> option) : IExpression<bool> option =
                exp |> map (fun exp -> exp.ToAssignStatement(pack, K.arithmaticOrComparisionOperators) :?> IExpression<bool>)

            match prjParam.TargetType, x with
            | XGK, _ -> x
            | XGI, DuTimer ({ RungInCondition = rungIn; ResetCondition = reset } as tmr) ->
                DuTimer { tmr with
                            RungInCondition = toAssignOndemand rungIn
                            ResetCondition  = toAssignOndemand reset }
            | XGI, DuCounter ({UpCondition = up; DownCondition = down; ResetCondition = reset; LoadCondition = load} as ctr) ->
                DuCounter {ctr with
                            UpCondition    = toAssignOndemand up 
                            DownCondition  = toAssignOndemand down
                            ResetCondition = toAssignOndemand reset
                            LoadCondition  = toAssignOndemand load }
            | _ -> x


        /// x 로 주어진 XGK statement 내의 expression 들을 모두 검사해서 사칙/비교연산을 assign statement 로 변환한다.
        member x.FunctionToAssignStatement (pack:DynamicDictionary) : Statement =
            let prjParam, _augs = pack.Unpack()
            let rec visitor (pack:DynamicDictionary) (expPath:IExpression list) (exp:IExpression): IExpression =
                if exp.Terminal.IsSome then
                    exp
                else
                    tracefn $"exp: {exp.ToText()}"
                    let newExp =
                        let args = exp.FunctionArguments |> map (fun ex -> visitor pack (exp::expPath) ex)
                        exp.WithNewFunctionArguments args
                    match newExp.FunctionName with
                    | Some (IsArithmeticOrComparisionOperator fn) when expPath.Any() ->
                        let augment =
                            match prjParam.TargetType, expPath with
                            | XGK, _head::_ -> true
                            | XGI, _head::_ when _head.FunctionName <> Some fn -> true
                            | _ ->
                                false

                        if augment then
                            newExp.ToAssignStatement(pack, K.arithmaticOrComparisionOperators)
                        else
                            newExp
                    | _ ->
                        newExp

            // visitor 를 이용해서 statement 내의 모든 expression 을 변환한다.
            x.VisitExpression(pack, visitor)
