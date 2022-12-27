[<AutoOpen>]
module Engine.CodeGenCPU.ConvertDAG

open System.Linq
open Engine.Core
open Engine.CodeGenCPU


type VertexManager with
       //test ahn
    member v.D1_DAGInitialStart(): CommentedStatement  = 
        (v.PA.Expr, v.OFF.Expr) --| (v.PA, "D1" )

          //test ahn
    member v.D2_DAGTailStart(): CommentedStatement  = 
        (v.PA.Expr, v.OFF.Expr) --| (v.PA, "D2" )

        