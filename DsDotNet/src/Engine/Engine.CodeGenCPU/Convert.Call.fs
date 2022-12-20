[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core


type VertexMemoryManager with
    ///C1 Call 시작조건 Statement 만들기
    member call.CreateCallStartRung(srcs:VertexMemoryManager seq, real:VertexMemoryManager): Statement =
        let sets  =
            [   for s in srcs do
                    s.Relay
                real.Going
            ].ToTags()
        let rsts  = [call.Relay].ToTags()
        call.StartTag <== FuncExt.GetNoRelayExpr(sets, rsts)


    ///C2 Call 작업완료 Statement 만들기
    member call.CreateCallRelayRung(srcs:VertexMemoryManager seq, tags:TagBase<bool> seq, parentReal:VertexMemoryManager): Statement =
        let sets  = [ for s in srcs -> s.Relay :> TagBase<bool> ] @ List.ofSeq tags
        let rsts  = [parentReal.Homing].ToTags()
        let relay = call.Relay

        call.Relay <== FuncExt.GetRelayExpr(sets, rsts, relay)

    ///C3 Call 시작출력 Statement 만들기
    member call.CreateOutputRungs(tags:TagBase<bool> seq) : Statement seq =
        tags.Select(fun outTag -> outTag <== tag2expr call.StartTag)

    ///C4 Call Start to Api TX.Start Statement 만들기
    member call.CreateLinkTxRungs(tags:TagBase<bool> seq) : Statement seq =
        tags.Select(fun txTag -> txTag <== tag2expr call.StartTag)

    //C5 Call End from  Api RX.End  Statement 만들기
    member call.TryCreateLinkRxStatement(tags:TagBase<bool> seq): Statement option =
        if tags.Any() then
            Some (call.EndTag <== FuncExt.GetAnd(tags))
        else
            None

    //C6 Call Tx ~ Rx 내용없을시 Coin Start-End 직접연결
    member call.CreateDirectLinkRung(): Statement  =
        call.EndTag <== FuncExt.GetAnd([call.StartTag])

