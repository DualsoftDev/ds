[<AutoOpen>]
module Engine.CodeGenCPU.ConvertTimerCounter

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type DsSystem with

    member s.T1_DelayCall() = 
        let calls = s.GetVerticesOfJobCalls()
        let aliasCalls = s.GetVertices().GetAliasTypeCalls()
        let fn = getFuncName()

        //let ends (call:Call) = 
        //    (call.EndPlan  <&&> call.VC._sim.Expr)
        //    <||>
        //    (call.EndActionOnlyIO <&&> !!call.VC._sim.Expr)
          
        [
            for call in calls do
                if call.UsingTon then 
                    let sets = call.V.ST.Expr <||>  call.V.SF.Expr <&&> call.End
                    yield (sets) --@ (call.VC.TDON, call.PresetTime, fn)

            for alias in aliasCalls do
                let call = alias.V.Vertex.GetPureCall().Value
                if call.UsingTon then 
                    let sets = alias.V.ST.Expr<||>  alias.V.SF.Expr  <&&> call.End
                    yield (sets) --@ (alias.VC.TDON, call.PresetTime, fn)
        ]
