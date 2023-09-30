[<AutoOpen>]
module Engine.CodeGenCPU.ConvertTimer

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS



type DsSystem with

    member s.T1_DelayCall(): CommentedStatement list =
        let allVertices = s.GetVertices()
        let calls = allVertices.OfType<CallDev>()
                          .Where(fun f->f.UsingTon)
        let aliasCalls = allVertices.GetAliasTypeCalls()
                          .Where(fun f -> f.TargetWrapper.CallTarget().Value.UsingTon)

      
        [
            for call in calls do
                let sets = call.V.ST.Expr <&&>  call.INsFuns
                yield (sets) --@ (call.V.TDON, call.PresetTime, getFuncName())

            for alias in aliasCalls do
                let call = alias.V.GetPureCall().Value
                let sets = alias.V.ST.Expr <&&> alias.TargetWrapper.CallTarget().Value.INsFuns
                yield (sets) --@ (alias.V.TDON, call.PresetTime, getFuncName())
        ]
