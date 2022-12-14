[<AutoOpen>]
module Engine.CodeGenCPU.ConvertRoot

open System.Linq
open System.Runtime.CompilerServices
open Engine.CodeGenCPU
open Engine.Core

[<AutoOpen>]
[<Extension>]
type StatementRoot =

    //F3. Real 자신의    Start Statement 만들기
    [<Extension>] static member TryCreateRealStart(pReal:DsMemory, srcs:DsMemory seq) =
                    if srcs.Any()
                    then
                        let sets  = srcs.Select(fun f->f.End).ToTags()
                        let rsts  = [pReal.End].ToTags()
                        pReal.Start <== fLogicalAnd [FuncExt.GetRelayExpr(sets, rsts, pReal.Start); tag pReal.Pause] |> Some //pReal.Pause _Auto 로 변경 필요
                    else None
    ///F2. Real 자신의    Reset Statement 만들기
    [<Extension>] static member TryGetRealResetStatement(real:DsMemory, goingSrcs:DsTag<bool> seq) =
                    if goingSrcs.Any()
                    then
                        //going relay srcs
                        let sets  = goingSrcs.ToTags()
                        let rsts  = [real.End].ToTags()
                        real.Reset <== fLogicalAnd [FuncExt.GetRelayExprReverseReset(sets, rsts, real.Reset); tag real.Pause] |> Some//pReal.Pause _Auto 로 변경 필요
                    else None
    ///F1. Real 자신의 Reset going relay  Statement 만들기
    [<Extension>] static member CreateResetGoing(realSrc:DsMemory, realTgt:DsMemory , going:DsTag<bool>) =
                    let sets  = [realSrc.Going].ToTags()
                    let rsts  = [realTgt.Homing].ToTags()
                    going <== FuncExt.GetRelayExpr(sets, rsts, going) //pReal.Pause _Auto 로 변경 필요

   