[<AutoOpen>]
module Engine.CodeGenCPU.ConvertErrorMonitor

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type VertexTagManager with

        //test ahn 구현 필요  onTimeOver ,  offTimeOver 2구현 필요 Job.JobTime.MAX 이용
    member v.E1_CallErrTimeOver() =
        let v= v :?> CoinVertexTagManager
        let call= v.Vertex.GetPure() :?> Call
        let vOff = v._off.Expr

        let iop = call.V.Flow.iop.Expr
        let rst = v.Flow.ClearExpr
        let fn = getFuncName()

        [|
            let running = v.MM.Expr <&&> !@call.End <&&> !@iop
            yield running --@ (v.TOUT, v.System._tout.Value, fn)
            if RuntimePackage.PCSIM = RuntimeDS.Package then
                yield (vOff, rst) ==| (v.ErrOnTimeOver , fn)
            else 
                yield (v.TOUT.DN.Expr, rst) ==| (v.ErrOnTimeOver , fn)
        |]
        //test ahn 구현 필요  onTimeShortagem,  offTimeShortage 2구현 필요 Job.JobTime.MAX 이용
    member v.E2_CallErrTimeShortage() =
        //let v= v :?> CoinVertexTagManager
        //let call= v.Vertex.GetPure() :?> Call
        //let vOff = v._off.Expr

        //let iop = call.V.Flow.iop.Expr
        //let rst = v.Flow.ClearExpr
        //let fn = getFuncName()

        [|
            //let running = v.MM.Expr <&&> !@call.End <&&> !@iop
            //yield running --@ (v.TOUT, v.System._tout.Value, fn)
            //if RuntimePackage.PCSIM = RuntimeDS.Package then
            //    yield (vOff, rst) ==| (v.ErrOnTimeOver , fn)
            //else 
            //    yield (v.TOUT.DN.Expr, rst) ==| (v.ErrOffTimeShortage , fn)
        |]

    member v.E3_CallErrorRXMonitor() =
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
        


    member v.E4_RealErrorTotalMonitor() =
        let real = v.Vertex :?> Real
        let rst = v._off.Expr
         
        (real.Errors.ToOrElseOff(), rst) --| (v.ErrTRX, getFuncName())

   
    member v.E5_CallErrorTotalMonitor() =
        let v= v :?> CoinVertexTagManager
        let call= v.Vertex.GetPure() :?> Call
        (call.Errors.ToOrElseOff() , v._off.Expr) --| (v.ErrTRX, getFuncName())


