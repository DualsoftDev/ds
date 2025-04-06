namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System
open System
open System.IO
open System.Threading.Tasks
open System


[<AutoOpen>]
module ConvertCpuCall =

    type Call with
        member c._on     = c.System._on
        member c._off    = c.System._off

        member c.HasSensor  = c.TaskDefs.Where(fun d-> d.ExistInput).Any()
        member c.HasAnalogSensor  =
            if c.HasSensor
            then
                c.TaskDefs
                    .Where(fun d-> d.ExistInput && d.InTag.DataType <> typedefof<bool>)
                    .Any()
            else
                false



        member c.MaxDelayTime  = c.CallTime.TimeDelayCheckMSec

        member c.EndPlan =
            if c.IsCommand then
                (c.TagManager :?> CoinVertexTagManager).CallCommandEnd.Expr
            elif c.IsOperator then
                (c.TagManager :?> CoinVertexTagManager).CallOperatorValue.Expr
            else
                c.VC.PE.Expr


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
                .Any(fun td-> td.IsAnalogActuator || td.IsAnalogSensor)


        member c.GetEndAction() =
            let tds =
                c.TaskDefs
                    .Where(fun td->td.ExistInput)
                    .Select(fun td->td.GetInExpr(c))

            if tds.Any() then
                Some(tds.ToAnd())
            else
                None


        member c.RealLinkExpr =
                 let rv = c.Parent.GetCore().TagManager :?>  RealVertexTagManager
                 !@rv.Link.Expr <&&> (rv.G.Expr <||> rv.OB.Expr<||> rv.OA.Expr)


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
                vmc.ErrInterlock
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


        member c.ActionInExpr =
            let inExprs =
                c.TaskDefs.Where(fun d-> d.ExistInput)
                          .Select(fun d-> d.GetInExpr(c))

            if inExprs.Any()
            then inExprs.ToAnd() |>Some

            else None

        member c.ActionOutExpr =
            let outExprs =
                c.TaskDefs.Where(fun d-> d.ExistOutput)
                          .Select(fun d-> d.GetOutExpr(c))

            if outExprs.Any()
            then outExprs.ToOr()|>Some
            else None

        member c.MutualResetCoins =
            let mts = c.Parent.GetSystem().S.MutualCalls
            if c.IsJob then mts[c] else []

        member c.MutualResetExpr =
                    c.MutualResetCoins
                                .Choose(tryGetPureCall)
                                .Choose(fun c->c.EndAction)
                                .Distinct()
                                .ToOrElseOff()





//[<Extension>]
//type CallExt =
//    [<Extension>]
//    static member GetSourceToken(c:Call):uint32 = c.SourceToken
