[<AutoOpen>]
module Engine.CodeGenCPU.ConvertMonitor

open System.Linq
open Engine.Core
open Engine.CodeGenCPU


type VertexManager with

    member v.M1_OriginMonitor(): CommentedStatement  =
        let real = v.Vertex :?> Real
        let ons    = getOriginIOs (real, InitialType.On)
        let offs   = getOriginIOs (real, InitialType.Off)
        let locks  = getNeedCheckExpression (real)

        let onExpr   = ons.ToAndElseOn v.System
        let rst     = offs.ToOrElseOff v.System

        (onExpr <&&> locks, rst) --| (v.OG, "M1" )

    member v.M2_PauseMonitor(): CommentedStatement  =
        let set = v.Flow.rop.Expr <||> v.Flow.rop.Expr
        let rst = v._off.Expr

        (set, rst) --| (v.PA, "M2" )

    member v.M3_CallErrorTXMonitor(): CommentedStatement list =
        let v= v :?> VertexMCoin
        let set = v.G.Expr <&&> v.TON.Expr
        let rst = v.Flow.clear.Expr <||> v.System._clear.Expr
        [
            //test ahn  going 직전시간 기준 타임아웃 시간 받기
            // 일단을 system 10초 타임아웃
            (v.G.Expr) --@ (v.TOUT, v.System._tout.Value, "M3")
            (set, rst) ==| (v.E1, "M3")
        ]

    member v.M4_CallErrorRXMonitor(): CommentedStatement  =
        let call = v.Vertex :?> Call
        let In_Rxs =
            [ for j in call.CallTargetJob.DeviceDefs do
                j.InTag:?>PlcTag<bool>, j.ApiItem.RXs.Select(getVM)]

        let onEventErr =
            [ for (input, rxs) in In_Rxs do
                input.Expr <&&> !!rxs.Select(fun f -> f.G).ToAndElseOn(v.System) ]

        let offEventErr =
            [ for (input, rxs) in In_Rxs do
                input.Expr <&&> rxs.Select(fun f -> f.H).ToOrElseOff(v.System)]

        let set = (onEventErr.ToOr() <||> offEventErr.ToOr())
        let rst = v.Flow.clear.Expr <||> v.System._clear.Expr

        (set, rst) ==| (v.E2, "M4" )


    member v.M5_RealErrorTXMonitor(): CommentedStatement  =
        let real = v.Vertex :?> Real
        let set = real.ErrorTXs.ToOrElseOff v.System
        let rst = v._off.Expr

        (set, rst) ==| (v.E1, "M5" )


    member v.M6_RealErrorRXMonitor(): CommentedStatement  =
        let real = v.Vertex :?> Real
        let set = real.ErrorRXs.ToOrElseOff v.System
        let rst = v._off.Expr

        (set, rst) ==| (v.E2, "M6" )

