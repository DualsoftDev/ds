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
            | Developer ->  yield (v.TOUT.DN.Expr <||> (real.V.RF.Expr <&&> v._sim.Expr), rst) ==| (v.ErrOnTimeOver , getFuncName())
            | Simulation -> yield (call._off.Expr, rst) ==| (v.ErrOnTimeOver , getFuncName())
            | _ ->          yield (v.TOUT.DN.Expr, rst) ==| (v.ErrOnTimeOver , getFuncName())
        ]

    member v.M4_CallErrorRXMonitor() =
        let call  = v.Vertex.GetPure() :?> Call
        let real  = call.Parent.GetCore() :?> Real
        
        let dop = call.V.Flow.d_st.Expr
        let rst = v.Flow.clear_btn.Expr
        [
            let using      = if call.HasSensor then v._on.Expr else  v._off.Expr 
            let input      = call.EndActionOnlyIO
            let checkCondi = using <&&> dop <&&> real.V.G.Expr 

            let rxReadyExpr  =  call.RXs.Select(fun f -> f.V.R).ToAndElseOff()
            let rxFinishExpr =  call.RXs.Select(fun f -> f.V.F).ToAndElseOff()
       
            yield (fbRisingAfter [input] :> IExpression<bool> , v._off.Expr) --| (v.ErrShortRising, getFuncName())
            yield (fbFallingAfter[input] :> IExpression<bool> , v._off.Expr) --| (v.ErrOpenRising,  getFuncName())
            
            (* short error *)
            yield (checkCondi <&&>  rxReadyExpr <&&> v.ErrShortRising.Expr,  rst<||>v._sim.Expr)  ==| (v.ErrShort, getFuncName())
            (* open  error *)
            if call.UsingTon
            then
                yield (checkCondi <&&>  rxFinishExpr <&&> !!call.V.G.Expr <&&> v.ErrOpenRising.Expr,   rst<||>v._sim.Expr)  ==| (v.ErrOpen , getFuncName())
            else
                yield (checkCondi <&&>  rxFinishExpr                      <&&> v.ErrOpenRising.Expr,   rst<||>v._sim.Expr)  ==| (v.ErrOpen , getFuncName())
        ]
        

    member v.M5_RealErrorTotalMonitor() =
        let real = v.Vertex :?> Real
        let rst = v._off.Expr
        [
            (real.ErrOpens.ToOrElseOff(), rst)  --| (v.ErrOpen, getFuncName())
            (real.ErrShorts.ToOrElseOff(), rst) --| (v.ErrShort, getFuncName())
            (real.ErrOnTimeShortages.ToOrElseOff(), rst) --| (v.ErrOnTimeShortage, getFuncName())
            (real.ErrOnTimeOvers.ToOrElseOff(), rst) --| (v.ErrOnTimeOver, getFuncName())
            (real.ErrOffTimeShortages.ToOrElseOff(), rst) --| (v.ErrOffTimeShortage, getFuncName())
            (real.ErrOffTimeOvers.ToOrElseOff(), rst) --| (v.ErrOffTimeOver, getFuncName())
            (real.Errors.ToOrElseOff(), rst) --| (v.ErrTRX, getFuncName())
        ]

   
    member v.M6_CallErrorTotalMonitor() =
        let v= v :?> VertexMCall
        let call= v.Vertex.GetPure() :?> Call
        (call.Errors.ToOrElseOff() , v._off.Expr) --| (v.ErrTRX,   getFuncName())


