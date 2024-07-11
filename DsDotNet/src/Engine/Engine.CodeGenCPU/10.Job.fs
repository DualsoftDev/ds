[<AutoOpen>]
module Engine.CodeGenCPU.ConvertJob

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type Job with
     
    member j.J1_JobActionOuts() =
        let vs = j.System.GetVerticesOfCoins()
        let jobCoins = vs.GetVerticesOfJobCoins(j).OfType<Call>()

        let _off = j.System._off.Expr
        [
            for td in j.DeviceDefs do
                if td.ExistOutput
                then 
                    let rstMemos = jobCoins.SelectMany(fun coin->coin.MutualResetCoins.Select(fun c->c.VC.MM))
                    let sets =
                        if RuntimeDS.Package.IsPackageSIM() then _off
                        else td.PE.Expr <&&> td.PS.Expr <&&> !@rstMemos.ToOrElseOff()


                    let outParam = td.GetOutParam(j)
                    if j.ActionType = Push 
                    then 
                        if td.ExistOutput
                        then yield (sets, rstMemos.ToOr()) ==| (td.OutTag:?> Tag<bool>, getFuncName())
                    else 
                        if outParam.Type = DuBOOL
                        then 
                            yield (sets, _off) --| (td.OutTag:?> Tag<bool>, getFuncName())
                        else 
                            yield (sets, outParam.DevValue.Value|>literal2expr) --> (td.OutTag, getFuncName())
        ]
   

