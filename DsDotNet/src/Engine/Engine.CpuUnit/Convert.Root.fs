[<AutoOpen>]
module Engine.Cpu.ConvertRoot

open System.Linq
open System.Runtime.CompilerServices
open Engine.Cpu
 
[<AutoOpen>]
[<Extension>]
type StatementRoot =
    
    
    [<Extension>] static member StartCondition(pReal:DsTag, srcs:DsTag seq) =
                    let sets  = srcs.Select(fun f->f.End).ToTags()
                    let rsts  = [pReal.End].ToTags()
                    Assign(anD[FuncExt.DoRelay(sets, rsts, pReal.Start);pReal.Pause], pReal.Start) //pReal.Pause _Auto 로 변경 필요
    
    [<Extension>] static member ResetCondition(real:DsTag, srcs:DsTag seq) =
                    //going relay srcs
                    let sets  = srcs.Select(fun f->f.Relay).ToTags()
                    let rsts  = [real.End].ToTags()
                    Assign(anD[FuncExt.DoRelayReverseReset(sets, rsts, real.Reset);real.Pause], real.Reset) //pReal.Pause _Auto 로 변경 필요
    
    
    [<Extension>] static member ResetGoingRelay(realSrc:DsTag, realTgt:DsTag , going:DsTag ) =
                    let sets  = [realSrc.Going].ToTags()
                    let rsts  = [realTgt.Homing].ToTags()
                    Assign(FuncExt.DoRelay(sets, rsts, going.Relay), going.Relay) //pReal.Pause _Auto 로 변경 필요

    [<Extension>] static member ResetSelf(pReal:DsTag ) =
                    let rsts  = [pReal.End].ToTags()
                    Assign(FuncExt.DoAnd(rsts), pReal.Reset) 

