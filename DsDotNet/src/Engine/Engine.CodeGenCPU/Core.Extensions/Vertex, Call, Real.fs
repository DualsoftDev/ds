namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module ConvertCpuVertex =


    let getSafetyExpr(xs:Call seq, sys:DsSystem) =        
        (xs.Select(fun f->f.EndAction).ToAndElseOn() <&&> !!sys._sim.Expr)
        <||> 
        (xs.Select(fun f->f.EndPlan).ToAndElseOn() <&&> sys._sim.Expr)

    type Vertex with
        member r.V = r.TagManager :?> VertexManager
        member r.VC = r.TagManager :?> VertexMCoin
        member r.VR = r.TagManager :?> VertexMReal
        member r._on  = r.Parent.GetSystem()._on
        member r._off = r.Parent.GetSystem()._off

    type Call with
        member c._on     = c.System._on
        member c._off     = c.System._off

        member c.InTags  = c.TaskDevs.Where(fun d->d.ApiItem.RXs.any() && d.InAddress <> TextSkip)
                                                 .Select(fun d->d.InTag :?> Tag<bool>)

        member c.UsingTon  = c.TargetJob.Func |> hasTime
        member c.UsingCtr  = c.TargetJob.Func |> hasCount
        member c.UsingNot  = c.TargetJob.Func |> hasNot
        member c.UsingMove = c.TargetJob.Func |> hasMove
      
        member c.EndPlan = c.TargetJob.ApiDefs.Select(fun f->f.PE).ToAndElseOff()
        member c.EndAction = 
                if c.UsingMove   then c._on.Expr  //todo : Move 처리 완료시 End
                elif c.UsingCtr  then c.VC.CTR.DN.Expr 
                elif c.UsingTon  then c.VC.TDON.DN.Expr
                elif c.UsingNot  then 
                                 if c.InTags.any() 
                                 then !!c.InTags.ToOrElseOff() 
                                 else failwithf $"'Not function' requires an InTag. {c.Name} input error"   

                else c.InTags.ToAndElseOn() 


        member c.GetEndAction(x:ApiItem) =
            let inTag = c.TaskDevs.First(fun d->d.ApiItem = x).InTag :?> Tag<bool>
            if c.UsingNot  then !!inTag.Expr
                           else inTag.Expr
      
        member c.SyncExpr =
                 let rv = c.Parent.GetCore().TagManager :?>  VertexMReal
                 !!rv.SYNC.Expr  <&&> rv.G.Expr 

        member c.PresetTime =   if c.UsingTon
                                then c.TargetJob.Func.Value.GetDelayTime()
                                else failwith $"{c.Name} not use timer" 

        member c.PresetCounter = if c.UsingCtr
                                 then c.TargetJob.Func.Value.GetRingCount()
                                 else failwith $"{c.Name} not use counter"
        
        member c.PSs           = c.TaskDevs.Where(fun j -> j.ApiItem.TXs.any()).Select(fun f->f.ApiItem.PS)
        member c.PEs           = c.TaskDevs.Where(fun j -> j.ApiItem.RXs.any()).Select(fun f->f.ApiItem.PE)
        member c.TXs           = c.TaskDevs|>Seq.collect(fun j -> j.ApiItem.TXs)
        member c.RXs           = c.TaskDevs|>Seq.collect(fun j -> j.ApiItem.RXs)
        member c.Errors       = 
                                [
                                    getVMCoin(c).ErrTimeOver
                                    getVMCoin(c).ErrTrendOut 
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
                        let homeManuAct = f.mop.Expr <&&> f.home_btn.Expr
                        let homeAutoAct = f.dop.Expr <&&> rv.RO.Expr
                        let homeAct =  homeManuAct <||> homeAutoAct
                        homeAct <&&> (!!c.EndAction <&&> !!c.System._sim.Expr)    
                                     <||>
                                     (!!c.EndPlan <&&> c.System._sim.Expr)     

                    else c._off.Expr
            | _ ->  
                c._off.Expr


    type Real with
        member r.V = r.TagManager :?> VertexMReal
        member r.CoinRelays = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ET)
        member r.ErrTimeOvers   = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ErrTimeOver) 
        member r.ErrTrendOuts   = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ErrTrendOut) 
        member r.ErrOpens   = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ErrOpen) 
        member r.ErrShorts   = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ErrShort) 
        member r.Errors     = r.ErrTimeOvers @ r.ErrTrendOuts @ r.ErrOpens @ r.ErrShorts 
        member r.SafetyExpr = getSafetyExpr(r.SafetyConditions.Choose(fun f->f.GetSafetyCall()), r.Parent.GetSystem())


