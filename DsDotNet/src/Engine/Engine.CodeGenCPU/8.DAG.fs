[<AutoOpen>]
module Engine.CodeGenCPU.ConvertDAG

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type VertexManager with

    member v.D1_DAGHeadStart() =
        let real = v.Vertex :?> Real
        let v = v :?> VertexMReal
        let coins = real.Graph.Inits.Select(getVM)
        [
            for coin in coins do
                let safety = coin.GetPureCall().Value.SafetyExpr
                let coin = coin :?> VertexMCoin
                let sets = v.RR.Expr <&&>  v.G.Expr <&&> safety
                let rsts = coin.ET.Expr <||> coin.RT.Expr 
                yield (sets, rsts) ==| (coin.ST, getFuncName())
        ]

    member v.D2_DAGTailStart() =
        let real = v.Vertex :?> Real
        let coins = real.Graph.Vertices.Except(real.Graph.Inits).Select(getVM)
        [
            for coin in coins do
                let safety = coin.GetPureCall().Value.SafetyExpr
                let coin = coin :?> VertexMCoin
                let sets = coin.GetWeakStartDAGAndCausals()  <&&>  v.G.Expr <&&> safety
                let rsts = coin.ET.Expr <||> coin.RT.Expr  
                yield (sets, rsts) ==| (coin.ST, getFuncName() )
        ]
        
    member v.D3_DAGCoinEnd() =
        let real = v.Vertex :?> Real
        let children = real.Graph.Vertices.Select(getVM)
        [
            for child in children do
                let coin = child :?> VertexMCoin
                let call = coin.GetPure().V.Vertex :?> Call
                let setEnd =
                    if call.UsingTon then 
                        call.PEs.ToAndElseOn() <&&> coin.TDON.DN.Expr
                    else 
                        call.PEs.ToAndElseOn() <&&> (call.EndAction <||> coin._sim.Expr)

                let sets = coin.ST.Expr <&&> setEnd <&&> real.V.G.Expr
                let rsts = coin.RT.Expr
                yield (sets, rsts) ==| (coin.ET, getFuncName() )
        ]


    member v.D4_DAGCoinReset() =
        let real = v.Vertex :?> Real
        let children = real.Graph.Vertices.Select(getVM)
        [
            for child in children do
                let child = child :?> VertexMCoin
                let sets = real.V.RT.Expr <&&> !!real.V.G.Expr
                let rsts = child.R.Expr
                yield (sets, rsts) ==| (child.RT, getFuncName() )
        ]
