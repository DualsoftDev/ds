[<AutoOpen>]
module Engine.CodeGenCPU.ConvertDAG

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type VertexTagManager with
   
    member v.CoinStartActive() =
        let real = v.Vertex :?> Real
        let v = v :?> RealVertexTagManager
        let coinHeads = real.Graph.Inits.Select(getVMCall)
        let coinTails = real.Graph.Vertices.Except(real.Graph.Inits).Select(getVMCall)
        let f = getFuncName()
        [|
            let start = !@real.V.ErrWork.Expr <&&> v.G.Expr <&&> v.Flow.aop.Expr
            for coin in coinHeads@coinTails do
                let sets = if coinHeads.Contains coin 
                           then  start<&&>  v.RR.Expr 
                           else  start <&&> coin.Vertex.GetStartDAGAndCausals() 
                let rsts = coin.ET.Expr <||> coin.RT.Expr 
                yield (sets, rsts) ==| (coin.ST, f)
        |]
        
    member v.CoinStartPassive() =
        let real = v.Vertex :?> Real
        let coins = real.Graph.Vertices.Select(getVMCall)
        let f = getFuncName()
        let initSrcs = real.Graph.HeadConnectedVertices
        [|
            for coin in coins do
                let sets =
                    if initSrcs.Contains coin.Vertex then
                        coin.CallOut.Expr  
                    else
                        coin.CallOut.Expr <&&> initSrcs.Select(fun f->f.VC.ET).ToAndElseOn()

                let rsts = coin.ET.Expr <||> coin.RT.Expr 

                yield (sets <&&> real.VR.G.Expr , rsts) ==| (coin.ST, f)
        |]
        
    member v.CoinEndActive() =
        let real = v.Vertex :?> Real
        let coins = real.Graph.Vertices.Select(getVMCall)
        let f = getFuncName()
        [|
            for coin in coins do
                let call = coin.Vertex.GetPure().V.Vertex :?> Call
                let rsts = coin.RT.Expr
                if call.Disabled then 
                    yield (coin.ST.Expr <&&> real.V.G.Expr, rsts) ==| (coin.ET, f )
                else 

                    let setStart = coin.ST.Expr <&&> real.V.G.Expr
                    let setEnd =  call.End <&&> !@real.V.ErrWork.Expr 

                    //무조건 센서 맞으면 기다리지않기 Coin 뒤집기
                    yield (setStart<&&>setEnd, rsts) ==| (coin.ET, f )
        |]
        
            ////아날로그 전용 job 은 기다리지 않고 값 성립하면 Coin 뒤집기
        //if call.IsAnalog then
        //    yield (setStart<&&>setEnd, rsts) ==| (coin.ET, f )
        //else
        //    yield! (setEnd, coin.System)  --^ (coin.GP, f) 
        //    yield (setStart <&&> coin.GP.Expr, rsts) ==| (coin.ET, f )


    member v.CoinEndPassive() =
        let real = v.Vertex :?> Real
        let coins = real.Graph.Vertices.Select(getVMCall)
        let f = getFuncName()
        [|
            for coin in coins do
                let call = coin.Vertex.GetPure().V.Vertex :?> Call
                let rsts = coin.RT.Expr
                let sets = coin.ST.Expr <&&> real.VR.G.Expr

                if call.HasSensor || call.ActionOutExpr.IsNone then
                    yield (sets <&&> call.End, rsts) ==| (coin.ET, f)
                else 
                    let tempRising  = getSM(call.System).GetTempBoolTag("tempFallingOutput")
                    yield! (!@call.ActionOutExpr.Value, call.System) --^ (tempRising, f)
                    yield (sets <&&> tempRising.Expr, rsts) ==| (coin.ET, f)
        |]


    member v.D3_CoinReset() =
        let real = v.Vertex :?> Real
        let children = real.Graph.Vertices.Select(getVMCall)
        let f = getFuncName()
        [|
            for child in children do
                let sets = real.V.RT.Expr // <&&> !@real.V.G.Expr
                let rsts = child.R.Expr
                yield (sets, rsts) ==| (child.RT, f )
        |]








