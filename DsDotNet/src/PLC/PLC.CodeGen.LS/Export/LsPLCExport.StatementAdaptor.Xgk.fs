namespace PLC.CodeGen.LS

open System.Linq
open System.Security

open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.Common

[<AutoOpen>]
module XgkTypeConvertorModule =
    type XgkTimerCounterStructResetCoil(tc:TimerCounterBaseStruct) =
        inherit TimerCounterBaseStruct(None, tc.Name, tc.DN, tc.PRE, tc.ACC, tc.RES, (tc :> IStorage).DsSystem)
        interface INamedExpressionizableTerminal with
            member x.StorageName = tc.Name


    let operatorToXgkFunctionName op =
        match op with
        | "+" -> "ADD"
        | "-" -> "SUB"
        | "*" -> "MUL"
        | "/" -> "DIV"
        | (">" | ">=" | "<"  | "<="  | "=" | "<>" | "!=" ) -> op
        | _ -> failwithlog "ERROR"

    /// XGK 전용 Statement 확장
    let rec internal statement2XgkStatements (prjParam: XgxProjectParams) (newLocalStorages: XgxStorage) (statement: Statement) : Statement list =
        let augmentedStatements = StatementContainer() // DuAugmentedPLCFunction case

        let newStatements =
            match statement with
            // e.g: XGK 에서 bool b3 = $nn1 > $nn2; 와 같은 선언의 처리.  다음과 같이 2개의 문장으로 분리한다.
            // bool b3;
            // b3 := $nn1 > $nn2;
            | DuVarDecl(exp, decl) when exp.Terminal.IsNone ->
                newLocalStorages.Add decl
                let stmt = DuAssign(exp, decl)
                statement2XgkStatements prjParam newLocalStorages stmt

            | DuTimer tmr when tmr.ResetCondition.IsSome ->
                // XGI timer 의 RST 조건을 XGK 에서는 Reset rung 으로 분리한다.
                let resetStatement = DuAssign(tmr.ResetCondition.Value, new XgkTimerCounterStructResetCoil(tmr.Timer.TimerStruct))
                [ statement; resetStatement ]

            | DuTimer _  -> [ statement ]

            | DuCounter ctr ->
                let statements = ResizeArray<Statement>([statement])
                // XGI counter 의 LD(Load) 조건을 XGK 에서는 Reset rung 으로 분리한다.
                let resetCoil = new XgkTimerCounterStructResetCoil(ctr.Counter.CounterStruct)
                let typ = ctr.Counter.Type
                match typ with
                | CTD -> DuAssign(ctr.LoadCondition.Value, resetCoil) |> statements.Add
                | (CTR|CTU|CTUD) -> DuAssign(ctr.ResetCondition.Value, resetCoil) |> statements.Add

                if typ = CTUD then
                    let mutable newStatement = statement
                    let mutable newCtr = ctr

                    // newStatementGenerator : fun () -> DuCounter({ ctr with UpCondition = Some ldVarExp })
                    let replaceComplexCondition (_ctr: CounterStatement) (cond:IExpression<bool>) (newStatementGenerator:IExpression<bool> -> Statement) =
                        let assignStatement, ldVar = cond.ToAssignStatementAndAuotVariable prjParam
                        statements.Add assignStatement
                        newLocalStorages.Add ldVar

                        let ldVarExp = ldVar.ToExpression() :?> IExpression<bool>
                        newStatement <- newStatementGenerator(ldVarExp)
                        match newStatement with
                        | DuCounter ctr -> newCtr <- ctr
                        | _ -> failwithlog "ERROR"

                        statements[0] <- newStatement


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

                statements.ToFSharpList()

            | _ ->
                // 공용 처리
                statement2XgxStatements prjParam newLocalStorages statement

        augmentedStatements @ newStatements |> List.ofSeq

