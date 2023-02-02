[<AutoOpen>]
module Engine.CodeGenCPU.ConvertMonitor

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Engine.Common.FS


type VertexManager with

    member v.M1_OriginMonitor(): CommentedStatement  =
        let real = v.Vertex :?> Real
        let ons    = getOriginIOs (real, InitialType.On)
        let offs   = getOriginIOs (real, InitialType.Off)
        let locks  = getNeedCheckExpression (real)

        let onExpr   = ons.ToAndElseOn v.System
        let rst     = offs.ToOrElseOff v.System

        (onExpr <&&> locks, rst) --| (v.OG, getFuncName())

    member v.M2_PauseMonitor(): CommentedStatement  =
        let set = v.Flow.sop.Expr
        let rst = v._off.Expr

        (set, rst) --| (v.PA, getFuncName())

    member v.M3_CallErrorTXMonitor(): CommentedStatement list =
        let v= v :?> VertexMCoin
        let set = v.G.Expr <&&> v.TON.DN.Expr
        let rst = v.Flow.clear.Expr
        [
            //test ahn  going 직전시간 기준 타임아웃 시간 받기
            // 일단을 system 10초 타임아웃
            (v.G.Expr) --@ (v.TOUT, v.System._tout.Value, getFuncName())
            (set, rst) ==| (v.E1, getFuncName())
        ]

    member v.M4_CallErrorRXMonitor(): CommentedStatement  =
        let call = v.Vertex :?> CallDev
        let In_Rxs =
            [ for j in call.CallTargetJob.DeviceDefs do
                if j.ApiItem.RXs.Any()
                then yield j.InTag:?>Tag<bool>, j.ApiItem.RXs.Select(getVM) ]

        let onErr =
            let on =
                [ for (input, rxs) in In_Rxs do
                    input.Expr <&&> !!rxs.Select(fun f -> f.G).ToAndElseOn(v.System) ]
            if on.Any() then on.ToOr() else v.System._off.Expr

        let offErr =
            let off =
                [ for (input, rxs) in In_Rxs do
                    input.Expr <&&> rxs.Select(fun f -> f.H).ToOrElseOff(v.System)]
            if off.Any() then off.ToOr() else v.System._off.Expr

        let set = (onErr <||> offErr)
        let rst = v.Flow.clear.Expr

        (set, rst) ==| (v.E2, getFuncName())


    member v.M5_RealErrorTXMonitor(): CommentedStatement  =
        let real = v.Vertex :?> Real
        let set = real.ErrorTXs.ToOrElseOff v.System
        let rst = v._off.Expr

        (set, rst) ==| (v.E1, getFuncName())


    member v.M6_RealErrorRXMonitor(): CommentedStatement  =
        let real = v.Vertex :?> Real
        let set = real.ErrorRXs.ToOrElseOff v.System
        let rst = v._off.Expr

        (set, rst) ==| (v.E2, getFuncName())

