[<AutoOpen>]
module Engine.CodeGenCPU.ConvertReal

open System.Linq
open Engine.Core
open Engine.CodeGenCPU

type VertexManager with

    member v.R1_RealInitialStart(): CommentedStatement  = 
        let v = v :?> VertexMReal
        let sets = v.G.Expr <&&> v.OG.Expr  
        let rsts = v.H.Expr

        (sets, rsts) ==| (v.RR, "R1")

    member v.R2_RealJobComplete(): CommentedStatement  = 
        let v = v :?> VertexMReal
        let real = v.Vertex :?> Real
        let sets = v.G.Expr <&&> real.CoinRelays.EmptyOnElseToAnd v.System
        let rsts = v.H.Expr

        (sets, rsts) ==| (v.ET, "R2")

    
    member v.R3_RealStartPoint(): CommentedStatement  =  
        let v = v :?> VertexMReal
        let sets = (v.G.Expr <&&> v.RR.Expr) <||> 
                   (v.H.Expr <&&> v.OG.Expr)
        let rsts = v.System._off.Expr

        (sets, rsts) ==| (v.RO, "R3")

