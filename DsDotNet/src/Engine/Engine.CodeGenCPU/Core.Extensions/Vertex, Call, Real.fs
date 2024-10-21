namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module ConvertCpuVertex =

    type Vertex with
        member v.V  = v.TagManager :?> VertexTagManager
        member v.VC = v.TagManager :?> CoinVertexTagManager
        member v.VR = v.TagManager :?> RealVertexTagManager
        member v._on  = v.Parent.GetSystem()._on
        member v._off = v.Parent.GetSystem()._off
        member v.Flow = v.Parent.GetFlow()
        member v.MutualResetCoins =
            let mts = v.Parent.GetSystem().S.MutualCalls
            match v with
            | :? Call as c 
                -> if c.IsJob then mts[v]
                              else []
            | _ -> mts[v]

    type VariableData with
        member v.VM = v.TagManager :?> VariableManager

    type ActionVariable  with
        member av.VM = av.TagManager :?> ActionVariableManager


    let getAgvTimes(c:Call) = c.TaskDefs.Select(fun td->td.ApiItem.TX.TimeAvgExist)

    type Call with
        member c._on     = c.System._on
        member c._off    = c.System._off

        member c.ActionInExpr =
            if not(c.IsJob) then None
            else
                let inExprs =
                    c.TargetJob.TaskDefs.Where(fun d-> d.ExistInput)
                              .Select(fun d-> d.GetInExpr(c))

                if inExprs.any() then
                    if c.ValueParamIO.In.IsNegativeTarget
                    then 
                        !@inExprs.ToAnd() |>Some
                    else
                        inExprs.ToAnd() |>Some
                else
                    None

        
        member j.ActionOutExpr =
            let outExprs =
                j.TaskDefs.Where(fun d-> d.ExistOutput)
                          .Select(fun d-> d.GetOutExpr(j))

            if outExprs.any()
            then outExprs.ToOr()|>Some
            else None



        member c.HasSensor  = c.TaskDefs.Where(fun d-> d.ExistInput).any()
        member c.HasAnalogSensor  =
            if c.HasSensor
            then
                c.TaskDefs
                    .Where(fun d-> d.ExistInput && d.InTag.DataType <> typedefof<bool>)
                    .any()
            else
                false

        member c.ExistAvgTime    = getAgvTimes(c).any()


        member c.MaxDelayTime  = getAgvTimes(c).Max()//getDelayTimes(c).Max() test ahn



        member c.MaxAvgTime    = getAgvTimes(c).Max()


        member c.EndPlan =
            if c.IsCommand then
                (c.TagManager :?> CoinVertexTagManager).CallCommandEnd.Expr
            elif c.IsOperator then
                (c.TagManager :?> CoinVertexTagManager).CallOperatorValue.Expr
            else
                c.PE   

        member c.TimeOutMaxMSec     = c.CallTime.TimeOutMaxMSec
        member c.TimeDelayCheckMSec = c.CallTime.TimeDelayCheckMSec
        member c.UsingTimeDelayCheck  = c.IsJob && c.CallTime.TimeDelayCheckMSec > 0u

        member c.EndAction = if c.IsJob then c.ActionInExpr else None
        member c.EndWithoutTimer = c.EndAction.DefaultValue(c.EndPlan)
        member c.End = 
                if c.UsingTimeDelayCheck then
                    (c.TagManager :?> CoinVertexTagManager).TimeCheck.DN.Expr
                else
                    c.EndWithoutTimer


        member c.IsAnalog =
            c.TaskDefs
                .any(fun td-> td.IsAnalogActuator || td.IsAnalogSensor)


        member c.GetEndAction() =
            let tds =
                c.TaskDefs
                    .Where(fun td->td.ExistInput)
                    .Select(fun td->td.GetInExpr(c))

            if tds.any() then
                Some(tds.ToAnd())
            else
                None


        member c.RealLinkExpr =
                 let rv = c.Parent.GetCore().TagManager :?>  RealVertexTagManager
                 !@rv.Link.Expr <&&> (rv.G.Expr <||> rv.OB.Expr<||> rv.OA.Expr)

        member c.PE =
            if c.IsJob
            then c.VC.PE.Expr
            else c.VC.CallCommandEnd.Expr

        member c.TXs =
            if c.IsJob
            then c.TaskDefs |>Seq.map(fun td -> td.ApiItem.TX)
            else []

        member c.RXs =
            if c.IsJob
            then c.TaskDefs |>Seq.map(fun td -> td.ApiItem.RX)
            else []

        member c.Errors =
            let vmc = getVMCall(c)
            [|
                vmc.ErrOnTimeOver
                vmc.ErrOnTimeUnder
                vmc.ErrOffTimeOver
                vmc.ErrOffTimeUnder
                vmc.ErrShort
                vmc.ErrOpen
            |]


        member c.SafetyExpr   = c.SafetyConditions.Choose(fun f->f.GetCall().ActionInExpr).ToAndElseOn()
        member c.AutoPreExpr = c.AutoPreConditions.Choose(fun f->f.GetCall().ActionInExpr).ToAndElseOn()

        member c.StartPointExpr =
            match c.Parent.GetCore() with
            | :? Real as r ->
                let rv = r.TagManager :?>  RealVertexTagManager
                let initOnCalls  = rv.OriginInfo.CallInitials
                                     .Where(fun (_c, ty) -> ty = InitialType.On)// && not(c.IsAnalog))
                                     .Select(fun (c, _)->c)

                if initOnCalls.Contains(c)
                    then
                        (r.VR.OB.Expr <||> r.VR.OA.Expr)
                        <&&> r.Flow.mop.Expr <&&> !@r.VR.OG.Expr <&&> !@c.End

                    else c._off.Expr
            | _ ->
                c._off.Expr

        member r.SourceToken:uint32 = r.V.GetVertexTag(VertexTag.sourceToken).BoxedValue :?> uint32

    type Real with
        member r.V = r.TagManager :?> RealVertexTagManager

        /// 실제 Token >= 1.   Token 없을 경우, 0 return.
        // 원래 token 없으면 null 을 반환해야 함!!
        member r.RealToken:uint32 = r.V.GetVertexTag(VertexTag.realToken).BoxedValue :?> uint32
        /// 실제 Token >= 1.   Token 없을 경우, 0 return.
        // 원래 token 없으면 null 을 반환해야 함!!
        member r.MergeToken:uint32 = r.V.GetVertexTag(VertexTag.mergeToken).BoxedValue :?> uint32

        member r.CoinSTContacts = r.Graph.Vertices.Select(getVMCall).Select(fun f->f.ST)
        member r.CoinRTContacts = r.Graph.Vertices.Select(getVMCall).Select(fun f->f.RT)
        member r.CoinETContacts = r.Graph.Vertices.Select(getVMCall).Select(fun f->f.ET)
        member r.CoinAllContacts = r.Graph.Vertices.Select(getVMCall)|>Seq.collect(fun f->[f.ST;f.RT;f.ET])

        member r.CoinAlloffExpr = !@r.V.CoinAnyOnST.Expr <&&> !@r.V.CoinAnyOnRT.Expr <&&> !@r.V.CoinAnyOnET.Expr

        member r.ErrOnTimeOvers   = r.Graph.Vertices.Select(getVMCall).Select(fun f->f.ErrOnTimeOver)
        member r.ErrOnTimeUnders   = r.Graph.Vertices.Select(getVMCall).Select(fun f->f.ErrOnTimeUnder)

        member r.ErrOffTimeOvers   = r.Graph.Vertices.Select(getVMCall).Select(fun f->f.ErrOffTimeOver)
        member r.ErrOffTimeUnders   = r.Graph.Vertices.Select(getVMCall).Select(fun f->f.ErrOffTimeUnder)

        member r.ErrOpens   = r.Graph.Vertices.Select(getVMCall).Select(fun f->f.ErrOpen)
        member r.ErrShorts   = r.Graph.Vertices.Select(getVMCall).Select(fun f->f.ErrShort)

        member r.Errors     = r.ErrOnTimeOvers  @ r.ErrOnTimeUnders
                            @ r.ErrOffTimeOvers @ r.ErrOffTimeUnders
                            @ r.ErrOpens @ r.ErrShorts  @ [ r.VR.ErrGoingOrigin  ]

[<Extension>]
type RealExt =
    [<Extension>]
    static member GetRealToken(r:Real):uint32 option = match r.RealToken with | 0u -> None | v -> Some v
    [<Extension>]
    static member GetMergeToken(r:Real):uint32 option = match r.MergeToken with | 0u -> None | v -> Some v

[<Extension>]
type CallExt =
    [<Extension>]
    static member GetSourceToken(c:Call):uint32 = c.SourceToken
