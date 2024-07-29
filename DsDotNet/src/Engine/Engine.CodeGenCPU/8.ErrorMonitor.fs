[<AutoOpen>]
module Engine.CodeGenCPU.ConvertErrorMonitor

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type VertexTagManager with


    member v.E2_CallErrorTXMonitor() =
        let v= v :?> CallVertexTagManager
        let call= v.Vertex.GetPure() :?> Call
        let vOff = v._off.Expr

        let iop = call.V.Flow.iop.Expr
        let rst = v.Flow.ClearExpr
        [
            let running = v.MM.Expr <&&> !@call.End <&&> !@iop
            yield running --@ (v.TOUT, v.System._tout.Value, getFuncName())
            if RuntimePackage.PCSIM = RuntimeDS.Package   
            then
                yield(vOff, rst) ==| (v.ErrOnTimeOver , getFuncName())
            else 
                yield(v.TOUT.DN.Expr, rst) ==| (v.ErrOnTimeOver , getFuncName())
        ]

    member v.E3_CallErrorRXMonitor() =
        let call  = v.Vertex.GetPure() :?> Call
        let real  = call.Parent.GetCore() :?> Real
        let v = v:?> CallVertexTagManager
        
        let dop = call.V.Flow.d_st.Expr
        let rst = v.Flow.ClearExpr
        [
            let using      = if call.HasSensor then v._on.Expr else  v._off.Expr 
            let input      = call.End
            let checkCondi = using <&&> dop <&&> real.V.G.Expr 

            let rxReadyExpr  =  call.RXs.Select(fun f -> f.V.R).ToAndElseOff()
            let rxFinishExpr =  call.RXs.Select(fun f -> f.V.F).ToAndElseOff()
            if RuntimeDS.Package.IsPLCorPLCSIM() 
            then
                yield (fbRisingAfter [input] :> IExpression<bool> , v._off.Expr) --| (v.ErrShortRising, getFuncName())
                yield (fbFallingAfter[input] :> IExpression<bool> , v._off.Expr) --| (v.ErrOpenRising,  getFuncName())

            elif RuntimeDS.Package.IsPCorPCSIM() then 
                yield! (input, v.System) --^ (v.ErrShortRising, getFuncName())
                yield! (!@input, v.System) --^ (v.ErrOpenRising,  getFuncName())
            else    
                failWithLog $"Not supported {RuntimeDS.Package} package"
            
            (* short error *)
            yield (checkCondi <&&>  rxReadyExpr <&&> v.ErrShortRising.Expr,  rst)  ==| (v.ErrShort, getFuncName())
            (* open  error *)
            if call.UsingTon
            then
                yield (checkCondi <&&> rxFinishExpr <&&> !@call.V.G.Expr <&&> v.ErrOpenRising.Expr, rst)  ==| (v.ErrOpen, getFuncName())
            else
                yield (checkCondi <&&> rxFinishExpr                      <&&> v.ErrOpenRising.Expr, rst)  ==| (v.ErrOpen, getFuncName())
        ]
        

    member v.E4_RealErrorTotalMonitor() =
        let real = v.Vertex :?> Real
        let rst = v._off.Expr
         
        (real.Errors.ToOrElseOff(), rst) --| (v.ErrTRX, getFuncName())

   
    member v.E5_CallErrorTotalMonitor() =
        let v= v :?> CallVertexTagManager
        let call= v.Vertex.GetPure() :?> Call
        (call.Errors.ToOrElseOff() , v._off.Expr) --| (v.ErrTRX, getFuncName())


