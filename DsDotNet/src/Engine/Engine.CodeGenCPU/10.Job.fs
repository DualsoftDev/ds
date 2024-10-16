[<AutoOpen>]
module Engine.CodeGenCPU.ConvertJob

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type Job with

    member j.J1_JobActionOuts(call:Call) =
        let fn = getFuncName()
        let _off = j.System._off.Expr
        let rstMemos = call.MutualResetCoins.Select(fun c->c.VC.MM)
        let flowEmg = call.Flow.emg_st.Expr
        let flowPause = call.Flow.emg_st.Expr
        let emgActions = call.Flow.HWEmergencyDigitalActions
        let pauseActions = call.Flow.HWPauseDigitalActions

        let getStatementTypeDigital(set, td:TaskDev) =
            let getStatement(actionSet:IExpression option, actionRst:IExpression option) =
                let sets = if actionSet.IsSome then actionSet.Value<||>set else set
                let rsts = if actionRst.IsSome then actionRst.Value else _off
                if j.ActionType = JobTypeAction.Push then
                    if rstMemos.IsEmpty then
                        failWithLog $"{call.Name} MutualResetCoins is empty"
                    (sets, rstMemos.ToOr()) ==| (td.OutTag:?> Tag<bool>, fn)  //단동 실린더? 멈추면 반대로 움직여서 emg 삽입??
                elif j.ActionType = JobTypeAction.ActionNormal then
                    (sets, rsts) --| (td.OutTag:?> Tag<bool>, fn)
                else 
                    failWithLog "Invalid JobTypeAction"

            match emgActions.TryFind(fun a->a.OutAddress = td.OutAddress)
                , pauseActions.TryFind(fun a->a.OutAddress = td.OutAddress) with
            | Some emg, Some pause ->
                match emg.DigitalOutputTarget.Value, pause.DigitalOutputTarget.Value with
                | true, true   -> getStatement (Some (flowEmg<||>flowPause), None)
                | true, false  -> getStatement (Some flowEmg, Some flowPause)
                | false, true  -> getStatement (Some flowPause, Some flowEmg)
                | false, false -> getStatement (None, Some (flowEmg<||>flowPause))
            | Some emg, None ->
                match emg.DigitalOutputTarget.Value with
                | true  -> getStatement (Some flowEmg, None)
                | false -> getStatement (None, Some flowEmg)
            | None, Some pause  ->
                match pause.DigitalOutputTarget.Value with
                | true  -> getStatement (Some flowPause, None)
                | false -> getStatement (None, Some flowPause)
            | None, None  ->
                getStatement (None, None)

        let getStatementTypeAnalog(sets, td:TaskDev) =
            [
                let outParam = td.GetOutParam(j)
                let valExpr = outParam.WriteValue |> any2expr
                yield (sets, valExpr) --> (td.OutTag, fn)  //test ahn Analog pulse 출력 안함 테스트중
            ]

        [|
            for td in j.TaskDefs.Where(fun t->t.ExistOutput) do
                let sets =
                    if RuntimeDS.Package.IsPackageSIM() then _off
                    else td.GetPlanOutput(j).Expr <||> _off
         
                if td.GetOutParam(j).DataType = DuBOOL then
                    yield getStatementTypeDigital(sets, td) 
                else
                    yield! getStatementTypeAnalog(sets, td) 
        |]

    member j.J2_InputDetected() =
        let _off = j.System._off.Expr
        let jm = getJM(j)
        match j.ActionInExpr with
        | Some inExprs -> [(inExprs, _off) --| (jm.InDetected, getFuncName())]
        | None -> []

    member j.J3_OutputDetected() =
        let _off = j.System._off.Expr
        let jm = getJM(j)
        match j.ActionOutExpr with
        | Some outExprs ->[(outExprs, _off) --| (jm.OutDetected, getFuncName())]
        | None -> []

