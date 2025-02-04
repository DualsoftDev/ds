[<AutoOpen>]
module Engine.CodeGenCPU.ConvertJob

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type TaskDevManager with




    member d.J1_JobActionOuts(coins:Vertex seq) =
        let fn = getFuncName()
        let _off = coins.First()._off.Expr

        let rstMemos = coins.SelectMany(fun c-> c.MutualResetCoins.Select(fun c->c.VC.PS)).Distinct()
        let flowEmg = coins.Select(fun c-> c.Flow.emg_st).ToOr()
        let flowPause = coins.Select(fun c-> c.Flow.p_st).ToOr()
        let emgActions = coins.SelectMany(fun c-> c.Flow.HWEmergencyDigitalActions)
        let pauseActions = coins.SelectMany(fun c-> c.Flow.HWPauseDigitalActions)

        (*StatementTypeAnalog*)
        let getStatementTypeAnalog(sets, td:TaskDev, call:Call) =
            [
                let valExpr = call.ValueParamIO.Out.WriteValue |> any2expr
                yield (sets, valExpr) --> (td.OutTag, fn)  //test ahn Analog pulse 출력 안함 테스트중
            ]
        (*StatementTypeDigital*)
        let getStatementTypeDigital(set, td:TaskDev, callActionType:CallActionType) =
            let getStatement(actionSet:IExpression option, actionRst:IExpression option) =
                let sets = if actionSet.IsSome then actionSet.Value<||>set else set
                let rsts = if actionRst.IsSome then actionRst.Value else _off
                if callActionType= CallActionType.Push then
                    if rstMemos.IsEmpty() then
                        failWithLog $"{td.FullName} MutualResetCoins is empty"
                    (sets, rstMemos.ToOr()) ==| (td.OutTag:?> Tag<bool>, fn)  //단동 실린더? 멈추면 반대로 움직여서 emg 삽입??
                elif callActionType= CallActionType.ActionNormal then
                    (sets, rsts) --| (td.OutTag:?> Tag<bool>, fn)
                else
                    failWithLog "Invalid CallTypeAction"

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




        [|
            if d.TaskDev.ExistOutput
            then
                let coinPlanOuts = coins.Select(fun s-> s.VC.PS.Expr <&&> s.VC.PE.Expr).ToOr()
                let sets = if RuntimeDS.ModelConfig.RuntimePackage.IsPackageSIM() then _off else coinPlanOuts
                let callAction = coins.Head().GetPureCall().CallActionType
                if d.TaskDev.OutDataType = DuBOOL then
                    yield getStatementTypeDigital(sets, d.TaskDev, callAction)
                else
                    for coin in coins do
                        yield! getStatementTypeAnalog(sets, d.TaskDev, coin:?>Call)
        |]

