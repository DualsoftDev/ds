namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module ConvertCpuVertex =


       
    let getSafetyExpr(xs:Call seq, sys:DsSystem) =    
        if xs.any()
        then
            (xs.Select(fun f->f.EndActionOnlyIO).ToAndElseOn() <&&> !!sys._sim.Expr)
            <||> 
            (xs.Select(fun f->f.EndPlan).ToAndElseOn() <&&> sys._sim.Expr)
        else 
            sys._on.Expr

    type Vertex with
        member r.V = r.TagManager :?> VertexManager
        member r.VC = r.TagManager :?> VertexMCall
        member r.VR = r.TagManager :?> VertexMReal
        member r._on  = r.Parent.GetSystem()._on
        member r._off = r.Parent.GetSystem()._off

    type Call with
        member c._on     = c.System._on
        member c._off    = c.System._off
        member c.JM    = c.TargetJob.TagManager :?> JobManager

        member c.HasSensor  =
            match c.IsJob with
            | true -> 
                c.TargetJob.DeviceDefs
                    .Where(fun d-> d.ExistInput)
                    .any()
            | false -> false
            

        member c.UsingTon  = c.CallOperatorType = DuOPTimer
        member c.UsingCompare  = c.CallOperatorType = DuOPCode //test ahn
        member c.UsingNot  = c.CallOperatorType = DuOPNot
        member c.UsingMove  = c.CallCommandType = DuCMDCode
        member c.EndPlan =  
                    if c.IsCommand
                    then
                        (c.TagManager :?> VertexMCall).CallCommandEnd.Expr
                    elif c.IsOperator
                    then
                        (c.TagManager :?> VertexMCall).CallOperatorValue.Expr
                    else 
                        c.TargetJob.ApiDefs.Select(fun f->f.PE).ToAnd()

        member c.EndActionOnlyIO = 
                if c.UsingNot 
                    then 
                        if c.HasSensor
                        then !!c.JM.JobOrExprTag.Expr  //안전상이 이유로 센서 OR 사용시 job을 분해해서 사용(하나라도 감지되지 않아야)
                        else failwithf $"$n 함수는 실제 Input address가 있어야 가능합니다. {c.Name} "   

                else
                    if c.HasSensor 
                        then c.JM.JobAndExprTag.Expr
                        else c.EndPlan

        member c.EndAction = 
                if c.UsingTon  then c.VC.TDON.DN.Expr
                else c.EndActionOnlyIO

        member c.GetEndAction(x:ApiItem) =
            let td = c.TargetJob.DeviceDefs.First(fun d->d.ApiItem = x) 
            if td.InAddress <> TextSkip && td.InAddress <> TextAddrEmpty
            then 
                let inTag = td.InTag :?> Tag<bool>
                if c.UsingNot  then !!inTag.Expr|>Some
                               else inTag.Expr  |>Some
            else 
                None
      
        member c.SyncExpr =
                 let rv = c.Parent.GetCore().TagManager :?>  VertexMReal
                 !!rv.SYNC.Expr <&&> (rv.G.Expr <||> rv.Flow.h_st.Expr)

        member c.PresetTime =   if c.UsingTon
                                then c.TargetJob.OperatorFunction.Value.GetDelayTime()
                                else failwith $"{c.Name} not use timer" 

        //member c.PresetCounter = if c.UsingCtr
        //                         then c.TargetJob.Func.Value.GetRingCount()
        //                         else failwith $"{c.Name} not use counter"
        
        member c.PSs =
            if c.IsJob 
            then c.TargetJob.DeviceDefs.Select(fun f->f.ApiItem.PS)
            else [c.VC._on]

        member c.PEs =
            if c.IsJob 
            then c.TargetJob.DeviceDefs.Select(fun f->f.ApiItem.PE)
            else [c.VC.CallCommandEnd]

        member c.TXs = 
            if c.IsJob
            then c.TargetJob.DeviceDefs |>Seq.collect(fun j -> j.ApiItem.TXs)
            else []

        member c.RXs = 
            if c.IsJob
            then c.TargetJob.DeviceDefs |>Seq.collect(fun j -> j.ApiItem.RXs)
            else []

        member c.Errors       = 
                                [
                                    getVMCoin(c).ErrTimeOver
                                    getVMCoin(c).ErrTimeShortage 
                                    getVMCoin(c).ErrShort 
                                    getVMCoin(c).ErrOpen 
                                ]
                         
        member c.MutualResetCalls =  c.System.S.MutualCalls[c].Cast<Call>()
          
        member c.SafetyExpr = getSafetyExpr(c.SafetyConditions.Choose(fun f->f.GetSafetyCall()), c.System)

        member c.StartPointExpr =
            let f = c.Parent.GetFlow()
            match c.Parent.GetCore() with
            | :? Real as r ->
                let rv = r.TagManager :?>  VertexMReal
                let initOnCalls  = rv.OriginInfo.CallInitials
                                     .Where(fun (_,ty) -> ty = InitialType.On)
                                     .Select(fun (call,_)->call)
               
                if initOnCalls.Contains(c)
                    then 
                        f.h_st.Expr <&&> (// 실제에서는 수동일때만 h_st 가능 ,시뮬레이션은 자동수동 둘다가능
                                     (!!c.EndActionOnlyIO <&&> !!c.System._sim.Expr)    
                                     <||>
                                     (!!c.EndPlan <&&>  c.System._sim.Expr )
                                     )   

                    else c._off.Expr
            | _ ->  
                c._off.Expr


    type Real with
        member r.V = r.TagManager :?> VertexMReal

        member r.CoinSTContacts = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ST)
        member r.CoinRTContacts = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.RT)
        member r.CoinETContacts = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ET)
        member r.CoinAllContacts = r.Graph.Vertices.Select(getVMCoin)|>Seq.collect(fun f->[f.ST;f.RT;f.ET])

        member r.CoinAlloffExpr = !!r.V.CoinAnyOnST.Expr <&&> !!r.V.CoinAnyOnRT.Expr <&&> !!r.V.CoinAnyOnET.Expr

        member r.ErrTimeOvers   = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ErrTimeOver) 
        member r.ErrTimeShortages   = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ErrTimeShortage) 
        member r.ErrOpens   = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ErrOpen) 
        member r.ErrShorts   = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ErrShort) 
        member r.Errors     = r.ErrTimeOvers @ r.ErrTimeShortages @ r.ErrOpens @ r.ErrShorts  @ [ r.VR.ErrGoingOrigin  ]
        member r.SafetyExpr = getSafetyExpr(r.SafetyConditions.Choose(fun f->f.GetSafetyCall()), r.Parent.GetSystem())


