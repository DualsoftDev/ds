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

    type Statement with
        /// XGK 전용 Statement 확장
        member internal x.ToStatementsXgk (prjParam: XgxProjectParams, augs:Augments) : unit =
            match x with
            | DuAssign(condition, exp, target) ->
                let numStatementsBefore = augs.Statements.Count
                let exp2 = exp.AugmentXgk(prjParam, condition, Some target, augs)
                let duplicated =
                    option {
                        // a := a 등의 형태 체크
                        let! terminal = exp2.Terminal
                        let! variable = terminal.Variable
                        return variable = target
                    } |> Option.defaultValue false

                let needAdd = augs.Statements.Count = numStatementsBefore || (exp <> exp2 && not duplicated)
                if needAdd then
                    let assignStatement = DuAssign(condition, exp2, target)
                    assignStatement.ToStatements(prjParam, augs)
                else
                    ()


            // e.g: XGK 에서 bool b3 = $nn1 > $nn2; 와 같은 선언의 처리.
            // XGK 에서 다음과 같이 2개의 문장으로 분리한다.
            // bool b3;
            // b3 = $nn1 > $nn2;
            | DuVarDecl(exp, decl) ->
                augs.Storages.Add decl
                let stmt = DuAssign(Some fake1OnExpression, exp, decl)
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


