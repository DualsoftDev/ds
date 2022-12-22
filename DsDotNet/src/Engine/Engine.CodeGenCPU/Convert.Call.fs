[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core


type VertexMemoryManager with
    ///C1 Call 시작조건 Statement 만들기
    member call.CreateCallStartRung(srcs:VertexMemoryManager seq, real:VertexMemoryManager): CommentedStatement =
        let sets  =
            [   for s in srcs do
                    s.Relay
                real.Going
            ].ToTags()
        let rsts  = [call.Relay].ToTags()
        let statement = call.StartTag <== FuncExt.GetNoRelayExpr(sets, rsts)
        CommentedStatement ("", statement)


    ///C2 Call 작업완료 Statement 만들기
    member call.CreateCallRelayRung(srcs:VertexMemoryManager seq, tags:TagBase<bool> seq, parentReal:VertexMemoryManager): CommentedStatement =
        let sets  = [ for s in srcs -> s.Relay :> TagBase<bool> ] @ List.ofSeq tags
        let rsts  = [parentReal.Homing].ToTags()
        let relay = call.Relay

        let statement = call.Relay <== FuncExt.GetRelayExpr(sets, rsts, relay)
        CommentedStatement ("", statement)

    ///C3 Call 시작출력 Statement 만들기
    member call.CreateOutputRungs(tags:TagBase<bool> seq) : CommentedStatement seq =
        [ for outTag in tags do
            let statement = outTag <== tag2expr call.StartTag
            CommentedStatement ("", statement) ]

    ///C4 Call Start to Api TX.Start Statement 만들기
    member call.CreateLinkTxRungs(tags:TagBase<bool> seq) : CommentedStatement seq =
        [ for txTag in tags do
            let statement = txTag <== tag2expr call.StartTag
            CommentedStatement ("", statement) ]

    //C5 Call End from  Api RX.End  Statement 만들기
    member call.TryCreateLinkRxStatement(tags:TagBase<bool> seq): CommentedStatement option =
        if tags.Any() then
            let statement = call.EndTag <== FuncExt.GetAnd(tags)
            Some (CommentedStatement ("", statement))
        else
            None

    //C6 Call Tx ~ Rx 내용없을시 Coin Start-End 직접연결
    member call.CreateDirectLinkRung(): CommentedStatement  =
        let statement = call.EndTag <== FuncExt.GetAnd([call.StartTag])
        CommentedStatement ("", statement)

