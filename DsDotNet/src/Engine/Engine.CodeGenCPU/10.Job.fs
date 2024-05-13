[<AutoOpen>]
module Engine.CodeGenCPU.ConvertJob

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type JobManager with

    member j.J1_JobAndTag() =
        let sys = j.Job.System
        let devs = j.Job.DeviceDefs
        let hasInputdevs = devs.Where(fun d-> d.ExistInput)
        [
            if j.Job.DataType = DuBOOL && hasInputdevs.any() 
            then 
                let andSets = hasInputdevs.Select(fun d-> d.GetInExpr()).ToAndElseOff()
                yield (andSets, sys._off.Expr) --| (j.JobBoolValueTag, getFuncName())
        ]

    member j.J2_JobValueTag() =
        let devs = j.Job.DeviceDefs
        if j.Job.DataType <> DuBOOL && devs.Count() > 1
        then 
            failWithLog $"Job {j.Job.Name} {j.Job.DataType} bool 타입이 아니면 하나의 Device만 할당 가능합니다."
        else 
            let hasInputdevs = devs.Where(fun d-> d.ExistInput)
            [
                if hasInputdevs.any()
                then
                    yield (j.Job.System._on.Expr, hasInputdevs.Head().InTag.ToExpression()) --> (j.JobValueTag, getFuncName())
            ]
     
    member j.J3_JobActionOuts() =
        let job = j.Job
        let jobCoins = job.System.GetVerticesOfJobCoins(job)

        let _off = job.System._off.Expr
        [
            for td in job.DeviceDefs do
                if td.ExistOutput
                then 
                    let api = td.ApiItem
                    let rstMemos = jobCoins.SelectMany(fun coin->coin.MutualResetCoins.Select(fun c->c.VC.MM))
                    let sets =
                        if RuntimeDS.Package.IsPackageEmulation()
                        then api.PE.Expr <&&> api.PS.Expr <&&> _off
                        else api.PE.Expr <&&> api.PS.Expr <&&> !!rstMemos.ToOrElseOff()

                    if job.ActionType = JobActionType.Push 
                    then 
                        let rstPush = rstMemos.ToOr()
                        if j.Job.DataType = DuBOOL
                        then yield (sets, rstPush  ) ==| (td.OutTag:?> Tag<bool>, getFuncName())
                        else failWithLog $"{job.Name} {job.ActionType} 은 bool 타입만 지원합니다." 
                    else 
                        if j.Job.DataType = DuBOOL
                        then yield (sets, _off) --| (td.OutTag:?> Tag<bool>, getFuncName())
                        else  
                             yield (sets, td.OutParam.DevValue.Value|>literal2expr) --> (td.OutTag, getFuncName())
        ]
   

