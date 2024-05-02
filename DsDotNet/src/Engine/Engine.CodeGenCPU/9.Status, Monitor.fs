[<AutoOpen>]
module Engine.CodeGenCPU.ConvertMonitor

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type VertexManager with
    ///Status
    member v.S1_RGFH() =
            let r = v.R <==  (( (!!) v.ST.Expr                       <&&> (!!) v.ET.Expr) //    R      x   -   x
                              <||> ( v.ST.Expr <&&>       v.RT.Expr  <&&> (!!) v.ET.Expr))//           o   o   x
            let g = v.G <==        ( v.ST.Expr <&&>  (!!) v.RT.Expr  <&&> (!!) v.ET.Expr) //    G      o   x   x
            let f = v.F <==        (                 (!!) v.RT.Expr  <&&>      v.ET.Expr) //    F      -   x   o
            let h = v.H <==        (                      v.RT.Expr  <&&>      v.ET.Expr) //    H      -   o   o

            [
               withExpressionComment $"{getFuncName()}{v.Name}(Ready)"        r
               withExpressionComment $"{getFuncName()}{v.Name}(Going)"        g
               withExpressionComment $"{getFuncName()}{v.Name}(Finish)"       f
               withExpressionComment $"{getFuncName()}{v.Name}(Homming)"      h
            ]

    ///Monitor
    member v.M1_OriginMonitor() =
        let v = v :?> VertexMReal

        let ons       = getOriginIOExprs     (v, InitialType.On)
        let onSims    = getOriginSimPlanEnds (v, InitialType.On)

        let offs      = getOriginIOExprs     (v, InitialType.Off)
        let offSims   = getOriginSimPlanEnds (v, InitialType.Off)

        let onExpr    = if ons.any() then ons.ToAndElseOff() else v._on.Expr
        let offExpr   = if offs.any() then offs.ToOrElseOn() else v._off.Expr

        let onSimExpr    = onSims.ToAndElseOn()
        let offSimExpr   = offSims.ToOrElseOff()

        let set =   (onExpr     <&&> (!!offExpr)    <&&> v.SYNC.Expr)
                <||>(onSimExpr  <&&> (!!offSimExpr) <&&> v._sim.Expr)

        (set, v._off.Expr) --| (v.OG, getFuncName())

    member v.M2_PauseMonitor() =
        let set = v.Flow.pause.Expr
        let rst = v._off.Expr

        (set, rst) --| (v.PA, getFuncName())

    member v.M3_CallErrorTXMonitor() =
        let v= v :?> VertexMCall
        let call= v.Vertex.GetPure() :?> Call
        let real= call.Parent.GetCore() :?> Real
        let iop = call.V.Flow.iop.Expr
        let rst = v.Flow.clear_btn.Expr
        [
            let running = v.MM.Expr <&&> !!call.EndActionOnlyIO <&&> !!iop
            yield running --@ (v.TOUT, v.System._tout.Value, getFuncName())

            match RuntimeDS.Package with 
            | Developer ->  yield (v.TOUT.DN.Expr <||> (real.V.RF.Expr <&&> v._sim.Expr), rst) ==| (v.ErrTimeOver , getFuncName())
            | Simulation -> yield (call._off.Expr, rst) ==| (v.ErrTimeOver , getFuncName())
            | _ ->          yield (v.TOUT.DN.Expr, rst) ==| (v.ErrTimeOver , getFuncName())
        ]

    member v.M4_CallErrorRXMonitor() =
        let callV = v :?> VertexMCall
        let call  = v.Vertex.GetPure() :?> Call
        let real  = call.Parent.GetCore() :?> Real
        
        let dop = call.V.Flow.d_st.Expr
        let rst = v.Flow.clear_btn.Expr
        [
            let using      = if call.HasSensor then v._on.Expr else  v._off.Expr 
            let input      = call.EndActionOnlyIO

            let offSet     = callV.RXErrOpenOff
            let offRising  = callV.RXErrOpenRising
            let offTemp    = callV.RXErrOpenTemp
                                
            let onSet      = callV.RXErrShortOn
            let onRising   = callV.RXErrShortRising
            let onTenmp    = callV.RXErrShortTemp

            let RxReadyExpr  =  call.RXs.Select(fun f -> f.V.R).ToAndElseOff()
            let RxFinishExpr =  call.RXs.Select(fun f -> f.V.F).ToAndElseOff()
       
            yield! (using <&&> input  , v.ErrShort.Expr)  --^ (onRising,   onSet, onTenmp, "RXErrShortOn")
            yield! (using <&&> !!input, v.ErrOpen.Expr)   --^ (offRising, offSet, offTemp, "RXErrOpenOff")

            yield (dop <&&> real.V.G.Expr <&&>   call.V.G.Expr <&&> onRising.Expr  <&&> RxReadyExpr,  rst<||>v._sim.Expr) ==| (v.ErrTimeShortage, getFuncName())
            yield (dop <&&> real.V.G.Expr <&&> !!call.V.G.Expr <&&> onRising.Expr  <&&> RxReadyExpr,  rst<||>v._sim.Expr) ==| (v.ErrShort, getFuncName())
            if call.UsingTon
            then //단락 에레는 시간적용한 감지시 채터링 이슈로 Call Going 시 제외시키고 부모 Going일때만 체크
                yield (dop <&&> real.V.G.Expr <&&>  !!call.V.G.Expr <&&>  offRising.Expr  <&&> RxFinishExpr, rst<||>v._sim.Expr) ==| (v.ErrOpen,  getFuncName())
            else 
                yield (dop <&&> real.V.G.Expr <&&>                      offRising.Expr  <&&> RxFinishExpr, rst<||>v._sim.Expr) ==| (v.ErrOpen,  getFuncName())
        ]
        

    member v.M5_RealErrorTotalMonitor() =
        let real = v.Vertex :?> Real
        let rst = v._off.Expr
        [
            (real.ErrOpens.ToOrElseOff(), rst)  --| (v.ErrOpen, getFuncName())
            (real.ErrShorts.ToOrElseOff(), rst) --| (v.ErrShort, getFuncName())
            (real.ErrTimeShortages.ToOrElseOff(), rst) --| (v.ErrTimeShortage, getFuncName())
            (real.ErrTimeOvers.ToOrElseOff(), rst) --| (v.ErrTimeOver, getFuncName())
            (real.Errors.ToOrElseOff(), rst) --| (v.ErrTRX, getFuncName())
        ]

   
    member v.M6_CallErrorTotalMonitor() =
        let v= v :?> VertexMCall
        let call= v.Vertex.GetPure() :?> Call
        (call.Errors.ToOrElseOff() , v._off.Expr) --| (v.ErrTRX,   getFuncName())


