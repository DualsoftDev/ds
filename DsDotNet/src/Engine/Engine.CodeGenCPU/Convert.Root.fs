namespace Engine.CodeGenCPU

open System.Linq
open Engine.CodeGenCPU
open Engine.Core

[<AutoOpen>]
module ConvertRoot =
    type VertexMemoryManager with
            ///F1. Real 자신의    Start Statement 만들기
        member pReal.TryCreateRealStart(srcs:VertexMemoryManager seq) =
            if srcs.Any() then
                let sets  = srcs.Select(fun f->f.EndTag).ToTags()
                let rsts  = [pReal.EndTag].ToTags()
                pReal.StartTag <== fLogicalAnd [FuncExt.GetRelayExpr(sets, rsts, pReal.StartTag); tag2expr pReal.Pause] |> Some //pReal.Pause _Auto 로 변경 필요
            else
                None
            ///F2. Real 자신의 Reset going relay  Statement 만들기
        member realSrc.CreateResetGoing(realTgt:VertexMemoryManager , going:DsTag<bool>) =
            let sets  = [realSrc.Going].ToTags()
            let rsts  = [realTgt.Homing].ToTags()
            going <== FuncExt.GetRelayExpr(sets, rsts, going) //pReal.Pause _Auto 로 변경 필요

           ///F3. Real 자신의    Reset Statement 만들기
        member real.TryGetRealResetStatement(goingSrcs:DsTag<bool> seq) =
           if goingSrcs.Any() then
               //going relay srcs
               let sets  = goingSrcs.ToTags()
               let rsts  = [real.EndTag].ToTags()
               real.ResetTag <== fLogicalAnd [FuncExt.GetRelayExprReverseReset(sets, rsts, real.ResetTag); tag2expr real.Pause] |> Some//pReal.Pause _Auto 로 변경 필요
           else
                None
