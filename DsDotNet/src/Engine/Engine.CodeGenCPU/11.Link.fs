[<AutoOpen>]
module Engine.CodeGenCPU.ConvertLink

open System.Linq
open Engine.Core
open Engine.CodeGenCPU



type VertexManager with
       //test ahn
    member v.L1_LinkStart(): CommentedStatement  = 
        (v.PA.Expr, v.OFF.Expr) --| (v.PA, "L1" )

    member v.L2_LinkReset(): CommentedStatement  = 
        (v.PA.Expr, v.OFF.Expr) --| (v.PA, "L2" )

    member v.L3_LinkStartReset(): CommentedStatement  = 
        (v.PA.Expr, v.OFF.Expr) --| (v.PA, "L3" )

        