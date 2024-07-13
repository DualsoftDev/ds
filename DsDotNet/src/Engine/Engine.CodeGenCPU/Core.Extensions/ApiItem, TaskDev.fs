namespace rec Engine.CodeGenCPU

open Engine.Core
open System.Linq
open ConvertCoreExtUtils

[<AutoOpen>]
module ConvertCpuApiItem =
    
    //let getLimitFromReals(api:ApiItem) = 

    //    let systemGraph = api.ApiSystem.MergeFlowGraphs()
    //    let reals = api.TXs.GetPathReals(systemGraph)
        
    //    let timeout = reals.Sum(fun r->r.TimeParam.Value.USL)
    //    let timeShort = reals.Sum(fun r->r.TimeParam.Value.LSL)
    //    timeout, timeShort
    
    type TaskDev with
        member td.ExistInput   = addressExist td.InAddress
        member td.ExistOutput  = addressExist td.OutAddress

        member td.PS     = getDM(td).PS
        member td.PE     = getDM(td).PE


    type ApiItem with
        member a.APISET                 = getAM(a).APISET
        member a.ApiItemSetPusle        = getAM(a).ApiItemSetPusle
        member a.ApiItemSetPusleRelay   = getAM(a).ApiItemSetPusleRelay
        member a.ApiItemSetPusleHold    = getAM(a).ApiItemSetPusleHold

        member a.APIEND     = getAM(a).APIEND

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






