[<AutoOpen>]
module Engine.CodeGenCPU.ConvertTimer

open System.Linq
open Engine.Core
open Engine.CodeGenCPU



type DsSystem with

    member s.T1_DelayCall(): CommentedStatement list =
        let allVertices = s.GetVertices()
        let calls = allVertices.OfType<Call>()
                          .Where(fun f->f.UsingTon)
        let aliasCalls = allVertices.GetAliasTypeCalls()
                          .Where(fun f -> f.TargetWrapper.CallTarget().Value.UsingTon)
        [
            for call in calls do
                let sets = call.V.ST.Expr <&&>  call.INs.ToAndElseOn s
                yield (sets) --@ (call.V.TON, call.PresetTime, "T1")

            for alias in aliasCalls do
                let call = alias.V.GetPureCall().Value
                let sets = alias.V.ST.Expr <&&> alias.TargetWrapper.CallTarget().Value.INs.ToAndElseOn s
                yield (sets) --@ (alias.V.TON, call.PresetTime,"T1")
        ]
