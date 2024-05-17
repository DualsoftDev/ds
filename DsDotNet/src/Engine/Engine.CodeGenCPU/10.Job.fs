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
                    let api = td.ApiItem
                    let rstMemos = jobCoins.SelectMany(fun coin->coin.MutualResetCoins.Select(fun c->c.VC.MM))
                    let sets =
                        if RuntimeDS.Package.IsPackageEmulation()
                        then api.PE.Expr <&&> api.PS.Expr <&&> _off
                        else api.PE.Expr <&&> api.PS.Expr <&&> !!rstMemos.ToOrElseOff()

                    if j.ActionType = JobActionType.Push 
                    then 
                        let rstPush = rstMemos.ToOr()

                        if td.OutParam.DevType = DuBOOL
                            then 
                                failWithLog $"{td.Name} {j.ActionType} 은 bool 타입만 지원합니다." 
                        if td.ExistOutput
                            then 
                                yield (sets, rstPush  ) ==| (td.OutTag:?> Tag<bool>, getFuncName())

                    else 
                        if td.OutParam.DevType = DuBOOL
                        then 
                            yield (sets, _off) --| (td.OutTag:?> Tag<bool>, getFuncName())
                        elif td.OutParam.DevValue.IsNull() 
                        then 
                            failWithLog $"{td.Name} {td.OutParam.DevAddress} 은 value 값을 입력해야 합니다." 
                        else 
                            yield (sets, td.OutParam.DevValue|>literal2expr) --> (td.OutTag, getFuncName())
        ]
   

