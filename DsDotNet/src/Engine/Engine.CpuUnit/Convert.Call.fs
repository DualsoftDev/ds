[<AutoOpen>]
module Engine.Cpu.ConvertCall

open System.Linq
open System.Runtime.CompilerServices
open Engine.Cpu
 
[<AutoOpen>]
[<Extension>]
type StatementCall =
    

    [<Extension>] static member GetCallStartStatement(call:DsTag, srcs:DsTag seq, real:DsTag) =
                    let sets  = srcs.Select(fun f->f.Relay).ToTags().Append(real.Going)
                    let rsts  = [call.Relay].ToTags()
                    Assign(FuncExt.GetNoRelayExpr(sets, rsts), call.Start)


    [<Extension>] static member GetCallRelayStatement(call:DsTag, srcs:DsTag seq, srcOuts:ActionTag<_> seq, parentReal:DsTag) =
                    let sets  = srcs.Select(fun s->s.Relay).ToTags() |> Seq.append (srcOuts.ToTags())
                    let rsts  = [parentReal.Homing].ToTags()
                    let relay = call.Relay

                    Assign(FuncExt.GetRelayExpr(sets, rsts, relay) , call.Relay)


    [<Extension>] static member GetOutputStatement(call:DsTag, callOut:ActionTag<_>)  =
                    Assign(FuncExt.DoAnd([call.Start]) , callOut)
    
    [<Extension>] static member GetLinkTxStatement(call:DsTag)  =
                    Assign(FuncExt.DoAnd([call.Start]) , call.Start)
    
    [<Extension>] static member GetLinkRxStatement(call:DsTag)  =
                    Assign(FuncExt.DoAnd([call.Start]) , call.End)
    
     