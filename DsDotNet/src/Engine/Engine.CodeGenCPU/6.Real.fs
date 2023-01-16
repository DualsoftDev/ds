[<AutoOpen>]
module Engine.CodeGenCPU.ConvertReal

open System.Linq
open Engine.Core
open Engine.CodeGenCPU

type VertexMReal with

    member v.R1_RealInitialStart(): CommentedStatement  =
        let set = v.G.Expr <&&> v.OG.Expr
        let rst = v.H.Expr

        (set, rst) ==| (v.RR, "R1")

    member v.R2_RealJobComplete(): CommentedStatement  =
        let real = v.Vertex :?> Real
        let set  = v.G.Expr <&&> real.CoinRelays.ToAndElseOn v.System
        let rst  = v.H.Expr

        (set, rst) ==| (v.ET, "R2")


    member v.R3_RealStartPoint(): CommentedStatement  =
        let set = (v.G.Expr <&&> v.RR.Expr) <||>
                  (v.H.Expr <&&> v.OG.Expr)
        let rst = v.System._off.Expr

        (set, rst) ==| (v.RO, "R3")

type VertexManager with
    member v.R1_RealInitialStart(): CommentedStatement  = (v :?> VertexMReal).R1_RealInitialStart()
    member v.R2_RealJobComplete() : CommentedStatement  = (v :?> VertexMReal).R2_RealJobComplete()
    member v.R3_RealStartPoint()  : CommentedStatement  = (v :?> VertexMReal).R3_RealStartPoint()
