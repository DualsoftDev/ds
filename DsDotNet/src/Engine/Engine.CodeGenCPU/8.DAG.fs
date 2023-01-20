[<AutoOpen>]
module Engine.CodeGenCPU.ConvertDAG

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Engine.Common.FS


type VertexManager with

    member v.D1_DAGHeadStart(): CommentedStatement list =
        let real = v.Vertex :?> Real
        let v = v :?> VertexMReal
        let coins = real.Graph.Inits.Select(getVM)
        let sets = v.G.Expr<&&> v.RR.Expr
        [
            for coin in coins do
                let coin = coin :?> VertexMCoin
                yield (sets, coin.CR.Expr) ==| (coin.ST, "D1" )
        ]

    member v.D2_DAGTailStart(): CommentedStatement list =
        let real = v.Vertex :?> Real
        let coins = real.Graph.Vertices.Except(real.Graph.Inits).Select(getVM)
        [
            for coin in coins do
                let coin = coin :?> VertexMCoin
                let srcsWeek, srcsStrong  = getStartEdgeSources(real.Graph, coin.Vertex)

                if srcsWeek.Any() then
                    let sets = srcsWeek.GetCausalTags(v.System, false)
                    yield (sets, coin.CR.Expr) ==| (coin.ST, "D2" )

                if srcsStrong.Any() then
                    let sets = srcsStrong.GetCausalTags(v.System, false)
                    yield (sets, coin.CR.Expr) --| (coin.ST, "D2" )

        ]


    member v.D3_DAGCoinComplete(): CommentedStatement list =
        let real = v.Vertex :?> Real
        let realV = v
        let children = real.Graph.Vertices.Select(getVM)
        [
            for child in children do
                let child = child :?> VertexMCoin
                let ands =
                    match child.GetPureCall() with
                    |Some call ->  if call.UsingTon
                                        then call.V.TON.DN.Expr    //On Delay
                                        else call.INs.ToAndElseOn(v.System)
                    |None -> failwithlog "Error D3_DAGCoinComplete"

                let sets = ands <&&> child.ST.Expr <&&> child.ET.Expr
                let rsts = realV.H.Expr
                yield (sets, rsts) ==| (child.CR, "D3" )
        ]
