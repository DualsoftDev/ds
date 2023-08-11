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
        let sets = v.RR.Expr
        [
            for coin in coins do
                let coin = coin :?> VertexMCoin
                yield (sets, coin.CR.Expr <||> !!v.Flow.dop.Expr) ==| (coin.ST, getFuncName())
        ]

    member v.D2_DAGTailStart(): CommentedStatement list =
        let real = v.Vertex :?> Real
        let coins = real.Graph.Vertices.Except(real.Graph.Inits).Select(getVM)
        [
            for coin in coins do
                let coin = coin :?> VertexMCoin
                let sets = coin.GetWeakStartRootAndCausals()
                yield (sets, coin.CR.Expr <||> !!v.Flow.dop.Expr) ==| (coin.ST, getFuncName() )
        ]


    member v.D3_DAGCoinRelay(): CommentedStatement list =
        let real = v.Vertex :?> Real
        let children = real.Graph.Vertices.Select(getVM)
        [
            for child in children do
                let child = child :?> VertexMCoin
                let sets = child.ST.Expr <&&> child.ET.Expr
                let rsts = child.H.Expr
                yield (sets, rsts) ==| (child.CR, getFuncName() )
        ]


     member v.D4_DAGCoinReset(): CommentedStatement list =
        let real = v.Vertex :?> Real
        let children = real.Graph.Vertices.Select(getVM)
        [
            for child in children do
                let child = child :?> VertexMCoin
                let sets = real.V.H.Expr
                let rsts = child.R.Expr
                yield (sets, rsts) ==| (child.RT, getFuncName() )
        ]
