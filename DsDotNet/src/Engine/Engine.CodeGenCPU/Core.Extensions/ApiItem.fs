namespace rec Engine.CodeGenCPU

open Engine.Core
open System.Linq
open ConvertCoreExtUtils

[<AutoOpen>]
module ConvertCpuApiItem =
    
    let getLimitFromReals(api:ApiItem) = 

        let systemGraph = api.ApiSystem.MergeFlowGraphs()
        let reals = api.TXs.GetPathReals(systemGraph)
        
        let timeout = reals.Sum(fun r->r.TimeParam.Value.USL)
        let timeShort = reals.Sum(fun r->r.TimeParam.Value.LSL)
        timeout, timeShort


    type ApiItem with
        member a.PS     = getAM(a).PS
        member a.PE     = getAM(a).PE
        ///sensorLinking
        member a.SL1     = getAM(a).SL1
        ///sensorLinked
        member a.SL2     = getAM(a).SL2
    
        member a.RxETs       = a.RXs |> Seq.map getVMReal |> Seq.map(fun f->f.ET)
        member a.TxSTs       = a.TXs |> Seq.map getVMReal |> Seq.map(fun f->f.ST)
        //member a.UpperLimit =
        //    match a.TimeParam with
        //    |Some t -> t.USL 
        //    |None -> getUpperLimitFromReals(a)






