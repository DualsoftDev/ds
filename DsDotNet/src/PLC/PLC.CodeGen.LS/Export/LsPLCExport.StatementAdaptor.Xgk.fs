namespace PLC.CodeGen.LS


open Engine.Core
open Dual.Common.Core.FS
open System
open PLC.CodeGen.Common

[<AutoOpen>]
module XgkTypeConvertorModule =
    type XgkTimerCounterStructResetCoil(tc:TimerCounterBaseStruct) =
        inherit TimerCounterBaseStruct(None, tc.Name, tc.DN, tc.PRE, tc.ACC, tc.RES, (tc :> IStorage).DsSystem)
        interface INamedExpressionizableTerminal with
            member x.StorageName = tc.Name
        interface ITerminal with
            member x.Variable = Some tc.RES
            member x.Literal = None


    type ExpressionConversionResult = IExpression * IStorage list * Statement list

    type IExpression with
        /// exp 내에 포함된, {문장(statement)으로 추출 해야만 할 요소}를 newStatements 에 추가한다.
        /// 이 과정에서 추가할 필요가 있는 storate 는 newLocalStorages 에 추가한다.
        /// 반환 : exp, 추가된 storage, 추가된 statement
        ///
        /// e.g: XGK 의 경우, 함수를 지원하지 않으므로,
        ///     입력 exp: "2 + 3 > 4"
        ///     추가 statement : "tmp1 = 2 + 3"
        ///     추가 storage : tmp2
        ///     최종 exp: "tmp1 > 4"
        ///     반환 : exp, [tmp2], [tmp1 = 2 + 3]
        member x.AugmentXgk (prjParam: XgxProjectParams, expStore:IStorage option, augs:Augments) : IExpression =
            let xexp = x
            let isXgk = prjParam.TargetType = XGK
            let rec helper (nestLevel:int) (exp: IExpression, expStore:IStorage option) : ExpressionConversionResult =
                match exp.FunctionName, exp.FunctionArguments with
                | Some fn, l::r::[] ->
                    let lexpr, lstgs, lstmts = helper (nestLevel + 1) (l, None)
                    let rexpr, rstgs, rstmts = helper (nestLevel + 1) (r, None)

                    if (*isXgk &&*) lexpr.DataType = typeof<bool> && fn.IsOneOf("!=", "==", "<>") then
                        // XGK 에는 bit 의 비교 연산이 없다.  따라서, bool 타입의 비교 연산을 수행할 경우, 이를 OR, AND 로 변환한다.
                        let l, r, nl, nr = lexpr, rexpr, fbLogicalNot [lexpr], fbLogicalNot [rexpr]
                        let newExp =
                            match fn with
                            | ("!=" | "<>") -> fbLogicalOr([fbLogicalAnd [l; nr]; fbLogicalAnd [nl; r]])
                            | "==" -> fbLogicalOr([fbLogicalAnd [l; r]; fbLogicalAnd [nl; nr]])
                            | _ -> failwithlog "ERROR"
                        newExp, (lstgs @ rstgs), (lstmts @ rstmts)
                    else
                        // XGK 에는 IEC Function 을 이용할 수 없으므로, 
                        // XGI 에는 사칙 연산을 중간 expression 으로 이용은 가능하나, ladder 그리는 로직이 너무 복잡해 지므로, 
                        // 수식 내에 포함된 사칙 연산이나 비교 연산을 따로 빼내어서 임시 변수에 대입하는 assign 문장으로 으로 변환한다.
                        let newExp = exp.WithNewFunctionArguments [lexpr; rexpr]
                        let createTmpStorage =
                            fun () -> 
                                match expStore with
                                | Some stg -> stg
                                | None ->
                                    let tmpNameHint = operatorToMnemonic fn
                                    let tmpVar = prjParam.CreateAutoVariable(tmpNameHint, exp.BoxedEvaluatedValue, $"{exp.ToText()}")
                                    tmpVar :> IStorage

                        match fn with
                        | IsArithmaticOrComparisionOperator _ ->
                            let stg = createTmpStorage()
                            let stmt = DuAssign(None, newExp, stg)
                            let varExp = stg.ToExpression()
                            varExp, (lstgs @ rstgs @ [ stg ]), (lstmts @ rstmts @ [ stmt ])
                        | _ ->
                            if lstgs.any() || rstgs.any() then
                                newExp, (lstgs @ rstgs), (lstmts @ rstmts)
                            else
                                exp, [], []
                | _ ->
                    exp, [], []

            if x.Terminal.IsSome then
                x
            else
                let exp, stgs, stmts = helper 0 (x, expStore)
                augs.Storages.AddRange(stgs)
                augs.Statements.AddRange(stmts)
                exp

    type Statement with
        /// XGK 전용 Statement 확장
        member internal x.ToStatementsXgk (prjParam: XgxProjectParams, augs:Augments) : unit =
            match x with
            | DuAssign(condition, exp, target) ->
                let numStatementsBefore = augs.Statements.Count
                let exp2 = exp.AugmentXgk(prjParam, Some target, augs)
                let duplicated =
                    option {
                        // a := a 등의 형태 체크
                        let! terminal = exp2.Terminal
                        let! variable = terminal.Variable
                        return variable = target
                    } |> Option.defaultValue false

                if augs.Statements.Count = numStatementsBefore || (exp <> exp2 && not duplicated) then
                    let assignStatement = DuAssign(condition, exp2, target)
                    assignStatement.ToStatements(prjParam, augs)


            // e.g: XGK 에서 bool b3 = $nn1 > $nn2; 와 같은 선언의 처리.
            // XGK 에서 다음과 같이 2개의 문장으로 분리한다.
            // bool b3;
            // b3 = $nn1 > $nn2;
            | DuVarDecl(exp, decl) when exp.Terminal.IsNone ->
                augs.Storages.Add decl
                let stmt = DuAssign(Some systemOnRising, exp, decl)
                stmt.ToStatementsXgk(prjParam, augs)

            | DuTimer tmr ->
                match tmr.ResetCondition with
                | Some rst ->
                    // XGI timer 의 RST 조건을 XGK 에서는 Reset rung 으로 분리한다.
                    augs.Statements.Add <| DuAssign(None, rst, new XgkTimerCounterStructResetCoil(tmr.Timer.TimerStruct))
                | _ -> ()

                augs.Statements.Add (DuTimer tmr)

            | DuCounter ctr ->
                let statements = StatementContainer([x])
                // XGI counter 의 LD(Load) 조건을 XGK 에서는 Reset rung 으로 분리한다.
                let resetCoil = new XgkTimerCounterStructResetCoil(ctr.Counter.CounterStruct)
                let typ = ctr.Counter.Type
                let assingExp =
                    match typ with
                    | CTD -> ctr.LoadCondition.Value
                    | (CTR|CTU|CTUD) -> ctr.ResetCondition.Value
                DuAssign(None, assingExp, resetCoil) |> statements.Add

                if typ = CTUD then
                    let mutable newCtr = ctr

                    /// newStatementGenerator : fun () -> DuCounter({ ctr with UpCondition = Some ldVarExp })
                    let replaceComplexCondition (_ctr: CounterStatement) (cond:IExpression<bool>) (newStatementGenerator:IExpression<bool> -> Statement) =
                        let ldVarExp =
                            let operators = [|"&&"; "||"; "!"|] @ K.arithmaticOrComparisionOperators
                            cond.ToAssignStatement prjParam augs operators :?> IExpression<bool>
                        statements[0] <- newStatementGenerator(ldVarExp)
                        match statements[0] with
                        | DuCounter ctr -> newCtr <- ctr
                        | _ -> failwithlog "ERROR"


                    match newCtr.UpCondition with
                    | Some cond when cond.Terminal.IsNone ->
                        replaceComplexCondition newCtr cond (fun ldVarExp -> DuCounter({ newCtr with UpCondition = Some ldVarExp }))
                    | _ -> ()

                    match newCtr.DownCondition with
                    | Some cond when cond.Terminal.IsNone ->
                        replaceComplexCondition newCtr cond (fun ldVarExp -> DuCounter({ newCtr with DownCondition = Some ldVarExp }))
                    | _ -> ()

                    (* XGK CTUD 에서 load : 별도의 statement 롭 분리: ldcondition --- MOV PV C0001  *)
                    match newCtr.LoadCondition with
                    | Some cond ->
                        DuAction(DuCopy(cond, literal2expr(ctr.Counter.PRE.Value), ctr.Counter.CounterStruct)) |> statements.Add
                    | _ -> ()

                augs.Statements.AddRange(statements)

            | _ ->
                // 공용 처리
                x.ToStatements(prjParam, augs)


