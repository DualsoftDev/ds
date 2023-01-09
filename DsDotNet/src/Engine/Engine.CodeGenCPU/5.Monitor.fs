[<AutoOpen>]
module Engine.CodeGenCPU.ConvertMonitor

open System.Linq
open Engine.Core
open Engine.CodeGenCPU

let getOriginIOs(real:Real, initialType:InitialType) =
    let origins, resetChains = OriginHelper.GetOriginsWithJobDefs real.Graph
    let ios = 
        origins
            .Where(fun w-> w.Value = initialType)
            .Select(fun s-> s.Key.InTag)
            .Cast<PlcTag<bool>>()
    ios

let getNeedCheckExpr(interlocks:PlcTag<bool> seq) =
    let sets = 
        interlocks
         .Select(fun il -> il.Expr <&&> !!interlocks.Except([il]).ToOr())
    sets


type VertexManager with
   
    member v.M1_OriginMonitor(): CommentedStatement  = 
        let real = v.Vertex :?> Real
        let ons    = getOriginIOs (real, InitialType.On)
        let offs   = getOriginIOs (real, InitialType.Off)
        let checks = getOriginIOs (real, InitialType.NeedCheck)
        let locks  = getNeedCheckExpr (checks)
        //test ahn 인터락 원위치 필요
        
        let onExpr   = ons.EmptyOnElseToAnd v.System
        let lockExpr = if locks.Any() then locks.ToAnd() else v.System._on.Expr
        let rsts     = offs.EmptyOffElseToOr v.System

        (onExpr <&&> lockExpr, rsts) --| (v.OG, "M1" )

    member v.M2_PauseMonitor(): CommentedStatement  = 
        let sets = v.Flow.eop.Expr <||> v.Flow.sop.Expr 
        let rsts = v.System._off.Expr

        (sets, rsts) --| (v.PA, "M2" )

    member v.M3_CallErrorTXMonitor(): CommentedStatement list = 
        let call = v.Vertex :?> Call
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
        let In_Rxs  = call.CallTarget.JobDefs
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

