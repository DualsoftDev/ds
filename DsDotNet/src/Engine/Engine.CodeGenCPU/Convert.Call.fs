[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open System.Runtime.CompilerServices
open Engine.CodeGenCPU
open Engine.Core

[<AutoOpen>]
[<Extension>]
type StatementCall =


    ///C1 Call 시작조건 Statement 만들기
    [<Extension>] static member CreateRungForCallStart(call:DsMemory, srcs:DsMemory seq, real:DsMemory) =
                    let sets  = srcs.Select(fun f->f.Relay).ToTags().Append(real.Going)
                    let rsts  = [call.Relay].ToTags()
                    call.Start <== FuncExt.GetNoRelayExpr(sets, rsts)


    ///C2 Call 작업완료 Statement 만들기
    [<Extension>] static member CreateRungForCallRelay(call:DsMemory, srcs:DsMemory seq, tags:Tag<bool> seq, parentReal:DsMemory) =
                    let sets  = srcs.Select(fun s->s.Relay).ToTags() |> Seq.append (tags.ToTags())
                    let rsts  = [parentReal.Homing].ToTags()
                    let relay = call.Relay

                    call.Relay <== FuncExt.GetRelayExpr(sets, rsts, relay)


    ///C3 Call 시작출력 Statement 만들기
    [<Extension>] static member CreateRungForOutputs(call:DsMemory, tags:Tag<bool> seq)  =
                    tags.Select(fun outTag -> outTag <== FuncExt.GetAnd([call.Start]))

    ///C4 Call Start to Api TX.Start Statement 만들기
    [<Extension>] static member CreateRungForLinkTx(call:DsMemory)  = ////ahn 구현필요
                    call.Start <== FuncExt.GetAnd([call.Start])

    //C5 Call End from  Api RX.End  Statement 만들기
    [<Extension>] static member CreateRungForLinkRx(call:DsMemory)  = //ahn 구현필요
                    call.Start <== FuncExt.GetAnd([call.Start])

