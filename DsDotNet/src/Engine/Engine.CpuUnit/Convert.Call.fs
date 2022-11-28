[<AutoOpen>]
module Engine.Cpu.ConvertCall

open System.Linq
open System.Runtime.CompilerServices
open Engine.Cpu
 
[<AutoOpen>]
[<Extension>]
type StatementCall =
    

    [<Extension>] static member StartRung(call:DsTag, srcs:DsTag seq, real:DsTag) =
                    let sets  = srcs.Select(fun f->f.Relay).ToTags().Append(real.Going)
                    let rsts  = [call.Relay].ToTags()
                    Assign(FuncExt.DoNoRelay(sets, rsts), call.Start)


    [<Extension>] static member RelayRung(call:DsTag, srcs:DsTag seq, srcOuts:ActionTag<_> seq, real:DsTag) =
                    let sets  = srcs.Select(fun s->s.Relay).ToTags() |> Seq.append (srcOuts.ToTags())
                    let rsts  = [real.Homing].ToTags()
                    let relay = call.Relay

                    Assign(FuncExt.DoRelay(sets, rsts, relay) , call.Relay)


    [<Extension>] static member OutputRung(call:DsTag, callOut:ActionTag<_>)  =
                    Assign(FuncExt.DoAnd([call.Start]) , callOut)
    
    [<Extension>] static member LinkTx(call:DsTag)  =
                    Assign(FuncExt.DoAnd([call.Start]) , call.Start)
    
    [<Extension>] static member LinkRx(call:DsTag)  =
                    Assign(FuncExt.DoAnd([call.Start]) , call.End)
    
     