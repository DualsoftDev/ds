[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type VertexTagManager with
    member v.C1_CallMemo() =
        let v = v :?> CallVertexTagManager
        let call = v.Vertex.GetPureCall()
       
        let dop, mop = v.Flow.d_st.Expr, v.Flow.mop.Expr
        
        let sets = 
            (
                call.StartPointExpr
                <||> (dop <&&> v.ST.Expr <&&> call.AutoPreExpr )
                <||> (mop <&&> v.SF.Expr)
            )
            <&&> call.SafetyExpr 
            
        let rst =
            if call.UsingTon then
                (v.TDON.DN.Expr <&&> dop) <||> (call.End <&&> mop)
            else
                call.End


        let parentReal = call.Parent.GetCore() :?> Vertex
        let rstMemos = call.MutualResetCoins.Select(fun c->c.VC.MM)
        
        let sets = sets <&&> !@rstMemos.ToOrElseOff()
        let rsts = rst <||> !@call.V.Flow.r_st.Expr <||> parentReal.VR.RT.Expr

        
        let f = getFuncName()
        if call.Disabled then
            (v._off.Expr, rsts) ==| (v.MM, f)
        else
            (sets, rsts) ==| (v.MM, f)


    member v.D1_DAGHeadStart() =
        let real = v.Vertex :?> Real
        let v = v :?> RealVertexTagManager
        let coins = real.Graph.Inits.Select(getVMCoin)
        [
            let f = getFuncName()
            for coin in coins do
                let call = coin.Vertex.TryGetPureCall().Value
                let safety = call.SafetyExpr
                let autoPreExpr = call.AutoPreExpr
                let sets = v.RR.Expr <&&>  v.G.Expr <&&> safety <&&> autoPreExpr
                let rsts = coin.ET.Expr <||> coin.RT.Expr 
                yield (sets, rsts) ==| (coin.ST, f)
        ]

    member v.D2_DAGTailStart() =
        let real = v.Vertex :?> Real
        let coins = real.Graph.Vertices.Except(real.Graph.Inits).Select(getVMCoin)
        [
            let f = getFuncName()
            for coin in coins do
                let call = coin.Vertex.TryGetPureCall().Value
                let safety = call.SafetyExpr
                let autoPreExpr = call.AutoPreExpr
                let sets = coin.Vertex.GetStartDAGAndCausals()  <&&>  v.G.Expr <&&> safety <&&> autoPreExpr
                let rsts = coin.ET.Expr <||> coin.RT.Expr  
                yield (sets, rsts) ==| (coin.ST, f )
        ]
        
    member v.D3_DAGCoinEnd() =
        let real = v.Vertex :?> Real
        let coins = real.Graph.Vertices.Select(getVMCoin)
        [
            let f = getFuncName()
            for coin in coins do
                let call = coin.Vertex.GetPure().V.Vertex :?> Call
                let rsts = coin.RT.Expr
                if call.Disabled then 
                    yield (coin.ST.Expr <&&> real.V.G.Expr, rsts) ==| (coin.ET, f )
                else 

                    let setStart = coin.ST.Expr <&&> real.V.G.Expr
                    let setEnd =  call.End

                     //아날로그 전용 job 은 기다리지 않고 값 성립하면 Coin 뒤집기
                    if call.IsAnalogOutput then
                        yield (setStart<&&>setEnd, rsts) ==| (coin.ET, f )
                    else
                        yield! (setEnd, coin.System)  --^ (coin.GP, f) 
                        yield (setStart <&&> coin.GP.Expr, rsts) ==| (coin.ET, f )
        ]


    member v.D4_DAGCoinReset() =
        let real = v.Vertex :?> Real
        let children = real.Graph.Vertices.Select(getVMCoin)
        [
            let f = getFuncName()
            for child in children do
                let sets = real.V.RT.Expr // <&&> !@real.V.G.Expr
                let rsts = child.R.Expr
                yield (sets, rsts) ==| (child.RT, f )
        ]


       
    




    