[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type VertexManager with
    member v.C1_CallMemo() =
        let v = v :?> VertexMCall
        let call = v.Vertex.GetPureCall().Value
       
        let dop, mop = v.Flow.d_st.Expr, v.Flow.mop.Expr
        
        let sets = 
            (
                call.StartPointExpr
                <||> (dop <&&> v.ST.Expr)
                <||> (mop <&&> v.SF.Expr)
            )
            <&&> call.SafetyExpr
            
        let rst =
            if call.UsingTon 
            then
                (v.TDON.DN.Expr  <&&> dop)
                            <||>
                (call.End <&&> mop)
            else
                (call.EndPlan <&&> v._sim.Expr)
                            <||>
                (call.End <&&> !@v._sim.Expr)


        let parentReal = call.Parent.GetCore() :?> Vertex
        let rsts = rst <||> !@call.V.Flow.r_st.Expr <||> parentReal.VR.RT.Expr

        
        if call.Disabled 
        then
            (v._off.Expr, rsts) ==| (v.MM, getFuncName())
        else
            (sets, rsts) ==| (v.MM, getFuncName())


    member v.D1_DAGHeadStart() =
        let real = v.Vertex :?> Real
        let v = v :?> VertexMReal
        let coins = real.Graph.Inits.Select(getVM)
        [
            for coin in coins do
                let safety = coin.Vertex.GetPureCall().Value.SafetyExpr
                let coin = coin :?> VertexMCall
                let sets = v.RR.Expr <&&>  v.G.Expr <&&> safety
                let rsts = coin.ET.Expr <||> coin.RT.Expr 
                yield (sets, rsts) ==| (coin.ST, getFuncName())
        ]

    member v.D2_DAGTailStart() =
        let real = v.Vertex :?> Real
        let coins = real.Graph.Vertices.Except(real.Graph.Inits).Select(getVM)
        [
            for coin in coins do
                let safety = coin.Vertex.GetPureCall().Value.SafetyExpr
                let coin = coin :?> VertexMCall
                let sets = coin.Vertex.GetStartDAGAndCausals()  <&&>  v.G.Expr <&&> safety
                let rsts = coin.ET.Expr <||> coin.RT.Expr  
                yield (sets, rsts) ==| (coin.ST, getFuncName() )
        ]
        
    member v.D3_DAGCoinEnd() =
        let real = v.Vertex :?> Real
        let children = real.Graph.Vertices.Select(getVM)
        [
            for child in children do
                let coin = child :?> VertexMCall
                let call = coin.Vertex.GetPure().V.Vertex :?> Call
                let setEnd = call.PEs.ToAndElseOn() <&&> call.End

                let sets = 
                    if call.Disabled then 
                        coin.ST.Expr <&&> real.V.G.Expr
                    else
                        coin.ST.Expr <&&> setEnd <&&> real.V.G.Expr

                let rsts = coin.RT.Expr
                yield (sets, rsts) ==| (coin.ET, getFuncName() )
        ]


    member v.D4_DAGCoinReset() =
        let real = v.Vertex :?> Real
        let children = real.Graph.Vertices.Select(getVM)
        [
            for child in children do
                let child = child :?> VertexMCall
                let sets = real.V.RT.Expr // <&&> !@real.V.G.Expr
                let rsts = child.R.Expr
                yield (sets, rsts) ==| (child.RT, getFuncName() )
        ]


       
    




    