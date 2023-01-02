[<AutoOpen>]
module Engine.CodeGenCPU.ConvertReal

open System.Linq
open Engine.Core
open Engine.CodeGenCPU

type VertexManager with

    member v.R1_RealInitialStart(): CommentedStatement  = 
        let sets = v.G.Expr <&&> v.OG.Expr  
        let rsts = v.H.Expr

        (sets, rsts) ==| (v.RR, "R1")

    member v.R2_RealJobComplete(): CommentedStatement  = 
        let real = v.Vertex :?> Real
        let sets = if real.CoinRelays.Any() then real.CoinRelays.ToAnd() else v.System._on.Expr
        let rsts = v.System._off.Expr

        (sets, rsts) --| (v.ET, "R2")
