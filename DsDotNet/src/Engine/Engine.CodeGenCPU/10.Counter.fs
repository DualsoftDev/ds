[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCounter

open System.Linq
open Engine.Core
open Engine.CodeGenCPU




type VertexManager with
       //test ahn
    member v.C1_FinishRingCounter(): CommentedStatement  = 
        (v.PA.Expr, v.System._off.Expr) --| (v.PA, "C1" )

        