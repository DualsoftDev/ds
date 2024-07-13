[<AutoOpen>]
module Engine.CodeGenCPU.ConvertReal

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type VertexMReal with

    member v.R1_RealInitialStart() =
        let set = v.G.Expr <&&> v.OG.Expr
        let rst = v.RT.Expr

        (set, rst) ==| (v.RR, getFuncName())

    member v.R2_RealJobComplete(): CommentedStatement list =
        let real = v.Vertex :?> Real
        [   
            let set = 
                let endExpr =  
                    v.GG.Expr
                    <&&> real.CoinETContacts.ToAndElseOn() 
                    <&&> if v.Real.Script.IsSome then   v.ScriptRelay.Expr else v._on.Expr
                    <&&> if v.Real.TimeAvg.IsSome then  v.TimeRelay.Expr   else v._on.Expr
                    <&&> if v.Real.Motion.IsSome then   v.MotionRelay.Expr else v._on.Expr


                if v.IsFinished && (RuntimeDS.Package.IsPackageSIM())
                then
                    endExpr <||> v.ON.Expr <||> !@v.Link.Expr
                else                          
                    endExpr <||> v.ON.Expr 

            let rst = 
                if real.Graph.Vertices.any()
                then v.RT.Expr <&&> real.CoinAlloffExpr  
                else v.RT.Expr 

            //수식 순서 중요 1.ET -> 2.GG (바뀌면 full scan Step제어 안됨)
            //1. EndTag 
            (set, rst) ==| (v.ET, getFuncName())              
            //2. 다른 Real Reset Tag Relay을 위한 1Scan 소비 (Scan에서 제어방식 바뀌면 H/S 필요)
            (v.G.Expr, v._off.Expr) --| (v.GG, getFuncName()) 
        ]


        
    member v.R3_RealStartPoint() =
        let set = (v.G.Expr <&&> !@v.RR.Expr<&&> v.Link.Expr)
        let rst = v._off.Expr

        (set, rst) --| (v.RO, getFuncName())   


    member v.R4_RealLink() =
        let real = v.Vertex :?> Real
        let set = real.Graph.Vertices.OfType<Call>()
                      .Where(fun call -> call.IsJob)
                      .SelectMany(fun call -> call.TargetJob.ApiDefs)
                      .Select(fun api-> api.SL2).ToAndElseOn()

        let rst = v._off.Expr
        (set, rst) --| (v.Link, getFuncName())
      
    member v.R5_DummyDAGCoils() =
        let real = v.Vertex :?> Real
        let rst = v._off.Expr
        [
            (real.CoinSTContacts.ToOrElseOff(), rst) --| (v.CoinAnyOnST, getFuncName())     // S
            (real.CoinRTContacts.ToOrElseOff(), rst) --| (v.CoinAnyOnRT, getFuncName())     // R
            (real.CoinETContacts.ToOrElseOff(), rst) --| (v.CoinAnyOnET, getFuncName())     // E
        ]

    member v.R6_RealDataMove() = ()
        //let set = v.RD.ToExpression() 
        //(set) --> (v.RD, getFuncName())

    member v.R7_RealGoingOriginError() =
        let dop = v.Flow.d_st.Expr
        let rst = v.Flow.ClearExpr
        [
            if not(RuntimeDS.Package.IsPackageSIM())
            then 
                let checking = v.G.Expr <&&> !@v.OG.Expr <&&> !@v.RR.Expr <&&> dop
                yield (checking, rst) ==| (v.ErrGoingOrigin , getFuncName())
        ]
        
    member v.R8_RealGoingPulse(): CommentedStatement  list =
        [ 
            if RuntimeDS.Package.IsPLCorPLCSIM() 
            then
                yield(fbRising[v.G.Expr], v._off.Expr) --| (v.GP, getFuncName())
            elif RuntimeDS.Package.IsPCorPCSIM() then 
                yield! (v.G.Expr, v.GPR, v.GPH)  --^ (v.GP, getFuncName()) 
            else    
                failWithLog $"Not supported {RuntimeDS.Package} package"
        ]

    member v.R10_RealGoingTime(): CommentedStatement  list =
        [
            if v.Real.TimeAvg.IsSome then
                yield (v.TimeStart.Expr<&&>v.TimeEnd.Expr,  v.ET.Expr) ==| (v.TimeRelay, getFuncName())
                yield (v.G.Expr,  v.TimeRelay.Expr) --| (v.TimeStart, getFuncName())
                
                if RuntimeDS.Package.IsPackageSIM() 
                then
                    if RuntimeDS.RuntimeMotionMode = MotionAsync then
                        yield (v.TimeStart.Expr) --@ (v.TRealOnTime, v.Real.TimeAvgMsec, getFuncName())
                        yield (v.TRealOnTime.DN.Expr,  v._off.Expr) --| (v.TimeEnd, getFuncName())
                        
                else 
                    yield (v.TimeStart.Expr,  v._off.Expr) --| (v.TimeEnd, getFuncName())
                    
        ]

    member v.R11_RealGoingMotion(): CommentedStatement  list =
        [
            if v.Real.Motion.IsSome then
                yield (v.MotionStart.Expr <&&> v.MotionEnd.Expr,  v.ET.Expr) ==| (v.MotionRelay, getFuncName())
                yield (v.G.Expr,  v.MotionEnd.Expr <||>  v.MotionRelay.Expr) --| (v.MotionStart, getFuncName())

                if RuntimeDS.Package.IsPackageSIM() 
                then
                    if RuntimeDS.RuntimeMotionMode = MotionAsync
                    then
                        if v.Real.TimeAvg.IsSome
                        then
                            yield (v.TimeEnd.Expr    , v._off.Expr) --| (v.MotionEnd, getFuncName())   
                        else 
                            yield (v.MotionStart.Expr, v._off.Expr) --| (v.MotionEnd, getFuncName())   

                    elif RuntimeDS.RuntimeMotionMode = MotionSync 
                    then
                        if v.Real.TimeAvg.IsSome
                        then
                            yield (v.MotionEnd.Expr    , v._off.Expr) --| (v.TimeEnd, getFuncName())   
                    else 
                        failwithlog $"RuntimeMotionMode err : {RuntimeDS.RuntimeMotionMode}"
                else
                    let realSensor  = v.Real.ParentApiSensorExpr
                    if realSensor.IsNull()
                    then yield (v.G.Expr, v._off.Expr) --| (v.MotionEnd, getFuncName())      //실제 rx에 해당하는 하지 않으면 action 안보고 going 후 바로 MotionEnd
                    else yield (realSensor, v._off.Expr) --| (v.MotionEnd, getFuncName())    //실제 rx에 해당하는 하면 api 실 action sensor
                    
        ]

    member v.R12_RealGoingScript(): CommentedStatement  list =
        [
            if v.Real.Script.IsSome then
                yield (v.ScriptStart.Expr<&&>v.ScriptEnd.Expr,  v.ET.Expr) ==| (v.ScriptRelay, getFuncName())
                yield (v.G.Expr,  v.ScriptRelay.Expr) --| (v.ScriptStart, getFuncName())  
        ]
