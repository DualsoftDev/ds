[<AutoOpen>]
module Engine.CodeGenCPU.ConvertReal

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Engine.Common.FS

type VertexMReal with

    member v.R1_RealInitialStart(): CommentedStatement  =
        let set = v.G.Expr <&&> v.OG.Expr
        let rst = v.H.Expr

        (set, rst) ==| (v.RR, getFuncName())

    member v.R2_RealJobComplete(): CommentedStatement  =
        let real = v.Vertex :?> Real
        let sReset =  v.GetStrongResetRootAndReadys()

        let setCoins = real.CoinRelays.ToAndElseOn v.System
        let rstCoins = real.CoinRelays.ToOrElseOff v.System
        let set  = v.G.Expr <&&> setCoins
                    <&&>  v.GG.Expr
                    <&&> sReset
        let rst  = v.H.Expr <&&> !!rstCoins

        (set, rst) ==| (v.ET, getFuncName())

    member v.R2_1_GoingRelayGroup(): CommentedStatement  =
        let goingRelays = getResetWeakEdgeSources(v, false).GetResetWeakResults(v)
        let set  = goingRelays.ToAndElseOn(v.System) <||> v.SF.Expr
        let rst  = if goingRelays.any() then  v.ET.Expr else v._off.Expr

        (set, rst) ==| (v.GG, getFuncName())

    member v.R3_RealStartPoint(): CommentedStatement  =
        let set = (v.G.Expr <&&> !!v.RR.Expr) <||>
                  (v.H.Expr <&&> !!v.OG.Expr)
        let rst = v._off.Expr

        (set, rst) ==| (v.RO, getFuncName())

type VertexManager with
    member v.R1_RealInitialStart(): CommentedStatement  = (v :?> VertexMReal).R1_RealInitialStart()
    member v.R2_RealJobComplete() : CommentedStatement  = (v :?> VertexMReal).R2_RealJobComplete()
    member v.R2_1_GoingRelayGroup()  : CommentedStatement    = (v :?> VertexMReal).R2_1_GoingRelayGroup()
    member v.R3_RealStartPoint()  : CommentedStatement  = (v :?> VertexMReal).R3_RealStartPoint()
