[<AutoOpen>]
module Engine.CodeGenCPU.ConvertReal

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type VertexMReal with

    member v.R1_RealInitialStart(): CommentedStatement  =
        let set = v.G.Expr <&&> v.OG.Expr
        let rst = v.RT.Expr

        (set, rst) ==| (v.RR, getFuncName())

    member v.R2_RealJobComplete(): CommentedStatement  =
        let real = v.Vertex :?> Real
        let wReset =  v.GetWeakResetRootAndReadys()
        let sReset =  v.GetStrongResetRootAndReadys()
        let setCoins = real.CoinRelays.ToAndElseOn v.System

        let set  = v.G.Expr <&&> setCoins  <&&> wReset <&&> sReset
        let rst  = v.H.Expr

        (set, rst) ==| (v.ET, getFuncName())



    member v.R3_RealStartPoint(): CommentedStatement  =
        let set = (v.G.Expr <&&> !!v.RR.Expr) <||>
                  (v.H.Expr <&&> !!v.OG.Expr)
        let rst = v._off.Expr

        (set, rst) --| (v.RO, getFuncName())

type VertexManager with
    member v.R1_RealInitialStart(): CommentedStatement  = (v :?> VertexMReal).R1_RealInitialStart()
    member v.R2_RealJobComplete() : CommentedStatement  = (v :?> VertexMReal).R2_RealJobComplete()
    member v.R3_RealStartPoint()  : CommentedStatement  = (v :?> VertexMReal).R3_RealStartPoint()
