namespace Engine.CodeGenCPU

open System.Linq
open Engine.CodeGenCPU
open Engine.Core

[<AutoOpen>]
module ConvertRoot =
    type VertexMemoryManager with
            ///F1. Real 자신의    Start Statement 만들기
        member pReal.TryCreateRealStartRung(srcs:VertexMemoryManager seq): CommentedStatement option =
            if srcs.Any() then
                let sets  = srcs.Select(fun f->f.EndTag).ToTags()
                let rsts  = [pReal.EndTag].ToTags()
                let statement = pReal.StartTag <== fLogicalAnd [FuncExt.GetRelayExpr(sets, rsts, pReal.StartTag); tag2expr pReal.Pause]
                statement |> withNoComment |> Some //pReal.Pause _Auto 로 변경 필요
            else
                None
            ///F2. Real 자신의 Reset going relay  Statement 만들기
        member realSrc.CreateResetGoingRung(realTgt:VertexMemoryManager , going:DsTag<bool>) : CommentedStatement =
            let sets  = [realSrc.Going].ToTags()
            let rsts  = [realTgt.Homing].ToTags()
            let statement = going <== FuncExt.GetRelayExpr(sets, rsts, going) //pReal.Pause _Auto 로 변경 필요
            statement |> withNoComment

           ///F3. Real 자신의    Reset Statement 만들기
        member real.TryCreateRealResetRung(goingSrcs:DsTag<bool> seq): CommentedStatement option =
           if goingSrcs.Any() then
               //going relay srcs
               let sets  = goingSrcs.ToTags()
               let rsts  = [real.EndTag].ToTags()
               let statement = real.ResetTag <== fLogicalAnd [FuncExt.GetRelayExprReverseReset(sets, rsts, real.ResetTag); tag2expr real.Pause]
               statement |> withNoComment |> Some //pReal.Pause _Auto 로 변경 필요
           else
                None
