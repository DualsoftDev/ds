[<AutoOpen>]
module Engine.CodeGenCPU.ConvertFlow

open System.Linq
open Engine.CodeGenCPU
open Engine.Core

type VertexMemoryManager with
        ///F1. Real 자신의    Start Statement 만들기
    member pReal.TryCreateRealStartRung(srcs:VertexMemoryManager seq): CommentedStatement option =
        if srcs.Any() then
            let sets  = srcs.Select(fun f->f.EndTag).ToAnd()
            let rsts  = [pReal.EndTag].ToAnd()
            let statement = pReal.StartTag.GetRelay (sets, rsts); 
            statement |> withNoComment |> Some //pReal.Pause _Auto 로 변경 필요
        else
            None
        ///F2. Real 자신의 Reset going relay  Statement 만들기
    member realSrc.CreateResetGoingRung(realTgt:VertexMemoryManager , going:Tag<bool>) : CommentedStatement =
        let sets  = [realSrc.Going].ToAnd()
        let rsts  = [realTgt.Homing].ToAnd()
        let statement = going.GetRelay(sets, rsts) //pReal.Pause _Auto 로 변경 필요
        statement |> withNoComment

        ///F3. Real 자신의    Reset Statement 만들기
    member real.TryCreateRealResetRung(goingSrcs:Tag<bool> seq): CommentedStatement option =
        if goingSrcs.Any() then
            //going relay srcs
            let sets  =  tags2AndExpr goingSrcs 
            let rsts  = !! [real.EndTag].ToAnd()
            let statement = real.ResetTag.GetRelay(sets, rsts)
            statement |> withNoComment |> Some //pReal.Pause _Auto 로 변경 필요
        else
            None
