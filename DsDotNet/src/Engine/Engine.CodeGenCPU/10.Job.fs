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
                  
                    if outParam.Type = DuBOOL then 
                        if j.ActionType = Push then 
                            yield (sets, rstMemos.ToOr()) ==| (td.OutTag:?> Tag<bool>, fn)
                        else 
                            yield (sets, _off) --| (td.OutTag:?> Tag<bool>, fn)

                    else
                        let valExpr = outParam.DevValue.Value|>literal2expr
                        if j.ActionType = Push then
                            yield (sets, valExpr) --> (td.OutTag, fn)
                        else 
                            if RuntimeDS.Package.IsPLCorPLCSIM() then
                                yield (fbRising[sets], valExpr) --> (td.OutTag, fn)

                            elif RuntimeDS.Package.IsPCorPCSIM() then                                
                                let tempRising  = getSM(j).GetTempBoolTag(td.QualifiedName) 
                                yield! (sets, j.System) --^ (tempRising,  fn)
                                yield (tempRising.Expr, valExpr) --> (td.OutTag, fn)
                            else    
                                failWithLog $"Not supported {RuntimeDS.Package} package"
        |]


   

