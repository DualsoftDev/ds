[<AutoOpen>]
module Engine.CodeGenCPU.ConvertReal

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS
open Engine.Common

type RealVertexTagManager with

    member v.R1_RealInitialStart() =
        let set = v.G.Expr <&&> v.OG.Expr
        let rst = v.RT.Expr

        (set, rst) ==| (v.RR, getFuncName())

    member v.R2_RealJobComplete(): CommentedStatement [] =
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

                //if RuntimeDS.ModelConfig.RuntimePackage.IsPLCorPLCSIM()
                //then
                //    //처음에는 자기 순서로 시작
                //    yield (tempInit.Expr <&&> fbRising[v.ET.Expr], order) --> (vr.SourceTokenData, fn)
                //    yield (tempInit.Expr <&&> fbRising[v.ET.Expr], order) --> (vr.RealTokenData, fn)
                //    //이후부터는 전체 값 만큼 증가
                //    yield (!@tempInit.Expr <&&> fbRising[v.GP.Expr], totalSrcToken, vr.SourceTokenData.ToExpression()) --+ (vr.SourceTokenData, fn)
                //    yield (!@tempInit.Expr <&&> fbRising[v.GP.Expr], vr.SourceTokenData.ToExpression()) --> (vr.RealTokenData, fn)
                //else
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
        [|
            if not(RuntimeDS.ModelConfig.RuntimePackage.IsPackageSIM()) then 
                let checking = v.G.Expr <&&> !@v.OG.Expr <&&> !@v.RR.Expr <&&> dop
                yield (checking, rst) ==| (v.ErrGoingOrigin , fn)
        |]
        
    member v.R8_RealGoingPulse(): CommentedStatement [] =
        let fn = getFuncName()
        [|
            yield! (v.G.Expr, v.System)  --^ (v.GP, fn) 
        |]

    member v.R10_RealGoingTime(): CommentedStatement [] =
        let fn = getFuncName()
        [|
            let getTimeStatement() =
                [|  yield (v.TimeStart.Expr) --@ (v.TRealOnTime, v.Real.TimeSimMsec, fn)
                    yield (v.TRealOnTime.DN.Expr, v._off.Expr) --| (v.TimeEnd, fn)     |]
                   
            if v.Real.Time.IsSome then
                
                yield (v.TimeStart.Expr<&&>v.TimeEnd.Expr, v.ET.Expr) ==| (v.TimeRelay, fn)
                yield (v.G.Expr, v.TimeRelay.Expr) --| (v.TimeStart, fn)
                
                if RuntimeDS.ModelConfig.RuntimePackage.IsPackageSIM() && v.Real.TimeAvgExist  then
                    if RuntimeDS.ModelConfig.RuntimeMotionMode = MotionAsync then
                        yield! getTimeStatement() 
                    elif RuntimeDS.ModelConfig.RuntimeMotionMode = MotionSync then
                        if v.Real.Motion.IsSome then
                            yield (v.MotionEnd.Expr  , v._off.Expr) --| (v.TimeEnd, fn)   //3D 사용하면   시간도 모션에 의해서 끝남
                        else 
                            yield! getTimeStatement() 

                else 
                    yield (v.TimeStart.Expr, v._off.Expr) --| (v.TimeEnd, fn)
                    
        |]

    member v.R11_RealGoingMotion(): CommentedStatement [] =
        let fn = getFuncName()
        [|
            if v.Real.Motion.IsSome then
                yield (v.MotionStart.Expr <&&> v.MotionEnd.Expr,   v.F.Expr) ==| (v.MotionRelay, fn)
                yield (v.G.Expr,  v.MotionEnd.Expr <||>  v.MotionRelay.Expr) --| (v.MotionStart, fn)

                if RuntimeDS.ModelConfig.RuntimePackage.IsPackageSIM() then
                    if RuntimeDS.ModelConfig.RuntimeMotionMode = MotionAsync then
                        if v.Real.TimeAvg.IsSome then
                            yield (v.TimeEnd.Expr    , v.R.Expr) ==| (v.MotionEnd, fn)   
                        else 
                            yield (v.MotionStart.Expr, v.R.Expr) ==| (v.MotionEnd, fn)   

                else
                    let realSensor  = v.Real.ParentApiSensorExpr
                    if realSensor.IsNull() then
                        yield (v.G.Expr, v._off.Expr) --| (v.MotionEnd, fn)      //실제 rx에 해당하는 하지 않으면 action 안보고 going 후 바로 MotionEnd
                    else
                        yield (realSensor, v._off.Expr) --| (v.MotionEnd, fn)    //실제 rx에 해당하는 하면 api 실 action sensor
                    
        |]

    member v.R12_RealGoingScript(): CommentedStatement [] =
        let fn = getFuncName()
        [|
            if v.Real.Script.IsSome then
                yield (v.ScriptStart.Expr<&&>v.ScriptEnd.Expr, v.ET.Expr) ==| (v.ScriptRelay, fn)
                yield (v.G.Expr, v.ScriptRelay.Expr) --| (v.ScriptStart, fn)  
        |]
