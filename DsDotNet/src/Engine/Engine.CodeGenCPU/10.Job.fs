[<AutoOpen>]
module Engine.CodeGenCPU.ConvertJob

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type Job with
     
    member j.J1_JobActionOuts(call:Call) =
        let _off = j.System._off.Expr
        [
            for td in j.TaskDefs do
                if td.ExistOutput
                then 
                    let rstMemos = call.MutualResetCoins.Select(fun c->c.VC.MM)
                    let sets =
                        if RuntimeDS.Package.IsPackageSIM() then _off
                        else td.GetPO(j).Expr

                    let outParam = td.GetOutParam(j)
                  
                    if outParam.Type = DuBOOL
                    then 
                        if j.ActionType = Push 
                        then 
                            yield (sets, rstMemos.ToOr()) ==| (td.OutTag:?> Tag<bool>, getFuncName())
                        else 
                            yield (sets, _off) --| (td.OutTag:?> Tag<bool>, getFuncName())

                    else
                        if j.ActionType = Push 
                        then
                            yield (sets, outParam.DevValue.Value|>literal2expr) --> (td.OutTag, getFuncName())
                        else 
                            if RuntimeDS.Package.IsPLCorPLCSIM() 
                            then
                                yield (fbRisingAfter[sets], outParam.DevValue.Value|>literal2expr) --> (td.OutTag, getFuncName())

                            elif RuntimeDS.Package.IsPCorPCSIM() then 
                                
                                let tempRising  = getSM(j).GetTempBoolTag(td.QualifiedName) 
                                yield! (sets, j.System) --^ (tempRising,  getFuncName())
                                yield (tempRising.Expr, outParam.DevValue.Value|>literal2expr) --> (td.OutTag, getFuncName())


                            else    
                                failWithLog $"Not supported {RuntimeDS.Package} package"
            

        ]


   

