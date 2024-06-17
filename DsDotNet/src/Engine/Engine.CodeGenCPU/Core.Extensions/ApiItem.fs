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


    type ApiItem with
        member a.PS     = getAM(a).PS
        member a.PE     = getAM(a).PE
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






