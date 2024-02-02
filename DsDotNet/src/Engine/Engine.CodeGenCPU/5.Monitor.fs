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
        let tds = call.TargetJob.DeviceDefs
                          .Where(fun f->f.ApiItem.TXs.any() && f.ApiItem.RXs.any())
        [
          
            for td in tds do
                let api = td.ApiItem
                let running = api.PS.Expr <&&> !!td.ActionINFunc  <&&> dop
                yield running --@ (api.TOUT, v.System._tout.Value, getFuncName())
                if(RuntimeDS.Package = RuntimePackage.SimulationDubug)
                then 
                    yield (api.TOUT.DN.Expr <||> (real.V.RF.Expr <&&> v._sim.Expr), rst) ==| (api.TXErrOverTime , getFuncName())
                elif(RuntimeDS.Package = RuntimePackage.Simulation)
                then 
                    yield (call._off.Expr, rst) ==| (api.TXErrOverTime , getFuncName())
                else 
                    yield (api.TOUT.DN.Expr, rst) ==| (api.TXErrOverTime , getFuncName())

            let sets = tds.Select(fun s->s.ApiItem.TXErrOverTime).ToOrElseOff()
            yield (sets, v._off.Expr) --| (v.E1, getFuncName())
        ]

    member v.M4_CallErrorRXMonitor(): CommentedStatement list =
        let call= v.Vertex.GetPure() :?> Call
        let real= call.Parent.GetCore() :?> Real
        
        let dop = call.V.Flow.dop.Expr
        let rst = v.Flow.clear_btn.Expr
        let tds = call.TargetJob.DeviceDefs.Where(fun f->f.ApiItem.RXs.any())
        [
            for td in tds do
                let input, rxs = td.ActionINFunc, td.ApiItem.RXs.Select(getVM)
                let offSet     = td.ApiItem.RXErrOpenOff
                let offRising  = td.ApiItem.RXErrOpenRising
                let offTemp    = td.ApiItem.RXErrOpenTemp
                let offErr     = td.ApiItem.RXErrOpen

                let onSet      = td.ApiItem.RXErrShortOn
                let onRising   = td.ApiItem.RXErrShortRising
                let onTenmp    = td.ApiItem.RXErrShortTemp
                let onErr      = td.ApiItem.RXErrShort

                yield! (input  , onErr.Expr)   --^ (onRising,   onSet, onTenmp, "RXErrShortOn")
                yield! (!!input, offErr.Expr)  --^ (offRising, offSet, offTemp, "RXErrOpenOff")

                yield (dop <&&> real.V.G.Expr <&&> onRising.Expr  <&&> rxs.Select(fun f -> f.R).ToAndElseOff() , rst<||>v._sim.Expr) ==| (onErr,   getFuncName())
                yield (dop <&&> real.V.G.Expr <&&> offRising.Expr <&&> rxs.Select(fun f -> f.F).ToAndElseOff() , rst<||>v._sim.Expr) ==| (offErr,  getFuncName())

            let sets = tds |> Seq.collect(fun s->[s.ApiItem.RXErrOpen;s.ApiItem.RXErrShort])

            if(RuntimeDS.Package = RuntimePackage.Simulation)
            then 
                yield (v._off.Expr, v._off.Expr) --| (v.E2, getFuncName())
            else 
                yield (sets.ToOrElseOff(), v._off.Expr) --| (v.E2, getFuncName())


        ]
        

    member v.M5_RealErrorTXMonitor(): CommentedStatement  =
        let real = v.Vertex :?> Real
        let set = real.ErrorTXs.ToOrElseOff()
        let rst = v._off.Expr

        (set, rst) --| (v.E1, getFuncName())


    member v.M6_RealErrorRXMonitor(): CommentedStatement  =
        let real = v.Vertex :?> Real
        let set = real.ErrorRXs.ToOrElseOff()
        let rst = v._off.Expr

        (set, rst) --| (v.E2, getFuncName())

  
    member v.M7_CallErrorTRXMonitor(): CommentedStatement list =
        let v= v :?> VertexMCoin
        let call= v.Vertex.GetPure() :?> Call
        let tds = call.TargetJob.DeviceDefs
        [
            for td in tds do
                let errs = 
                    [
                        td.ApiItem.RXErrOpen
                        td.ApiItem.RXErrShort
                        td.ApiItem.TXErrOverTime
                        td.ApiItem.TXErrTrendOut
                    ]
                yield (errs.ToOrElseOff() , v._off.Expr) --| (td.ApiItem.TRxErr,   getFuncName())


            yield (tds.Select(fun d-> d.ApiItem.TRxErr).ToOrElseOff() , v._off.Expr) --| (v.ErrTRX,   getFuncName())
        ]

    member v.M8_RealErrorTRXMonitor(): CommentedStatement  =
        let real = v.Vertex :?> Real
        let txs = real.ErrorRXs.ToOrElseOff()
        let rxs = real.ErrorRXs.ToOrElseOff()
        (txs <||> rxs, v._off.Expr) --| (v.ErrTRX, getFuncName())
