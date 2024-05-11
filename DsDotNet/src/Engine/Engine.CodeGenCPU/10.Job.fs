[<AutoOpen>]
module Engine.CodeGenCPU.ConvertJob

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type JobManager with
   

   //다시작성 Bool 아닌것은 job에 하나 Dev 만 그리고 데이터 카피로
    member j.J1_JobAndOrTags() =
        let sys = j.Job.System
        let devs = j.Job.DeviceDefs
        let inTags = devs.Where(fun d-> d.ExistInput)
                         .Select(fun d-> d.InTag)

        let andSets = if inTags.any() then inTags.Select(fun f->f:?> Tag<bool>).ToAnd() else sys._off.Expr
        let orSets  = if inTags.any() then inTags.Select(fun f->f:?> Tag<bool>).ToOr()  else sys._off.Expr
        [
            yield (andSets, sys._off.Expr) --| (j.JobAndExprTag, getFuncName())
            yield (orSets, sys._off.Expr) --| (j.JobOrExprTag, getFuncName())
        ]
    //다시 작성 실출력 코드로
    member j.J2_JobActionOuts() =
        let sys = j.Job.System
        let devs = j.Job.DeviceDefs
        let inTags = devs.Where(fun d-> d.ExistInput)
                         .Select(fun d-> d.InTag)
        let andSets = if inTags.any() then inTags.Select(fun f->f:?> Tag<bool>).ToAnd() else sys._off.Expr
        let orSets = if inTags.any() then inTags.Select(fun f->f:?> Tag<bool>).ToOr() else sys._off.Expr
        [
            yield (andSets, sys._off.Expr) --| (j.JobAndExprTag, getFuncName())
            yield (orSets, sys._off.Expr) --| (j.JobOrExprTag, getFuncName())
        ]
        
    //member v.J2_ActionOut() =
    //    let v = v :?> VertexMCall
    //    let coin = v.Vertex :?> Call
    //    [
    //        let rstNormal = coin._off.Expr
    //        for td in coin.TargetJob.DeviceDefs do
    //            let api = td.ApiItem
    //            if td.ExistOutput
    //            then 
    //                let rstMemos = coin.MutualResetCalls.Select(fun c->c.VC.MM)
    //                let sets =
    //                    if RuntimeDS.Package.IsPackageEmulation()
    //                    then api.PE.Expr <&&> api.PS.Expr <&&> coin._off.Expr
    //                    else api.PE.Expr <&&> api.PS.Expr <&&> !!rstMemos.ToOrElseOff()

    //                if coin.TargetJob.ActionType = JobActionType.Push 
    //                then 
    //                        let rstPush = rstMemos.ToOr()
                        
    //                        yield (sets, rstPush  ) ==| (td.AO, getFuncName())
    //                else 
    //                        yield (sets, rstNormal) --| (td.AO, getFuncName())
    //    ]


