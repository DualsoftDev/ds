[<AutoOpen>]
module Engine.Cpu.ConvertCall

open System.Linq
open System.Runtime.CompilerServices
open Engine.Cpu

[<AutoOpen>]
[<Extension>]
type StatementCall =


    [<Extension>] static member CreateRungForCallStart(call:DsMemory, srcs:DsMemory seq, real:DsMemory) =
                    let sets  = srcs.Select(fun f->f.Relay).ToTags().Append(real.Going)
                    let rsts  = [call.Relay].ToTags()
                    call.Start <== FuncExt.GetNoRelayExpr(sets, rsts)


    [<Extension>] static member CreateRungForCallRelay(call:DsMemory, srcs:DsMemory seq, srcOuts:PlcTag<_> seq, parentReal:DsMemory) =
                    let sets  = srcs.Select(fun s->s.Relay).ToTags() |> Seq.append (srcOuts.ToTags())
                    let rsts  = [parentReal.Homing].ToTags()
                    let relay = call.Relay

                    call.Relay <== FuncExt.GetRelayExpr(sets, rsts, relay)


    [<Extension>] static member CreateRungForOutput(call:DsMemory, callOut:PlcTag<_>)  =
                    callOut <== FuncExt.GetAnd([call.Start])

    [<Extension>] static member CreateRungForLinkTx(call:DsMemory)  = ////ahn 구현필요
                    call.Start <== FuncExt.GetAnd([call.Start])

    [<Extension>] static member CreateRungForLinkRx(call:DsMemory)  = //ahn 구현필요
                    call.Start <== FuncExt.GetAnd([call.Start])

