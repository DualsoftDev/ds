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
        let callMutualOns = call.MutualResetCoins
                                .Choose(tryGetPureCall)
                                .Choose(fun c->c.EndAction).ToOrElseOff()
        
        let iop = call.V.Flow.iop.Expr
        let rst = v.Flow.ClearExpr
        let running = (v.PS.Expr(* <||> call.End*)) <&&> !@iop

        [|
            yield running --@ (v.TimeMax, call.TimeOutMaxMSec, fn)

            (*TimeMax*)
            yield (v.TimeMax.DN.Expr <&&> !@call.End, rst) ==| (v.ErrOnTimeOver ,fn)
            (*TimeMaxOff*)
            yield (v.TimeMax.DN.Expr <&&> callMutualOns, rst) ==| (v.ErrOffTimeOver , fn)
        |]

    //member v.E2_CallErrTimeUnder() = //  시간 미달 DS Operator 정의로 해결

    member v.E2_CallErrRXMonitor() =
        let call  = v.Vertex.GetPure() :?> Call
        let real  = call.Parent.GetCore() :?> Real
        let v = v:?> CoinVertexTagManager
        
        let dop = call.V.Flow.d_st.Expr
        let rst = v.Flow.ClearExpr
        let fn = getFuncName()

        [|
            let using      = if call.HasSensor && not(call.HasAnalogSensor) then v._on.Expr else  v._off.Expr 
            let input      = call.EndWithoutTimer
            let checkCondi = using <&&> dop <&&> real.V.G.Expr 

            let rxReadyExpr  =  call.RXs.Select(fun f -> f.V.R).ToAndElseOff()
            let rxFinishExpr =  call.RXs.Select(fun f -> f.V.F).ToAndElseOff()

            let errShortRising = v.System.GetTempBoolTag($"{call.QualifiedName}errShortRising")
            let errOpenRising = v.System.GetTempBoolTag($"{call.QualifiedName}errOpenRising")
            yield! (input, v.System) --^ (errShortRising, fn)
            yield! (!@input, v.System) --^ (errOpenRising, fn)
            
            (* short error *)
            yield (checkCondi <&&> rxReadyExpr <&&> errShortRising.Expr,  rst)  ==| (v.ErrShort, fn)
            (* open  error *)
            if call.UsingTimeDelayCheck then
                yield (checkCondi <&&> rxFinishExpr <&&> !@call.V.G.Expr <&&> errOpenRising.Expr, rst)  ==| (v.ErrOpen, fn)
            else
                yield (checkCondi <&&> rxFinishExpr                      <&&> errOpenRising.Expr, rst)  ==| (v.ErrOpen, fn)
        |]
        
    //인터락 동시 에러 추가 전진시 다른센서해제안됨에러
    member v.E3_CallErrRXInterlockMonitor() =
        let v= v :?> CoinVertexTagManager
        let fn = getFuncName()
        let call= v.Vertex.GetPure() :?> Call
        let callMutualOns = call.MutualResetCoins
                                .Choose(tryGetPureCall)
                                .Choose(fun c->c.EndAction)
                                .Distinct()
                                .ToOrElseOff()
        
        let iop = call.V.Flow.iop.Expr
        let rst = v.Flow.ClearExpr

        match call.EndAction with
        | Some input ->
            let errRXInterlock = v.System.GetTempBoolTag($"{call.QualifiedName}errRXInterlock")
            [|
                yield! (input <&&> !@iop, v.System) --^ (errRXInterlock, fn)
                yield (errRXInterlock.Expr <&&> callMutualOns , rst) ==| (v.ErrInterlock , fn)
            |]
        | _ -> [||]

    member v.E4_CallErrTotalMonitor() =
        let v= v :?> CoinVertexTagManager
        let call= v.Vertex.GetPure() :?> Call
        (call.Errors.ToOrElseOff() , v._off.Expr) --| (v.ErrTRX, getFuncName())

    member v.E5_RealErrTotalMonitor() =
        let real = v.Vertex :?> Real
        let rst = v._off.Expr
         
        (real.Errors.ToOrElseOff(), rst) --| (v.ErrTRX, getFuncName())
