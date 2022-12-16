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
    [<Extension>] static member CreateCallStart(call:VertexM, srcs:VertexM seq, real:VertexM) =
                    let sets  = srcs.Select(fun f->f.Relay).ToTags().Append(real.Going)
                    let rsts  = [call.Relay].ToTags()
                    call.StartTag <== FuncExt.GetNoRelayExpr(sets, rsts)


    ///C2 Call 작업완료 Statement 만들기
    [<Extension>] static member CreateCallRelay(call:VertexM, srcs:VertexM seq, tags:TagBase<bool> seq, parentReal:VertexM) =
                    let sets  = srcs.Select(fun s->s.Relay).ToTags() |> Seq.append (tags.ToTags())
                    let rsts  = [parentReal.Homing].ToTags()
                    let relay = call.Relay

                    call.Relay <== FuncExt.GetRelayExpr(sets, rsts, relay)

    ///C3 Call 시작출력 Statement 만들기
    [<Extension>] static member CreateOutputs(call:VertexM, tags:TagBase<bool> seq)  =
                    tags.Select(fun outTag -> outTag <== FuncExt.GetAnd([call.StartTag]))

    ///C4 Call Start to Api TX.Start Statement 만들기
    [<Extension>] static member CreateLinkTxs(call:VertexM, tags:TagBase<bool> seq)  =
                    tags.Select(fun txTag -> txTag <== FuncExt.GetAnd([call.StartTag]))

    //C5 Call End from  Api RX.End  Statement 만들기
    [<Extension>] static member CreateLinkRx(call:VertexM, tags:TagBase<bool> seq)  =
                    if tags.Any()
                    then
                         Some (call.EndTag <== FuncExt.GetAnd(tags))
                    else None

    //C6 Call Tx ~ Rx 내용없을시 Coin Start-End 직접연결
    [<Extension>] static member CreateDirectLink(call:VertexM)  =
                    call.EndTag <== FuncExt.GetAnd([call.StartTag])
            
