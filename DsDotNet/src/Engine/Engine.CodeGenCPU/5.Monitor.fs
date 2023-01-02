[<AutoOpen>]
module Engine.CodeGenCPU.ConvertMonitor

open System.Linq
open Engine.Core
open Engine.CodeGenCPU

let getOriginIOs(real:Real, initialType:InitialType) =
    let origins = OriginHelper.GetOriginsWithJobDefs real.Graph
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
        real.Parent.GetSystem().GenerationJobIO()
        let ons    = getOriginIOs (real, InitialType.On)
        let offs   = getOriginIOs (real, InitialType.Off)
        let checks = getOriginIOs (real, InitialType.NeedCheck)
        let locks  = getNeedCheckExpr (checks)

        let onExpr   = if ons.Any() then ons.ToAnd() else v.System._on.Expr
        let lockExpr = if locks.Any() then locks.ToOr() else v.System._on.Expr
        let rsts     = if offs.Any() then offs.ToAnd() else v.System._off.Expr

        (onExpr <&&> lockExpr, rsts) --| (v.OG, "M1" )

    member v.M2_PauseMonitor(): CommentedStatement  = 
        let sets = v.Flow.eop.Expr <||> v.Flow.sop.Expr 
        let rsts = v.System._off.Expr

        (sets, rsts) --| (v.PA, "M2" )

        //test ahn
    member v.M3_CallErrorTXMonitor(): CommentedStatement  = 
        let call = v.Vertex :?> Call
        let sets = v.Flow.eop.Expr <||> v.Flow.sop.Expr   //test ahn timmer 적용
        let rsts = v.Flow.clear.Expr <||> v.System._clear.Expr

        (sets, rsts) ==| (v.E1, "M3" )


    member v.M4_CallErrorRXMonitor(): CommentedStatement  = 
        let call = v.Vertex :?> Call
        let sets = (v.G.Expr <&&> call.INs.ToOr())
                   <||> (v.H.Expr <&&> !!call.INs.ToOr())
        let rsts = v.Flow.clear.Expr <||> v.System._clear.Expr

        (sets, rsts) ==| (v.E2, "M4" )


        //test ahn
    member v.M5_RealErrorTXMonitor(): CommentedStatement  = 
        let real = v.Vertex :?> Real
        let sets = v.Flow.eop.Expr <||> v.Flow.sop.Expr   //test ahn timmer 적용
        let rsts = v.Flow.clear.Expr <||> v.System._clear.Expr

        (sets, rsts) ==| (v.E1, "M5" )


        //test ahn
    member v.M6_RealErrorRXMonitor(): CommentedStatement  = 
        let real = v.Vertex :?> Real
        let sets = v.Flow.clear.Expr <||> v.System._clear.Expr
        let rsts = v.Flow.clear.Expr <||> v.System._clear.Expr

        (sets, rsts) ==| (v.E2, "M7" )
   
   