[<AutoOpen>]
module Engine.CodeGenCPU.ConvertRealTimeMotionScript

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS
open Engine.Common


let getTimeStatement(v:RealVertexTagManager) =
        [|
            yield (v.TimeStart.Expr) --@ (v.TRealOnTime, v.Real.TimeSimMsec, getFuncName())
            yield (v.TRealOnTime.DN.Expr, v._off.Expr) --| (v.TimeEnd, getFuncName())
        |]

type RealVertexTagManager with

        
    member v.RealGoingTimeSimulation(): CommentedStatement [] =
        let fn = getFuncName()
        if v.Real.Time.IsNone then [||] 
        else
            [|
                yield (v.TimeStart.Expr<&&>v.TimeEnd.Expr, v.ET.Expr) ==| (v.TimeRelay, fn)
                yield (v.G.Expr, v.TimeRelay.Expr) --| (v.TimeStart, fn)
                yield! getTimeStatement(v) 
            |]

    member v.RealGoingTime(): CommentedStatement [] =
        let fn = getFuncName()
        if v.Real.Time.IsNone then [||] 
        else
            [|
                yield (v.TimeStart.Expr<&&>v.TimeEnd.Expr, v.ET.Expr) ==| (v.TimeRelay, fn)
                yield (v.G.Expr, v.TimeRelay.Expr) --| (v.TimeStart, fn)
                
                if v.Real.Motion.IsSome then
                    yield (v.MotionEnd.Expr  , v._off.Expr) --| (v.TimeEnd, fn)   //3D 사용하면   시간도 모션에 의해서 끝남
                else 
                    yield! getTimeStatement(v) 
            |]



    member v.R10_GoingTime(mode:RuntimePackage): CommentedStatement [] =
        match mode with 
        | Simulation -> v.RealGoingTimeSimulation()
        | VirtualLogic 
        | VirtualPlant 
        | Control 
        | Monitoring -> v.RealGoingTime()


    member private v.R11_RealGoingMotion(): CommentedStatement [] =
        let fn = getFuncName()
        [|
            if v.Real.Motion.IsSome then
                yield (v.MotionStart.Expr <&&> v.MotionEnd.Expr,   v.F.Expr) ==| (v.MotionRelay, fn)
                yield (v.G.Expr,  (*v.MotionEnd.Expr <||>*)  v.MotionRelay.Expr) --| (v.MotionStart, fn)
                let realSensor  = v.Real.ParentApiSensorExpr
                if realSensor.IsNull() then
                    yield (v.G.Expr, v._off.Expr) --| (v.MotionEnd, fn)      //실제 rx에 해당하는 하지 않으면 action 안보고 going 후 바로 MotionEnd
                else
                    yield (realSensor, v._off.Expr) --| (v.MotionEnd, fn)    //실제 rx에 해당하는 하면 api 실 action sensor
                    
        |]

    member private v.RealGoingMotionSimulation(): CommentedStatement [] =
        let fn = getFuncName()
        [|
            if v.Real.Motion.IsSome then
                yield (v.MotionStart.Expr <&&> v.MotionEnd.Expr,   v.F.Expr) ==| (v.MotionRelay, fn)
                yield (v.G.Expr,  (*v.MotionEnd.Expr <||>*)  v.MotionRelay.Expr) --| (v.MotionStart, fn)
                if v.Real.TimeAvg.IsSome then
                    yield (v.TimeEnd.Expr    , v.R.Expr) ==| (v.MotionEnd, fn)   
                else 
                    yield (v.MotionStart.Expr, v.R.Expr) ==| (v.MotionEnd, fn)   
                    
        |]  
    member private v.RealGoingMotionVirtualLogic(): CommentedStatement [] =
        let fn = getFuncName()
        [|
            if v.Real.Motion.IsSome then
                yield (v.MotionStart.Expr <&&> v.MotionEnd.Expr,   v.F.Expr) ==| (v.MotionRelay, fn)
                yield (v.G.Expr,  (*v.MotionEnd.Expr <||>*)  v.MotionRelay.Expr) --| (v.MotionStart, fn)
        |]

            
    member v.R11_GoingMotion(mode:RuntimePackage): CommentedStatement [] = 
        match mode with
        | Simulation -> v.RealGoingMotionSimulation()
        | VirtualLogic -> v.RealGoingMotionVirtualLogic()
        | VirtualPlant 
        | Control
        | Monitoring -> v.R11_RealGoingMotion()
        

    member v.R12_RealGoingScript(): CommentedStatement [] =
        let fn = getFuncName()
        [|
            if v.Real.Script.IsSome then
                yield (v.ScriptStart.Expr<&&>v.ScriptEnd.Expr, v.ET.Expr) ==| (v.ScriptRelay, fn)
                yield (v.G.Expr, v.ScriptRelay.Expr) --| (v.ScriptStart, fn)  
        |]
