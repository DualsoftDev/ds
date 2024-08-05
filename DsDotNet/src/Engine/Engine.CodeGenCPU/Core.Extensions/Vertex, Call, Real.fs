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
        member r.VC = r.TagManager :?> CallVertexTagManager
        member r.VR = r.TagManager :?> RealVertexTagManager
        member r._on  = r.Parent.GetSystem()._on
        member r._off = r.Parent.GetSystem()._off
        member r.MutualResetCoins = 
            let mutual = r.Parent.GetSystem().S.MutualCalls
            mutual[r]
    
    type VariableData with
        member v.VM = v.TagManager :?> VariableManager  
        
    type ActionVariable  with
        member av.VM = av.TagManager :?> ActionVariableManager


    let getAgvTimes(c:Call) = c.TargetJob.TaskDefs.SelectMany(fun td->td.ApiItems.Where(fun f->f.TX.TimeAvgExist))
    let getDelayTimes(c:Call) = c.TargetJob.TaskDefs.SelectMany(fun td->td.ApiItems.Where(fun f->f.TX.TimeDelayExist))
    type Call with
        member c._on     = c.System._on
        member c._off    = c.System._off

        member c.HasSensor  =
            match c.IsJob with
            | true -> 
                c.TargetJob.TaskDefs
                    .Where(fun d-> d.ExistInput)
                    .any()
            | false -> false

        member c.ExistAvgTime    = getAgvTimes(c).any()
        member c.ExistDelayTime  = getDelayTimes(c).any()

        member c.MaxAvgTime    = getAgvTimes(c).Max()
        member c.MaxDelayTime  = getDelayTimes(c).Max()

        member c.UsingTon  = c.IsJob && c.ExistDelayTime
        member c.UsingCompare  = c.CallOperatorType = DuOPCode //test ahn
        member c.UsingMove  = c.CallCommandType = DuCMDCode

        member c.EndPlan =  
            if c.IsCommand then
                (c.TagManager :?> CallVertexTagManager).CallCommandEnd.Expr
            elif c.IsOperator then
                (c.TagManager :?> CallVertexTagManager).CallOperatorValue.Expr
            else 
                c.TargetJob.TaskDefs.Select(fun td-> td.GetPlanEnd(c.TargetJob)).ToAnd()

        member c.EndAction = 
            if c.IsJob then
                c.TargetJob.ActionInExpr 
            else
                None   
                        
        member c.End = c.EndAction.DefaultValue(c.EndPlan)

        member c.EndWithTimer = 
            if  c.UsingTon then
                c.VC.TDON.DN.Expr
            else
                c.End


        member c.IsAnalogOutput = 
            c.TargetJob.TaskDefs
                .All(fun td-> 
                    td.OutTag.IsNonNull() 
                    && td.OutTag.DataType <> typedefof<bool>) 

        member c.GetEndAction() =
            let tds =
                c.TargetJob.TaskDefs
                    .Where(fun td->td.ExistInput)
                    .Select(fun td->td.GetInExpr(c.TargetJob))

            if tds.any() then 
                Some(tds.ToAnd())
            else 
                None

        //member c.UpdateChildRealExpr(x:ApiItem) =
        //    let td = c.TargetJob.TaskDefs.First(fun d->d.ApiItem = x) 
        //    if td.ExistInput
        //    then 
        //        Some(td.GetInExpr(c.TargetJob))
        //    else 
        //        None

        member c.RealLinkExpr =
                 let rv = c.Parent.GetCore().TagManager :?>  RealVertexTagManager
                 !@rv.Link.Expr <&&> (rv.G.Expr <||> rv.OB.Expr<||> rv.OA.Expr)

        member c.PresetTime =   if c.UsingTon
                                then c.MaxDelayTime.ToString() |> CountUnitType.Parse
                                else failwith $"{c.Name} not use timer"

        //member c.PresetCounter = if c.UsingCtr
        //                         then c.TargetJob.Func.Value.GetRingCount()
        //                         else failwith $"{c.Name} not use counter"
        
        //member c.PSs =
        //    if c.IsJob 
        //    then c.TargetJob.TaskDefs.Select(fun f->f.PS)
        //    else [c.VC._on]

        member c.PEs =
            if c.IsJob 
            then c.TargetJob.TaskDefs.Select(fun f->f.GetPlanEnd(c.TargetJob))
            else [c.VC.CallCommandEnd]

        member c.TXs = 
            if c.IsJob
            then c.TargetJob.TaskDefs |>Seq.map(fun td -> td.GetApiItem(c.TargetJob).TX)
            else []

        member c.RXs = 
            if c.IsJob
            then c.TargetJob.TaskDefs |>Seq.map(fun td -> td.GetApiItem(c.TargetJob).RX)
            else []

        member c.Errors = 
            let vmc = getVMCoin(c)
            [|
                vmc.ErrOnTimeOver
                vmc.ErrOnTimeShortage 
                vmc.ErrOffTimeOver
                vmc.ErrOffTimeShortage 
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
                                     .Where(fun (_,ty) -> ty = InitialType.On)
                                     .Select(fun (call,_)->call)
               
                if initOnCalls.Contains(c)
                    then 
                        (r.VR.OB.Expr <||> r.VR.OA.Expr) 
                        <&&> r.Flow.mop.Expr <&&> !@r.VR.OG.Expr <&&> !@c.End
                        
                        //(// 실제에서는 수동일때만 h_st 가능 ,시뮬레이션은 자동수동 둘다가능
                        //             (!@c.EndActionOnlyIO <&&> !@c.System._sim.Expr)    
                        //             <||>
                        //             (!@c.EndPlan <&&>  c.System._sim.Expr )
                        //             )   

                    else c._off.Expr
            | _ ->  
                c._off.Expr


    type Real with
        member r.V = r.TagManager :?> RealVertexTagManager

        member r.RealSEQ:uint32 = r.V.GetVertexTag(VertexTag.realSEQ).BoxedValue :?> uint32

        member r.CoinSTContacts = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ST)
        member r.CoinRTContacts = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.RT)
        member r.CoinETContacts = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ET)
        member r.CoinAllContacts = r.Graph.Vertices.Select(getVMCoin)|>Seq.collect(fun f->[f.ST;f.RT;f.ET])

        member r.CoinAlloffExpr = !@r.V.CoinAnyOnST.Expr <&&> !@r.V.CoinAnyOnRT.Expr <&&> !@r.V.CoinAnyOnET.Expr

        member r.ErrOnTimeOvers   = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ErrOnTimeOver) 
        member r.ErrOnTimeShortages   = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ErrOnTimeShortage) 
        
        member r.ErrOffTimeOvers   = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ErrOffTimeOver) 
        member r.ErrOffTimeShortages   = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ErrOffTimeShortage) 

        member r.ErrOpens   = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ErrOpen) 
        member r.ErrShorts   = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ErrShort) 

        member r.Errors     = r.ErrOnTimeOvers  @ r.ErrOnTimeShortages 
                            @ r.ErrOffTimeOvers @ r.ErrOffTimeShortages 
                            @ r.ErrOpens @ r.ErrShorts  @ [ r.VR.ErrGoingOrigin  ]

[<Extension>]
type RealExt =
    [<Extension>]
    static member GetRealSEQ(r:Real):uint32 = r.RealSEQ

