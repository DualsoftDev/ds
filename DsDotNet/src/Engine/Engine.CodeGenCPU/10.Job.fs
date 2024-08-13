[<AutoOpen>]
module Engine.CodeGenCPU.ConvertJob

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type Job with
     
    member j.J1_JobActionOuts(call:Call) =
        let _off = j.System._off.Expr
        let fn = getFuncName()
        [|
            for td in j.TaskDefs do
                if td.ExistOutput then 
                    let rstMemos = call.MutualResetCoins.Select(fun c->c.VC.MM)
                    let sets =
                        if RuntimeDS.Package.IsPackageSIM() then _off
                        else td.GetPlanOutput(j).Expr

                    let outParam = td.GetOutParam(j)
                    let emg = call.Flow.emg_st.Expr
                  
                    if outParam.DataType = DuBOOL then 
                        if j.ActionType = Push then 
                            yield (sets, rstMemos.ToOr()) ==| (td.OutTag:?> Tag<bool>, fn)  //단동 실린더? 멈추면 반대로 움직여서 emg 삽입??
                        else 
                            yield (sets, emg) --| (td.OutTag:?> Tag<bool>, fn)
                    else
                        let valExpr = outParam.WriteValue|>literal2expr
                        let valDefalut = outParam.DefaultValue|>literal2expr
                        if j.ActionType = Push then
                            yield (sets, valExpr) --> (td.OutTag, fn)
                        else 
                            let tempRising  = getSM(j).GetTempBoolTag(td.QualifiedName) 
                            yield! (sets, j.System) --^ (tempRising,  fn)
                            yield (tempRising.Expr, valExpr) --> (td.OutTag, fn)
                            yield (emg, valDefalut) --> (td.OutTag, fn)
        |]

                            //if RuntimeDS.Package.IsPLCorPLCSIM() then
                            //    yield (fbRising[sets], valExpr) --> (td.OutTag, fn)
                            //elif RuntimeDS.Package.IsPCorPCSIM() then                                
                            //else    
                            //    failWithLog $"Not supported {RuntimeDS.Package} package"

    member j.J2_InputDetected() =
        let _off = j.System._off.Expr
        let jm = getJM(j)
        let sets =  match j.ActionInExpr with
                    | Some inExprs -> inExprs
                    | None -> _off

        (sets, _off) --| (jm.InDetected, getFuncName())


    member j.J3_OutputDetected() =
        let _off = j.System._off.Expr
        let jm = getJM(j)
        let sets =  match j.ActionOutExpr with
                    | Some outExprs -> outExprs
                    | None -> _off
         
        (sets, _off) --| (jm.OutDetected, getFuncName())

