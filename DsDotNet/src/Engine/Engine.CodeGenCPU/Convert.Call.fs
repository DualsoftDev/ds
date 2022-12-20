[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core


type VertexMemoryManager with
    ///C1 Call 시작조건 Statement 만들기
    member call.CreateCallStart(srcs:VertexMemoryManager seq, real:VertexMemoryManager) =
        let sets  =
            [   for s in srcs do
                    s.Relay
                real.Going
            ].ToTags()
        let rsts  = [call.Relay].ToTags()
        call.StartTag <== FuncExt.GetNoRelayExpr(sets, rsts)


    ///C2 Call 작업완료 Statement 만들기
    member call.CreateCallRelay(srcs:VertexMemoryManager seq, tags:TagBase<bool> seq, parentReal:VertexMemoryManager) =
        let sets  = [ for s in srcs -> s.Relay :> TagBase<bool> ] @ List.ofSeq tags
        let rsts  = [parentReal.Homing].ToTags()
        let relay = call.Relay

        call.Relay <== FuncExt.GetRelayExpr(sets, rsts, relay)

    ///C3 Call 시작출력 Statement 만들기
    member call.CreateOutputs(tags:TagBase<bool> seq)  =
        tags.Select(fun outTag -> outTag <== tag2expr call.StartTag)

    ///C4 Call Start to Api TX.Start Statement 만들기
    member call.CreateLinkTxs(tags:TagBase<bool> seq)  =
        tags.Select(fun txTag -> txTag <== tag2expr call.StartTag)

    //C5 Call End from  Api RX.End  Statement 만들기
    member call.CreateLinkRx(tags:TagBase<bool> seq)  =
        if tags.Any() then
            Some (call.EndTag <== FuncExt.GetAnd(tags))
        else
            None

    //C6 Call Tx ~ Rx 내용없을시 Coin Start-End 직접연결
    member call.CreateDirectLink()  =
        call.EndTag <== FuncExt.GetAnd([call.StartTag])

