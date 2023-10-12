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

        let onExpr    = if ons.any() then ons.ToAnd() else v._on.Expr
        let offExpr   = if offs.any() then offs.ToOr() else v._off.Expr

        let onSimExpr    = onSims.ToAndElseOn v.System
        let offSimExpr   = offSims.ToOrElseOff v.System

        let set =   (onExpr    <&&> locks    <&&> (!!offExpr))
                <||>(onSimExpr <&&> lockSims <&&> (!!offSimExpr) <&&> v._sim.Expr)

        (set, v._off.Expr) --| (v.OG, getFuncName())

    member v.M2_PauseMonitor(): CommentedStatement  =
        let set = v.Flow.sop.Expr
        let rst = v._off.Expr

        (set, rst) --| (v.PA, getFuncName())

    member v.M3_CallErrorTXMonitor(): CommentedStatement list =
        let v= v :?> VertexMCoin
        let call= v.Vertex.GetPure() :?> CallDev
        let rst = v.Flow.clear.Expr
        [
            let tds = call.CallTargetJob.DeviceDefs
                          .Where(fun f->f.ApiItem.TXs.any() && f.ApiItem.RXs.any())
           
            for td in tds do
                let api = td.ApiItem
                let running = api.PS.Expr <&&> !!td.ActionINFunc 
                yield running --@ (api.TOUT, v.System._tout.Value, getFuncName())
                yield (api.TOUT.DN.Expr, rst) ==| (api.TXErr , getFuncName())

            let sets = tds.Select(fun s->s.ApiItem.TXErr).ToOrElseOff(v.System)
            yield (sets, v._off.Expr) --| (v.E1, getFuncName())
        ]

    member v.M4_CallErrorRXMonitor(): CommentedStatement  =
        let call = v.Vertex :?> CallDev
        let In_Rxs =
            [ for j in call.CallTargetJob.DeviceDefs do
                if j.ApiItem.RXs.Any()
                then yield j.ActionINFunc, j.ApiItem.RXs.Select(getVM) ]

        let onErr =
            let on =
                [ for (input, rxs) in In_Rxs do
                    input <&&> rxs.Select(fun f -> f.G).ToOr() ]
            if on.Any() then on.ToOr() else v.System._off.Expr

        let offErr =
            let off =
                [ for (input, rxs) in In_Rxs do
                    !!input <&&> rxs.Select(fun f -> f.H).ToOr()]
            if off.Any() then off.ToOr() else v.System._off.Expr

        let set = (onErr <||> offErr) <&&> !!v._sim.Expr
        let rst = v.Flow.clear.Expr

        (set, rst) ==| (v.E2, getFuncName())


    member v.M5_RealErrorTXMonitor(): CommentedStatement  =
        let real = v.Vertex :?> Real
        let set = real.ErrorTXs.ToOrElseOff v.System
        let rst = v._off.Expr

        (set, rst) --| (v.E1, getFuncName())


    member v.M6_RealErrorRXMonitor(): CommentedStatement  =
        let real = v.Vertex :?> Real
        let set = real.ErrorRXs.ToOrElseOff v.System
        let rst = v._off.Expr

        (set, rst) --| (v.E2, getFuncName())

