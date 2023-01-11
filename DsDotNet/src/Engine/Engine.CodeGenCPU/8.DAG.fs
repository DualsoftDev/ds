[<AutoOpen>]
module Engine.CodeGenCPU.ConvertDAG

open System.Linq
open Engine.Core
open Engine.CodeGenCPU


type VertexManager with
    member v.D1_DAGHeadStart(): CommentedStatement list =
        let real = v.Vertex :?> Real
        let v = v :?> VertexReal
        let coins = real.Graph.Inits.Select(getVM)
        let sets = v.G.Expr<&&> v.RR.Expr
        [
            for coin in coins do
                let coin = coin :?> VertexCoin
                yield (sets, coin.CR.Expr) --| (coin.ST, "D1" )
        ]
   
    member v.D2_DAGTailStart(): CommentedStatement list = 
        let real = v.Vertex :?> Real
        let coins = real.Graph.Vertices.Except(real.Graph.Inits).Select(getVM)
        [
            for coin in coins do
                let coin = coin :?> VertexCoin
                let srcs = real.Graph.FindEdgeSources(coin.Vertex, StartEdge).Select(getVMCoin).CRs()
                let sets = ([v.G] @ srcs).ToAnd()
                yield (sets, coin.CR.Expr) --| (coin.ST, "D2" )
        ]
   

    member v.D3_DAGComplete(): CommentedStatement list = 
        let real = v.Vertex :?> Real
        let realV = v
        let children = real.Graph.Vertices.Select(getVM)
        [
            for child in children do
                let child = child :?> VertexCoin
                let ands = 
                    match child.GetPureCall() with
                    |Some call ->  if call.UsingTon 
                                        then call.V.TON.DN.Expr   //On Delay
                                        else call.INs.EmptyOnElseToAnd(v.System)
                    |None -> failwith "Error D3_DAGComplete"

                let sets = ands <&&> child.ST.Expr <&&> child.ET.Expr
                let rsts = realV.H.Expr
                yield (sets, rsts) --| (child.CR, "D3" )
        ]   