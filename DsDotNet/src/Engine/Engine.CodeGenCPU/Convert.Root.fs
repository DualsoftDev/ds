[<AutoOpen>]
module Engine.CodeGenCPU.ConvertRoot

open System.Linq
open System.Runtime.CompilerServices
open Engine.CodeGenCPU
open Engine.Core

[<AutoOpen>]
[<Extension>]
type StatementRoot =


    [<Extension>] static member TryCreateRungForRealStart(pReal:DsMemory, srcs:DsMemory seq) =
                    if srcs.Any()
                    then
                        let sets  = srcs.Select(fun f->f.End).ToTags()
                        let rsts  = [pReal.End].ToTags()
                        pReal.Start <== fLogicalAnd [FuncExt.GetRelayExpr(sets, rsts, pReal.Start); tag pReal.Pause] |> Some //pReal.Pause _Auto 로 변경 필요
                    else None

    [<Extension>] static member TryGetRealResetStatement(real:DsMemory, goingSrcs:DsMemory seq) =
                    if goingSrcs.Any()
                    then
                        //going relay srcs
                        let sets  = goingSrcs.Select(fun f->f.Relay).ToTags()
                        let rsts  = [real.End].ToTags()
                        real.Reset <== fLogicalAnd [FuncExt.GetRelayExprReverseReset(sets, rsts, real.Reset); tag real.Pause] |> Some//pReal.Pause _Auto 로 변경 필요
                    else None

    [<Extension>] static member CreateRungForResetGoing(realSrc:DsMemory, realTgt:DsMemory , going:DsMemory ) =
                    let sets  = [realSrc.Going].ToTags()
                    let rsts  = [realTgt.Homing].ToTags()
                    going.Relay <== FuncExt.GetRelayExpr(sets, rsts, going.Relay) //pReal.Pause _Auto 로 변경 필요


    [<Extension>] static member CreateRungForResetSelf(pReal:DsMemory ) =
                    let rsts  = [pReal.End].ToTags()
                    pReal.Reset <== FuncExt.GetAnd(rsts)

