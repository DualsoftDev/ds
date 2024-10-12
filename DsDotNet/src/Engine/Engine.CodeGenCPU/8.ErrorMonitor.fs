[<AutoOpen>]
module Engine.CodeGenCPU.ConvertErrorMonitor

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type VertexTagManager with

    member v.E1_CallErrTimeOver() =
        let v= v :?> CoinVertexTagManager
        let fn = getFuncName()
        let call= v.Vertex.GetPure() :?> Call
        let callMutualOns = call.MutualResetCoins.Choose(tryGetPureCall)
                                .Select(fun c->getJM(c.TargetJob).InDetected).ToOrElseOff()
        
        let iop = call.V.Flow.iop.Expr
        let rst = v.Flow.ClearExpr
        let runningMaxOn  = v.MM.Expr <&&> !@iop
        let runningMaxOff = v.MM.Expr <&&> !@iop <&&> !@callMutualOns 

        [|
            (*TimeMaxOn*)
            yield runningMaxOn --@ (v.TimeMaxOn, call.TimeMaxOnMSec, fn)
            yield (v.TimeMaxOn.DN.Expr, rst) ==| (v.ErrOnTimeOver ,fn)

            (*TimeMaxOff*)
            yield runningMaxOff --@ (v.TimeMaxOff, call.TimeMaxOffMSec, fn)
            yield (v.TimeMaxOff.DN.Expr, rst) ==| (v.ErrOffTimeOver , fn)
        |]



    member v.E2_CallErrTimeUnder() =
        let v= v :?> CoinVertexTagManager
        let call= v.Vertex.GetPure() :?> Call
        let callMutualOns = call.MutualResetCoins.Choose(tryGetPureCall)
                                .Select(fun c->getJM(c.TargetJob).InDetected).ToOrElseOff()
        
        let iop = call.V.Flow.iop.Expr
        let rst = v.Flow.ClearExpr
        let running = (v.MM.Expr <||> call.End) <&&> !@iop

        [|
            (*TimeMinOnMSec*)
            if call.TimeMinOnMSec <> 0u 
            then
                yield running --@ (v.TimeMinOn, call.TimeMinOnMSec, getFuncName())
                yield (!@v.TimeMinOn.DN.Expr <&&> v.G.Expr <&&> v.ET.Expr, rst) ==| (v.ErrOnTimeUnder ,getFuncName())

            (*TimeMinOffMSec*)
            if call.TimeMinOffMSec <> 0u 
            then
                yield running --@ (v.TimeMinOff, call.TimeMinOffMSec, getFuncName())
                yield (!@v.TimeMinOff.DN.Expr <&&> v.G.Expr <&&> !@callMutualOns , rst) ==| (v.ErrOffTimeUnder ,getFuncName())
        |]



    member v.E3_CallErrRXMonitor() =
        let call  = v.Vertex.GetPure() :?> Call
        let real  = call.Parent.GetCore() :?> Real
        let v = v:?> CoinVertexTagManager
        
        let dop = call.V.Flow.d_st.Expr
        let rst = v.Flow.ClearExpr
        let fn = getFuncName()

        [|
            let using      = if call.HasSensor && not(call.HasAnalogSensor) then v._on.Expr else  v._off.Expr 
            let input      = call.End
            let checkCondi = using <&&> dop <&&> real.V.G.Expr 

            let rxReadyExpr  =  call.RXs.Select(fun f -> f.V.R).ToAndElseOff()
            let rxFinishExpr =  call.RXs.Select(fun f -> f.V.F).ToAndElseOff()

            let errShortRising = v.System.GetTempBoolTag($"{call.QualifiedName}errShortRising")
            let errOpenRising = v.System.GetTempBoolTag($"{call.QualifiedName}errOpenRising")
            yield! (input, v.System) --^ (errShortRising, fn)
            yield! (!@input, v.System) --^ (errOpenRising,  fn)
            
            (* short error *)
            yield (checkCondi <&&>  rxReadyExpr <&&> errShortRising.Expr,  rst)  ==| (v.ErrShort, fn)
            (* open  error *)
            if call.UsingTon then
                yield (checkCondi <&&> rxFinishExpr <&&> !@call.V.G.Expr <&&> errOpenRising.Expr, rst)  ==| (v.ErrOpen, fn)
            else
                yield (checkCondi <&&> rxFinishExpr                      <&&> errOpenRising.Expr, rst)  ==| (v.ErrOpen, fn)
        |]
        


    member v.E4_RealErrTotalMonitor() =
        let real = v.Vertex :?> Real
        let rst = v._off.Expr
         
        (real.Errors.ToOrElseOff(), rst) --| (v.ErrTRX, getFuncName())

   
    member v.E5_CallErrTotalMonitor() =
        let v= v :?> CoinVertexTagManager
        let call= v.Vertex.GetPure() :?> Call
        (call.Errors.ToOrElseOff() , v._off.Expr) --| (v.ErrTRX, getFuncName())


