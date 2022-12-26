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
                    s.RelayCallDone
                real.Going
            ].ToAnd()
        let rsts  = [call.RelayCallDone].ToAnd()
        let statement = call.StartTag.GetRung(Some sets, Some rsts)
        statement |> withNoComment


    ///C2 Call 작업완료 Statement 만들기
    member call.CreateCallRelayRung(srcs:VertexMemoryManager seq, tags:Tag<bool> seq, parentReal:VertexMemoryManager): CommentedStatement =
        let sets  = srcs.Select(fun s -> s.RelayCallDone).Cast<Tag<bool>>() |> Seq.append tags  |> tags2AndExpr 
        let rsts  = [parentReal.Homing]

        let statement = call.RelayCallDone.GetRelay(sets, rsts.ToAnd())
        statement |> withNoComment

    ///C3 Call 시작출력 Statement 만들기
    member call.CreateOutputRungs(tags:Tag<bool> seq) : CommentedStatement seq =
        [ for outTag in tags do
            let statement = outTag <== tag2expr call.StartTag
            statement |> withNoComment ]

    ///C4 Call Start to Api TX.Start Statement 만들기
    member call.CreateLinkTxRungs(tags:Tag<bool> seq) : CommentedStatement seq =
        [ for txTag in tags do
            let statement = txTag <== tag2expr call.StartTag
            statement |> withNoComment ]

    //C5 Call End from  Api RX.End  Statement 만들기
    member call.TryCreateLinkRxStatement(tags:Tag<bool> seq): CommentedStatement option =
        if tags.Any() then
            let statement = call.EndTag <== tags2AndExpr tags 
            statement |> withNoComment |> Some
        else
            None

    //C6 Call Tx ~ Rx 내용없을시 Coin Start-End 직접연결
    member call.CreateDirectLinkRung(): CommentedStatement  =
        let statement = call.EndTag <== [call.StartTag].ToAnd()
        statement |> withNoComment

