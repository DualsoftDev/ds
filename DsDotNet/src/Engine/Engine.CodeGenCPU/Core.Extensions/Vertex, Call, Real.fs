namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module ConvertCpuVertex =

    type Vertex with
        member r.V = r.TagManager :?> VertexTagManager
        member r.VC = r.TagManager :?> CoinVertexTagManager
        member r.VR = r.TagManager :?> RealVertexTagManager
        member r._on  = r.Parent.GetSystem()._on
        member r._off = r.Parent.GetSystem()._off
        member r.MutualResetCoins =
            let mts = r.Parent.GetSystem().S.MutualCalls
            match r with
            | :? Call as c 
                -> if c.IsJob then mts[r]
                              else []
            | _ -> mts[r]

    type VariableData with
        member v.VM = v.TagManager :?> VariableManager

    type ActionVariable  with
        member av.VM = av.TagManager :?> ActionVariableManager


    let getAgvTimes(c:Call) = c.TaskDefs.SelectMany(fun td->td.ApiItems.Where(fun f->f.TX.TimeAvgExist))

    type Call with
        member c._on     = c.System._on
        member c._off    = c.System._off

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

        member c.UsingCompare  = c.CallOperatorType = DuOPCode //test ahn
        member c.UsingMove  = c.CallCommandType = DuCMDCode

        member c.EndPlan =
            if c.IsCommand then
                (c.TagManager :?> CoinVertexTagManager).CallCommandEnd.Expr
            elif c.IsOperator then
                (c.TagManager :?> CoinVertexTagManager).CallOperatorValue.Expr
            else
                c.TaskDefs.Select(fun td-> td.GetPlanEnd(c.TargetJob)).ToAnd()

        member c.TimeOutMaxMSec     =  c.TargetJob.JobTime.TimeOutMaxMSec
        member c.TimeDelayCheckMSec = c.TargetJob.JobTime.TimeDelayCheckMSec
        member c.UsingTimeDelayCheck  = c.IsJob && c.TargetJob.JobTime.TimeDelayCheckMSec > 0u

        member c.EndAction = if c.IsJob then c.TargetJob.ActionInExpr else None
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
                    .Select(fun td->td.GetInExpr(c.TargetJob))

            if tds.any() then
                Some(tds.ToAnd())
            else
                None

        //member c.UpdateChildRealExpr(x:ApiItem) =
        //    let td = c.TaskDefs.First(fun d->d.ApiItem = x)
        //    if td.ExistInput
        //    then
        //        Some(td.GetInExpr(c.TargetJob))
        //    else
        //        None

        member c.RealLinkExpr =
                 let rv = c.Parent.GetCore().TagManager :?>  RealVertexTagManager
                 !@rv.Link.Expr <&&> (rv.G.Expr <||> rv.OB.Expr<||> rv.OA.Expr)

       
        //member c.PresetCounter = if c.UsingCtr
        //                         then c.TargetJob.Func.Value.GetRingCount()
        //                         else failwith $"{c.Name} not use counter"

        //member c.PSs =
        //    if c.IsJob
        //    then c.TaskDefs.Select(fun f->f.PS)
        //    else [c.VC._on]

        member c.PEs =
            if c.IsJob
            then c.TaskDefs.Select(fun f->f.GetPlanEnd(c.TargetJob))
            else [c.VC.CallCommandEnd]

        member c.TXs =
            if c.IsJob
            then c.TaskDefs |>Seq.map(fun td -> td.GetApiItem(c.TargetJob).TX)
            else []

        member c.RXs =
            if c.IsJob
            then c.TaskDefs |>Seq.map(fun td -> td.GetApiItem(c.TargetJob).RX)
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


        member c.SafetyExpr   = c.SafetyConditions.Choose(fun f->f.GetJob().ActionInExpr).ToAndElseOn()
        member c.AutoPreExpr = c.AutoPreConditions.Choose(fun f->f.GetJob().ActionInExpr).ToAndElseOn()

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
