[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core

type VertexManager with

    member v.C1_CallActionOut(): CommentedStatement list = 
        let call = v.GetPureCall().Value
        
        let sets = ([v.ST] @ v.GetSharedCall().STs()).ToOr()
        let rsts = call.MutualResetOuts.EmptyOffElseToOr v.System
        [
            for out in call.OUTs do
                yield (sets, rsts) --| (out, "C1" )
        ]

                 
    member v.C2_CallTx(): CommentedStatement list = 
        let call = v.GetPureCall().Value
        let sets = ([v.ST] @ v.GetSharedCall().STs()).ToOr()
        let rsts = v.System._off.Expr
        [
            for out in call.TXs do
                yield (sets, rsts) --| (out, "C2" )
        ]

    member v.C3_CallRx(): CommentedStatement  = 
        let call = v.GetPureCall().Value
        let sets = call.RXs.EmptyOnElseToAnd v.System
        let rsts = v.System._off.Expr

        (sets, rsts) --| (v.ET, "C3" )
    