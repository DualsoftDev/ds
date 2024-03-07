[<AutoOpen>]
module Engine.CodeGenCPU.ConvertTimer

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
                let sets = call.V.ST.Expr <&&>  ends(call)
                yield (sets) --@ (call.VC.TDON, call.PresetTime, getFuncName())

            for alias in aliasCalls do
                let call = alias.V.GetPureCall().Value
                let sets = alias.V.ST.Expr <&&> ends(call)
                yield (sets) --@ (alias.VC.TDON, call.PresetTime, getFuncName())
        ]
