[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core

let getPureCall(v:Vertex) =
            match v with
            | :? Call as c  ->  v :?> Call
            | :? Alias as a  -> a.TargetWrapper.GetTarget() :?> Call
            |_ -> failwith "Error"

type VertexManager with

    member v.C1_CallActionOut(): CommentedStatement list = 
        let call = v.Vertex :?> Call
        
        let sets = ([v.ST] @ v.GetSharedCall().STs()).ToOr()
        let rsts = if call.MutualResetOuts.Any() then call.MutualResetOuts.ToOr() else v.System._off.Expr
        [
            for out in call.OUTs do
                yield (sets, rsts) --| (out, "C1" )
        ]

                 
    member v.C2_CallTx(): CommentedStatement list = 
        let call = getPureCall  v.Vertex
        let sets = ([v.ST] @ v.GetSharedCall().STs()).ToOr()
        let rsts = v.System._off.Expr
        [
            for out in call.TXs do
                yield (sets, rsts) --| (out, "C2" )
        ]

    member v.C3_CallRx(): CommentedStatement  = 
        let call = getPureCall  v.Vertex
        let sets = if call.RXs.Any() then call.RXs.ToAnd() else v.System._on.Expr
        let rsts = v.System._off.Expr

        (sets, rsts) --| (v.ET, "C3" )
    