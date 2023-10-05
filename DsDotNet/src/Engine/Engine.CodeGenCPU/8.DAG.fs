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
                let rsts = coin.ET.Expr <||>coin.RT.Expr <||> !!v.Flow.dop.Expr
                yield (sets, rsts) ==| (coin.ST, getFuncName())
        ]

    member v.D2_DAGTailStart(): CommentedStatement list =
        let real = v.Vertex :?> Real
        let coins = real.Graph.Vertices.Except(real.Graph.Inits).Select(getVM)
        [
            for coin in coins do
                let coin = coin :?> VertexMCoin
                let sets = coin.GetWeakStartDAGAndCausals()  <&&>  v.G.Expr
                let rsts = coin.ET.Expr <||>coin.RT.Expr <||> !!v.Flow.dop.Expr
                yield (sets, rsts) ==| (coin.ST, getFuncName() )
        ]
        
    member v.D3_DAGCoinEnd(bRoot:bool): CommentedStatement list =
        let real = v.Vertex :?> Real
        let children = real.Graph.Vertices.Select(getVM)
        [
            for child in children do
                let coin = child :?> VertexMCoin
                let call = coin.Vertex :?> CallDev
                let setEnd =
                    let action =
                        if call.UsingTon
                            then call.V.TDON.DN.Expr   //On Delay
                            else call.INsFuns
                  
                    (action <||> coin._sim.Expr)
                    <&&> if bRoot then coin._on.Expr
                                  else call.PEs.ToAndElseOn(coin.System) 

                let sets = coin.ST.Expr <&&> setEnd <&&> real.V.G.Expr
                let rsts = coin.RT.Expr
                yield (sets, rsts) ==| (coin.ET, getFuncName() )
        ]


    member v.D4_DAGCoinReset(): CommentedStatement list =
        let real = v.Vertex :?> Real
        let children = real.Graph.Vertices.Select(getVM)
        [
            for child in children do
                let child = child :?> VertexMCoin
                let sets = real.V.RT.Expr
                let rsts = child.R.Expr
                yield (sets, rsts) ==| (child.RT, getFuncName() )
        ]
