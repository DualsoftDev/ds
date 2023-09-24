[<AutoOpen>]
module Engine.CodeGenCPU.ConvertDAG

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type VertexManager with

    member v.D1_DAGHeadStart(): CommentedStatement list =
        let real = v.Vertex :?> Real
        let v = v :?> VertexMReal
        let coins = real.Graph.Inits.Select(getVM)
        let sets = v.RR.Expr <&&>  v.G.Expr
        [
            for coin in coins do
                let coin = coin :?> VertexMCoin
                let rsts = coin.CR.Expr <||>coin.RT.Expr <||> !!v.Flow.dop.Expr
                yield (sets, rsts) ==| (coin.ST, getFuncName())
        ]

    member v.D2_DAGTailStart(): CommentedStatement list =
        let real = v.Vertex :?> Real
        let coins = real.Graph.Vertices.Except(real.Graph.Inits).Select(getVM)
        [
            for coin in coins do
                let coin = coin :?> VertexMCoin
                let sets = coin.GetWeakStartDAGAndCausals()  <&&>  v.G.Expr
                let rsts = coin.CR.Expr <||>coin.RT.Expr <||> !!v.Flow.dop.Expr
                yield (sets, rsts) ==| (coin.ST, getFuncName() )
        ]


    member v.D3_DAGCoinRelay(): CommentedStatement list =
        let real = v.Vertex :?> Real
        let children = real.Graph.Vertices.Select(getVM)
        [
            for child in children do
                let child = child :?> VertexMCoin
                let sets = child.ST.Expr <&&> child.ET.Expr<&&> real.V.G.Expr
                let rsts = child.RT.Expr
                yield (sets, rsts) ==| (child.CR, getFuncName() )
        ]


     member v.D4_DAGCoinReset(): CommentedStatement list =
        let real = v.Vertex :?> Real
        let children = real.Graph.Vertices.Select(getVM)
        [
            for child in children do
                let child = child :?> VertexMCoin
                let sets = real.V.RP.Expr
                let rsts = child.R.Expr
                yield (sets, rsts) ==| (child.RT, getFuncName() )
        ]
