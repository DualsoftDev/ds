[<AutoOpen>]
module Engine.CodeGenCPU.ConvertMonitor

open System.Linq
open Engine.Core
open Engine.CodeGenCPU

type VertexManager with
   
        //test ahn
    member v.M1_OriginMonitor(): CommentedStatement  = 
        let real = v.Vertex :?> Real
        let jobDefInfos = OriginHelper.GetOriginsWithJobDefs real.Graph
        
        (v.OG.Expr, v.OFF.Expr) --| (v.OG, "M1" )

        //test ahn
    member v.M2_PauseMonitor(): CommentedStatement  = 
        (v.PA.Expr, v.OFF.Expr) --| (v.PA, "M2" )

        //test ahn
    member v.M3_ErrorTXMonitor(): CommentedStatement  = 
        (v.E1.Expr, v.OFF.Expr) --| (v.E1, "M3" )

        //test ahn
    member v.M4_ErrorRXMonitor(): CommentedStatement  = 
        (v.E2.Expr, v.OFF.Expr) --| (v.E2, "M4" )

   
   