[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCounter

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS




type DsSystem with

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
