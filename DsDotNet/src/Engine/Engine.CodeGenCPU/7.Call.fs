[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core


type VertexManager with
    /////C1 Call 시작조건 Statement 만들기
    //member call.CreateCallStartRung(srcs:VertexManager seq, real:VertexManager): CommentedStatement =
    //    let sets  =
    //        [   for s in srcs do
    //                s.CR
    //            real.G
    //        ].ToAnd()
    //    let rsts  = [call.CR].ToAnd()
    //    (sets, rsts) --| (call.ST, "")


    /////C2 Call 작업완료 Statement 만들기
    //member call.CreateCallRelayRung(srcs:VertexManager seq, tags:Tag<bool> seq, parentReal:VertexManager): CommentedStatement =
    //    let sets  = srcs.Select(fun s -> s.CR).Cast<Tag<bool>>() |> Seq.append tags  |> toAnd 
    //    let rsts  = [parentReal.H]

    //    (sets, rsts.ToAnd()) ==|  (call.CR , "")

    /////C3 Call 시작출력 Statement 만들기
    //member call.CreateOutputRungs(tags:Tag<bool> seq) : CommentedStatement seq =
    //    [ for outTag in tags do
    //        let statement = outTag <== tag2expr call.ST
    //        statement |> withNoComment ]

    /////C4 Call Start to Api TX.Start Statement 만들기
    //member call.CreateLinkTxRungs(tags:Tag<bool> seq) : CommentedStatement seq =
    //    [ for txTag in tags do
    //        let statement = txTag <== tag2expr call.ST
    //        statement |> withNoComment ]

    ////C5 Call End from  Api RX.End  Statement 만들기
    //member call.TryCreateLinkRxStatement(tags:Tag<bool> seq): CommentedStatement option =
    //    if tags.Any() then
    //        let statement = call.EndTag <== toAnd tags 
    //        statement |> withNoComment |> Some
    //    else
    //        None

    ////C6 Call Tx ~ Rx 내용없을시 Coin Start-End 직접연결
    //member call.CreateDirectLinkRung(): CommentedStatement  =
    //    let statement = call.EndTag <== [call.ST].ToAnd()
    //    statement |> withNoComment

              //test ahn
    member v.C1_CallActionOut(): CommentedStatement  = 
        (v.PA.Expr, v.System._off.Expr) --| (v.PA, "C1" )
          //test ahn
    member v.C2_CallInitialComplete(): CommentedStatement  = 
        (v.PA.Expr, v.System._off.Expr) --| (v.PA, "C2" )
          //test ahn
    member v.C3_CallTailComplete(): CommentedStatement  = 
        (v.PA.Expr, v.System._off.Expr) --| (v.PA, "C3" )
                  //test ahn
    member v.C4_CallTx(): CommentedStatement  = 
        (v.PA.Expr, v.System._off.Expr) --| (v.PA, "C4" )
                  //test ahn
    member v.C5_CallRx(): CommentedStatement  = 
        (v.PA.Expr, v.System._off.Expr) --| (v.PA, "C5" )
    