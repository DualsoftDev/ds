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
        let emg = call.Flow.emg_st.Expr

        let getStatementTypeDigital(sets, td:TaskDev) =
            if j.ActionType = Push then
                (sets, rstMemos.ToOr()) ==| (td.OutTag:?> Tag<bool>, fn)  //단동 실린더? 멈추면 반대로 움직여서 emg 삽입??
            else
                (sets, emg) --| (td.OutTag:?> Tag<bool>, fn)

        let getStatementTypeAnalog(sets, td:TaskDev) =
            [
                let outParam = td.GetOutParam(j)
                let valExpr = outParam.WriteValue |> any2expr
                let valDefalut = outParam.DefaultValue |> any2expr
                if j.ActionType = Push then
                    yield (sets, valExpr) --> (td.OutTag, fn)
                else
                    let tempRising  = getJSM(j).GetTempBoolTag(td.QualifiedName)
                    yield! (sets, j.System) --^ (tempRising,  fn)
                    yield (tempRising.Expr, valExpr) --> (td.OutTag, fn)
                    yield (emg, valDefalut) --> (td.OutTag, fn)
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

