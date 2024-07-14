namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module ConvertCpuVertex =


       
    let getSafetyNAutoPreConditionExpr(xs:Call seq, sys:DsSystem) =    
        if xs.any()
        then xs.Select(fun f->f.End).ToAnd()
        else sys._on.Expr

    type Vertex with
        member r.V = r.TagManager :?> VertexManager
        member r.VC = r.TagManager :?> VertexMCall
        member r.VR = r.TagManager :?> VertexMReal
        member r._on  = r.Parent.GetSystem()._on
        member r._off = r.Parent.GetSystem()._off
        member r.MutualResetCoins = 
            let mutual = r.Parent.GetSystem().S.MutualCalls
            mutual[r]
    
    type VariableData with
        member v.VM = v.TagManager :?> VariableManager  
        
    type ActionVariable  with
        member av.VM = av.TagManager :?> ActionVariableManager



    type Call with
        member c._on     = c.System._on
        member c._off    = c.System._off

        member c.HasSensor  =
            match c.IsJob with
            | true -> 
                c.TargetJob.DeviceDefs
                    .Where(fun d-> d.ExistInput)
                    .any()
            | false -> false
            

        member c.UsingTon  = c.IsJob && c.TargetJob.OnDelayTime.IsSome //test ahn  real 의 타임으로 변경
        member c.UsingCompare  = c.CallOperatorType = DuOPCode //test ahn
        member c.UsingMove  = c.CallCommandType = DuCMDCode

        member c.EndPlan =  
            if c.IsCommand
            then
                (c.TagManager :?> VertexMCall).CallCommandEnd.Expr
            elif c.IsOperator
            then
                (c.TagManager :?> VertexMCall).CallOperatorValue.Expr
            else 
                c.TargetJob.DeviceDefs.Select(fun f-> f.PE).ToAnd()

        member c.EndAction = 
                    if c.IsJob 
                    then c.TargetJob.ActionInExpr 
                    else None   
                        
        member c.End = 
                if c.EndAction.IsSome 
                then c.EndAction.Value
                else c.EndPlan

        member c.EndWithTimer = 
                if  c.UsingTon
                then c.VC.TDON.DN.Expr
                else c.End

        member c.GetEndAction(x:ApiItem) =
            let td = c.TargetJob.DeviceDefs.First(fun d->d.ApiItem = x) 
            if td.ExistInput
            then 
                Some(td.GetInExpr(c.TargetJob))
            else 
                None
        

        member c.UpdateChildRealExpr(x:ApiItem) =
            let td = c.TargetJob.DeviceDefs.First(fun d->d.ApiItem = x) 
            if td.ExistInput
            then 
                Some(td.GetInExpr(c.TargetJob))
            else 
                None
      

        member c.LinkExpr =
                 let rv = c.Parent.GetCore().TagManager :?>  VertexMReal
                 !@rv.Link.Expr <&&> (rv.G.Expr <||> rv.OB.Expr<||> rv.OA.Expr)

        member c.PresetTime =   if c.UsingTon
                                then c.TargetJob.OnDelayTime.Value.ToString() |> CountUnitType.Parse
                                else failwith $"{c.Name} not use timer"

        //member c.PresetCounter = if c.UsingCtr
        //                         then c.TargetJob.Func.Value.GetRingCount()
        //                         else failwith $"{c.Name} not use counter"
        
        member c.PSs =
            if c.IsJob 
            then c.TargetJob.DeviceDefs.Select(fun f->f.PS)
            else [c.VC._on]

        member c.PEs =
            if c.IsJob 
            then c.TargetJob.DeviceDefs.Select(fun f->f.PE)
            else [c.VC.CallCommandEnd]

        member c.TXs = 
            if c.IsJob
            then c.TargetJob.DeviceDefs |>Seq.map(fun j -> j.ApiItem.TX)
            else []

        member c.RXs = 
            if c.IsJob
            then c.TargetJob.DeviceDefs |>Seq.map(fun j -> j.ApiItem.RX)
            else []

        member c.Errors       = 
                                [
                                    getVMCoin(c).ErrOnTimeOver
                                    getVMCoin(c).ErrOnTimeShortage 
                                    getVMCoin(c).ErrOffTimeOver
                                    getVMCoin(c).ErrOffTimeShortage 
                                    getVMCoin(c).ErrShort 
                                    getVMCoin(c).ErrOpen 
                                ]
                         
          
        member c.SafetyExpr = getSafetyNAutoPreConditionExpr(c.SafetyConditions.Map(fun f->f.GetCall()), c.System)
        member c.AutoPreExpr = getSafetyNAutoPreConditionExpr(c.AutoPreConditions.Map(fun f->f.GetCall()), c.System)

        member c.StartPointExpr =
            match c.Parent.GetCore() with
            | :? Real as r ->
                let rv = r.TagManager :?>  VertexMReal
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
        member r.V = r.TagManager :?> VertexMReal

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



