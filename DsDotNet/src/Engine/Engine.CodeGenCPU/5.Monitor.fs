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
        let locks  = getNeedCheck (real)
        
        let onExpr   = ons.EmptyOnElseToAnd v.System
        let rsts     = offs.EmptyOffElseToOr v.System

        (onExpr <&&> locks, rsts) --| (v.OG, "M1" )

    member v.M2_PauseMonitor(): CommentedStatement  = 
        let sets = v.Flow.rop.Expr <||> v.Flow.rop.Expr 
        let rsts = v.System._off.Expr

        (sets, rsts) --| (v.PA, "M2" )

    member v.M3_CallErrorTXMonitor(): CommentedStatement list = 
        let v= v :?> VertexMCoin
        let sets = v.G.Expr <&&> v.TON.Expr
        let rsts = v.Flow.clear.Expr <||> v.System._clear.Expr
        [
            //test ahn  going 직전시간 기준 타임아웃 시간 받기
            // 일단을 system 10초 타임아웃
            (v.G.Expr) --@ (v.TOUT, v.System._tout.Value, "M3")
            (sets, rsts) ==| (v.E1, "M3")
        ]

    member v.M4_CallErrorRXMonitor(): CommentedStatement  = 
        let call = v.Vertex :?> Call
        let In_Rxs  = call.CallTargetJob.JobDefs
                        .Select(fun j -> j.InTag:?>PlcTag<bool>, j.ApiItem.RXs.Select(getVM))

        let onEventErr  = In_Rxs.Select(fun (input, rxs) -> 
                        input.Expr <&&> !!rxs.Select(fun f -> f.G).EmptyOnElseToAnd(v.System))

        let offEventErr = In_Rxs.Select(fun (input, rxs) -> 
                        input.Expr <&&> rxs.Select(fun f -> f.H).EmptyOffElseToOr(v.System))

        let sets = (onEventErr.ToOr() <||> offEventErr.ToOr())
        let rsts = v.Flow.clear.Expr <||> v.System._clear.Expr

        (sets, rsts) ==| (v.E2, "M4" )


    member v.M5_RealErrorTXMonitor(): CommentedStatement  = 
        let real = v.Vertex :?> Real
        let sets = real.ErrorTXs.EmptyOffElseToOr v.System
        let rsts = v.System._off.Expr

        (sets, rsts) ==| (v.E1, "M5" )


    member v.M6_RealErrorRXMonitor(): CommentedStatement  = 
        let real = v.Vertex :?> Real
        let sets = real.ErrorRXs.EmptyOffElseToOr v.System
        let rsts = v.System._off.Expr

        (sets, rsts) ==| (v.E2, "M6" )

