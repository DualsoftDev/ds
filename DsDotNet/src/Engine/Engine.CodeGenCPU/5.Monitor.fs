[<AutoOpen>]
module Engine.CodeGenCPU.ConvertMonitor

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type VertexManager with

    member v.M1_OriginMonitor(): CommentedStatement  =
        let v = v :?> VertexMReal
        let real = v.Vertex :?> Real

        let ons       = getOriginIOExprs     (v, InitialType.On)
        let onSims    = getOriginSimPlanEnds (v, InitialType.On)

        let offs      = getOriginIOExprs     (v, InitialType.Off)
        let offSims   = getOriginSimPlanEnds (v, InitialType.Off)

        let locks     = getNeedCheckIOs (real, false)
        let lockSims  = getNeedCheckIOs (real ,true)

        let onExpr    = if ons.any() then ons.ToAndElseOff() else v._on.Expr
        let offExpr   = if offs.any() then offs.ToOrElseOn() else v._off.Expr

        let onSimExpr    = onSims.ToAndElseOn()
        let offSimExpr   = offSims.ToOrElseOff()

        let set =   (onExpr    <&&> locks    <&&> (!!offExpr))
                <||>(onSimExpr <&&> lockSims <&&> (!!offSimExpr) <&&> v._sim.Expr)

        (set, v._off.Expr) --| (v.OG, getFuncName())

    member v.M2_PauseMonitor(): CommentedStatement  =
        let set = v.Flow.sop.Expr
        let rst = v._off.Expr

        (set, rst) --| (v.PA, getFuncName())

    member v.M3_CallErrorTXMonitor(): CommentedStatement list =
        let v= v :?> VertexMCoin
        let call= v.Vertex.GetPure() :?> Call
        let real= call.Parent.GetCore() :?> Real
        let dop = call.V.Flow.dop.Expr
        let rst = v.Flow.clear_btn.Expr
        [
            let running = v.MM.Expr <&&> !!call.INsFuns <&&> dop
            yield running --@ (v.TOUT, v.System._tout.Value, getFuncName())

            match RuntimeDS.Package with 
            | SimulationDubug ->  yield (v.TOUT.DN.Expr <||> (real.V.RF.Expr <&&> v._sim.Expr), rst) ==| (v.ErrTimeOver , getFuncName())
            | Simulation ->   yield (call._off.Expr, rst) ==| (v.ErrTimeOver , getFuncName())
            | _ ->         yield (v.TOUT.DN.Expr, rst) ==| (v.ErrTimeOver , getFuncName())
         
        ]

    member v.M4_CallErrorRXMonitor(): CommentedStatement list =
        let callV = v :?> VertexMCoin
        let call  = v.Vertex.GetPure() :?> Call
        let real  = call.Parent.GetCore() :?> Real
        
        let dop = call.V.Flow.dop.Expr
        let rst = v.Flow.clear_btn.Expr
        [
            let input      = call.INsFuns
            let offSet     = callV.RXErrOpenOff
            let offRising  = callV.RXErrOpenRising
            let offTemp    = callV.RXErrOpenTemp
                                
            let onSet      = callV.RXErrShortOn
            let onRising   = callV.RXErrShortRising
            let onTenmp    = callV.RXErrShortTemp

            yield! (input  , v.ErrShort.Expr)  --^ (onRising,   onSet, onTenmp, "RXErrShortOn")
            yield! (!!input, v.ErrOpen.Expr)   --^ (offRising, offSet, offTemp, "RXErrOpenOff")

            yield (dop <&&> real.V.G.Expr <&&> onRising.Expr  <&&> call.RXs.Select(fun f -> f.V.R).ToAndElseOff() , rst<||>v._sim.Expr) ==| (v.ErrShort, getFuncName())
            yield (dop <&&> real.V.G.Expr <&&> offRising.Expr <&&> call.RXs.Select(fun f -> f.V.F).ToAndElseOff() , rst<||>v._sim.Expr) ==| (v.ErrOpen,  getFuncName())
        ]
        

    member v.M5_RealErrorTotalMonitor(): CommentedStatement list =
        let real = v.Vertex :?> Real
        let rst = v._off.Expr
        [
            (real.ErrOpens.ToOrElseOff(), rst)  --| (v.ErrOpen, getFuncName())
            (real.ErrShorts.ToOrElseOff(), rst) --| (v.ErrShort, getFuncName())
            (real.ErrTrendOuts.ToOrElseOff(), rst) --| (v.ErrTrendOut, getFuncName())
            (real.ErrTimeOvers.ToOrElseOff(), rst) --| (v.ErrTimeOver, getFuncName())
            (real.Errors.ToOrElseOff(), rst) --| (v.ErrTRX, getFuncName())
        ]

   
    member v.M6_CallErrorTotalMonitor(): CommentedStatement  =
        let v= v :?> VertexMCoin
        let call= v.Vertex.GetPure() :?> Call
        (call.Errors.ToOrElseOff() , v._off.Expr) --| (v.ErrTRX,   getFuncName())
