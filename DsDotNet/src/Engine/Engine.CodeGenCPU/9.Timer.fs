[<AutoOpen>]
module Engine.CodeGenCPU.ConvertTimer

open System.Linq
open Engine.Core
open Engine.CodeGenCPU



type VertexManager with
       //test ahn
    member v.T1_DelayInput(): CommentedStatement  = 
        (v.PA.Expr, v.System._off.Expr) --| (v.PA, "T1" )

          //test ahn
    member v.T2_SustainOutput(): CommentedStatement  = 
        (v.PA.Expr, v.System._off.Expr) --| (v.PA, "T2" )

        