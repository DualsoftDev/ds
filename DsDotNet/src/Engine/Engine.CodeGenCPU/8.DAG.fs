[<AutoOpen>]
module Engine.CodeGenCPU.ConvertDAG

open System.Linq
open Engine.Core
open Engine.CodeGenCPU


type VertexManager with
    member v.D1_DAGHeadStart(): CommentedStatement list =
        let real = v.Vertex :?> Real
        let coins = real.Graph.Inits.Select(getVM)
        let sets = v.G.Expr<&&> v.RR.Expr
        [
            for coin in coins do
                yield (sets, coin.CR.Expr) --| (coin.ST, "D1" )
        ]

    member v.D2_DAGHeadComplete(): CommentedStatement list = 

        let real = v.Vertex :?> Real
        let realV = v
        let coins = real.Graph.Inits.Select(getVM)
        let rsts = realV.H.Expr
        [
            for coin in coins do
                let call = getPureCall  coin.Vertex
                let ins = if call.INs.Any() then call.INs.ToAnd() else v.System._on.Expr
                let sets = ins <&&> realV.RR.Expr <&&> coin.ET.Expr
                
                yield (sets, rsts) --| (coin.CR, "D2" )
        ]


    member v.D3_DAGTailStart(): CommentedStatement list = 
        let real = v.Vertex :?> Real
        let coins = real.Graph.Vertices.Except(real.Graph.Inits).Select(getVM)
        [
            for coin in coins do
                let srcs = real.Graph.FindEdgeSources(coin.Vertex, StartEdge).Select(getVM).CRs()
                let sets = ([v.G] @ srcs).ToAnd()
                yield (sets, coin.CR.Expr) --| (coin.ST, "D3" )
        ]
   

    member v.D4_DAGTailComplete(): CommentedStatement list = 
        let real = v.Vertex :?> Real
        let realV = v
        let coins = real.Graph.Vertices.Except(real.Graph.Inits).Select(getVM)
        [
            for coin in coins do
                let srcs = real.Graph.FindEdgeSources(coin.Vertex, StartEdge).Select(getVM).CRs()
                let call = getPureCall  coin.Vertex
                let ins = if call.INs.Any() then call.INs.ToAnd() else v.System._on.Expr
                let sets = ins <&&> srcs.ToAnd()
                let rsts = realV.H.Expr
                yield (sets, rsts) --| (coin.CR, "D4" )
        ]