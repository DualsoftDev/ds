namespace rec Engine.CodeGenCPU

open Engine.Core
open System.Linq
open ConvertCoreExtUtils

[<AutoOpen>]
module ConvertCpuApiItem =
    let getTM(td:TaskDev) = td.TagManager:?> TaskDevManager
    
    type TaskDev with
        member td.ExistInput   = addressExist td.InAddress
        member td.ExistOutput  = addressExist td.OutAddress

        member td.GetPlanStart(job:Job)  = getTM(td).PlanStart(job)   
        member td.GetPlanEnd(job:Job)    = getTM(td).PlanEnd(job)   
        member td.GetPlanOutput(job:Job) = getTM(td).PlanOutput(job)   
        
        //member td.GetPlanStart(api:ApiItem)  = getTM(td).PlanStart
        //member td.GetPlanEnd(api:ApiItem)    = getTM(td).PlanEnd(api)   
        //member td.GetPlanOutput(api:ApiItem) = getTM(td).PlanOutput(api)   
        
    type ApiItem with
        member a.ApiItemSet                 = getAM(a).ApiItemSet
        member a.ApiItemSetPusle        = getAM(a).ApiItemSetPusle
        member a.ApiItemSetPusleRelay   = getAM(a).ApiItemSetPusleRelay
        member a.ApiItemSetPusleHold    = getAM(a).ApiItemSetPusleHold

        member a.ApiItemEnd     = getAM(a).ApiItemEnd

        ///sensorLinking
        member a.SL1     = getAM(a).SL1
        ///sensorLinked
        member a.SL2     = getAM(a).SL2
    
        member a.RxET    = getVMReal(a.RX).ET 
        member a.TxST    = getVMReal(a.TX).ST 
        //member a.UpperLimit =
        //    match a.TimeParam with
        //    |Some t -> t.USL 
        //    |None -> getUpperLimitFromReals(a)






