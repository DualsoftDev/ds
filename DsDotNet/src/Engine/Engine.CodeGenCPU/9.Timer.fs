[<AutoOpen>]
module Engine.CodeGenCPU.ConvertTimer

open System.Linq
open Engine.Core
open Engine.CodeGenCPU



type VertexManager with
       //test ahn
    member v.T1_DelayInput(): CommentedStatement  = 
        let call = v.Vertex :?> Call
        let sets = if call.INs.Any() then call.INs.ToAnd() else v.System._on.Expr
        (sets) --@ (v.TRX, "T1" )

          //test ahn
    member v.T2_SustainOutput(): CommentedStatement  = 
        let call = v.Vertex :?> Call
        let sets = if call.OUTs.Any() then call.OUTs.ToAnd() else v.System._on.Expr
        (sets) --@ (v.TRX, "T2" )

        