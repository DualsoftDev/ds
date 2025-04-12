[<AutoOpen>]
module Engine.CodeGenCPU.ConvertReal

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS
open Engine.Common
open System.Collections.Generic

type RealVertexTagManager with

    member v.R1_RealInitialStart() =
        let set = v.G.Expr <&&> v.OG.Expr
        let rst = v.RT.Expr

        (set, rst) ==| (v.RR, getFuncName())

    member v.RealEndActive(): CommentedStatement [] =
        let real = v.Vertex :?> Real
        let fn = getFuncName()
        [|
            let set = 
                let endExpr =  
                    v.GG.Expr
                    <&&> real.CoinETContacts.ToAndElseOn() 
                    <&&> if v.Real.Script.IsSome then   v.ScriptRelay.Expr else v._on.Expr
                    <&&> if v.Real.Time.IsSome   then   v.TimeRelay.Expr   else v._on.Expr
                    <&&> if v.Real.Motion.IsSome then   v.MotionRelay.Expr else v._on.Expr

                let forceOn = v.ONP.Expr <&&> v.Flow.mop.Expr
                endExpr <||> forceOn

            let rst = 
                if real.Graph.Vertices.Any() then
                    v.RT.Expr <&&> real.CoinAlloffExpr  
                else
                    v.RT.Expr 

            //수식 순서 중요 1.ET -> 2.GG (바뀌면 full scan Step제어 안됨)
            //1. EndTag 
            (set, rst) ==| (v.ET, fn)              
            //2. 다른 Real Reset Tag Relay을 위한 1Scan 소비 (Scan에서 제어방식 바뀌면 H/S 필요)
            (v.G.Expr, v._off.Expr) --| (v.GG, fn) 
        |]

    member v.RealEndPassive(): CommentedStatement [] =
        let real = v.Vertex :?> Real
        let fn = getFuncName()
        [|
            let setNormal = real.CoinETContacts.ToAndElseOn() 
            let initSrcs = real.Graph.HeadConnectedOrSingleVertex
            let dict = Dictionary<string, PlanVar<bool>>()
            for coin in initSrcs do
                let tempRising = v.System.GetTempBoolTag("tempCallOut")
                dict.Add(coin.QualifiedName, tempRising) |>ignore
                yield! (coin.VC.CallOut.Expr, v.System)  --^ (tempRising, fn) 
                
            let setOrExpr = //한번끝난 시작동전이 하나라도 한번더 동작시작하면 강제 RR
                initSrcs.OfType<Call>().Select(fun coin -> 
                        dict[coin.QualifiedName].Expr <&&> coin.V.F.Expr)
                        .ToOrElseOff()


            let rst = 
                if real.Graph.Vertices.Any() then
                    v.RT.Expr <&&> real.CoinAlloffExpr  
                else
                    v.RT.Expr

            (setOrExpr, rst) ==| (real.VR.RR, fn)              
            (setNormal(* <||> setOrExpr*), rst) ==| (v.ET, fn)       //setOrExpr  2번시작시 자동END       
        |]
        
    member v.R3_RealStartPoint() =
        let set = (v.G.Expr <&&> !@v.RR.Expr<&&> v.Link.Expr)
        let rst = v._off.Expr

        (set, rst) --| (v.RO, getFuncName())   


    member v.R4_RealLink() =
        let real = v.Vertex :?> Real
        let set =
            real.Graph.Vertices
                .OfType<Call>()
                .Where(fun call -> call.IsJob)
                .SelectMany(fun call -> call.ApiDefs)
                .Select(fun api-> api.SensorLinked).ToAndElseOn()

        let rst = v._off.Expr
        (set, rst) --| (v.Link, getFuncName())
      
    member v.R5_DummyDAGCoils() =
        let fn = getFuncName()
        let real = v.Vertex :?> Real
        let rst = v._off.Expr
        if real.Graph.Vertices.Any()
        then
            [|
                (real.CoinSTContacts.ToOrElseOff(), rst) --| (v.CoinAnyOnST, fn)     // S
                (real.CoinRTContacts.ToOrElseOff(), rst) --| (v.CoinAnyOnRT, fn)     // R
                (real.CoinETContacts.ToOrElseOff(), rst) --| (v.CoinAnyOnET, fn)     // E
            |]
        else
            [||]


    member v.R6_SourceTokenNumGeneration() =
        let vr = v.Vertex.VR
        let fn = getFuncName()
        match v.Vertex.TokenSourceOrder with
        | Some order ->
            [|
                let tempInit= v.System.GetTempBoolTag("tempInitCheckTokenSrc")
                let initExpr = 0u |> literal2expr ==@ vr.SourceTokenData.ToExpression()
                yield (initExpr, v._off.Expr) --| (tempInit, fn)

                let order = order |> uint32 |> literal2expr
                let totalSrcToken = v.System.GetSourceTokenCount() |> uint32 |>literal2expr

                yield (tempInit.Expr   <&&> v.ET.Expr, order) --> (vr.SourceTokenData, fn)
                yield (tempInit.Expr   <&&> v.ET.Expr, order) --> (vr.RealTokenData, fn)
                yield (!@tempInit.Expr <&&> v.GP.Expr, totalSrcToken, vr.SourceTokenData.ToExpression()) --+ (vr.SourceTokenData, fn)
                yield (!@tempInit.Expr <&&> v.GP.Expr, vr.SourceTokenData.ToExpression()) --> (vr.RealTokenData, fn)
            |]
        |None -> [||]


    //member v.R6_RealTokenMoveNSink() = 
    //    let fn = getFuncName()
    //    let srcs = getStartEdgeSources(v.Vertex)
    //    let srcReals = srcs.GetPureReals().ToArray()
    //    let sinkReals =srcReals.Where(fun w-> w.NoTransData)
    //    let moveReals =srcReals.Where(fun w-> not(w.NoTransData))      
    //    let causalCalls = srcs.GetPureCalls()
    //    [| 

    //        //(* 자신의 alias가 소스로 사용될경우 토큰 전송*)
    //        //let usedAliasSet = v.SysAliasFlowEdgeSet
    //        //                        .Where(fun w-> w.EdgeType.HasFlag(EdgeType.Start))
    //        //                        .Where(fun w-> w.Source :? Alias)
    //        //                        .Where(fun w-> w.Source.GetPure() = v.Vertex)
    //        //                        .Where(fun w-> not(v.Real.NoTransData))

    //        //for edge in usedAliasSet do
    //        //    let srcAliasSEQ = v.RealTokenData
    //        //    let tgtAliasSEQ = edge.Target.GetPureReal().V.RealTokenData
    //        //    yield (v.R.Expr, srcAliasSEQ.ToExpression()) --> (tgtAliasSEQ, fn) 


    //        (* 자신이 타겟으로로 사용될경우 토큰 받기*)
    //        let tgtTagSEQ = v.RealTokenData
    //        let srcTagSEQ = 
    //            match moveReals.any(), causalCalls.any() with    // Call SourceTokenData 가 우선
    //            | _, true -> 
    //                causalCalls.First().VC.SourceTokenData  |>Some
    //            | true, false -> 
    //                moveReals.First().VR.RealTokenData  |>Some
    //            | false, false -> 
    //                None

    //        if srcTagSEQ.IsSome
    //        then 
    //            yield (v.R.Expr, srcTagSEQ.Value.ToExpression()) --> (tgtTagSEQ, fn) 
    //            for sinkReal in sinkReals do
    //                yield (v.R.Expr, srcTagSEQ.Value.ToExpression()) --> (sinkReal.VR.MergeTokenData, fn) 
    //    |]



    member v.R7_RealGoingOriginError() =
        let fn = getFuncName()
        let dop = v.Flow.d_st.Expr
        let rst = v.Flow.ClearExpr
        let checking = v.G.Expr <&&> !@v.OG.Expr <&&> !@v.RR.Expr <&&> dop
        (checking, rst) ==| (v.ErrGoingOrigin , fn)
        
    member v.R8_RealGoingPulse(): CommentedStatement [] =
        let fn = getFuncName()
        [|
            yield! (v.G.Expr, v.System)  --^ (v.GP, fn) 
        |]

   