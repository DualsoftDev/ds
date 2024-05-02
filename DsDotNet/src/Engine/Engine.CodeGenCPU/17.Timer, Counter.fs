[<AutoOpen>]
module Engine.CodeGenCPU.ConvertTimerCounter

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type DsSystem with

    member s.T1_DelayCall() =
        let allVertices = s.GetVertices()
        let calls = allVertices.OfType<Call>()
                          .Where(fun f->f.UsingTon)
        let aliasCalls = allVertices.GetAliasTypeCalls()
                          .Where(fun f -> f.TargetWrapper.CallTarget().Value.UsingTon)

        let ends (call:Call) = 
            (call.EndPlan  <&&> call.VC._sim.Expr)
            <||>
            (call.EndActionOnlyIO <&&> !!call.VC._sim.Expr)
            
        [
            for call in calls do
                let sets = call.V.ST.Expr <||>  call.V.SF.Expr <&&>  ends(call)
                yield (sets) --@ (call.VC.TDON, call.PresetTime, getFuncName())

            for alias in aliasCalls do
                let call = alias.V.Vertex.GetPureCall().Value
                let sets = alias.V.ST.Expr<||>  alias.V.SF.Expr  <&&> ends(call)
                yield (sets) --@ (alias.VC.TDON, call.PresetTime, getFuncName())
        ]

    member s.C1_FinishRingCounter() = ()  //test ahn real 반복으로 수정 필요
        //let allVertices = s.GetVertices()
        //let calls = allVertices.OfType<Call>()
        //                  .Where(fun f->f.UsingCtr)
        //let aliasCalls = allVertices.GetAliasTypeCalls()
        //                  .Where(fun f -> f.TargetWrapper.CallTarget().Value.UsingCtr)
        //[
        //    for call in calls do
        //        let sets = call.V.F.Expr
        //        yield (sets) --% (call.VC.CTR, call.PresetCounter,  getFuncName())

        //    for alias in aliasCalls do
        //        let call = alias.V.Vertex.GetPureCall().Value
        //        let sets = alias.V.F.Expr
        //        yield (sets) --% (alias.VC.CTR, call.PresetCounter, getFuncName())
        //]
