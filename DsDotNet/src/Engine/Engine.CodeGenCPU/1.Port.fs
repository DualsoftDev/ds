[<AutoOpen>]
module Engine.CodeGenCPU.ConvertPort

open Engine.CodeGenCPU
open Engine.Core

type VertexMemoryManager with
    
    member Real.P1_RealStartPort(srcs:Tag<bool> seq): Statement =
        Real.StartPort <== srcs.ToAnd()

    member Real.P2_RealResetPort(srcs:Tag<bool> seq): Statement =
        Real.ResetPort <== srcs.ToAnd()

    member Real.P3_RealEndPort(srcs:Tag<bool> seq): Statement =
        Real.EndPort <==  srcs.ToAnd()
   
    member Call.P4_CallStartPort(srcs:Tag<bool> seq): Statement =
        Call.StartPort <== srcs.ToAnd()

    member Call.P5_CallResetPort(srcs:Tag<bool> seq): Statement =
        Call.ResetPort <== srcs.ToAnd()

    member Call.P6_CallEndPort(srcs:Tag<bool> seq): Statement =
        Call.EndPort <==  srcs.ToAnd()
