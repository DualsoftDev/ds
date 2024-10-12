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

        [|
            for call in calls do
                if call.UsingTimeDelayCheck then 
                    let sets = call.V.ST.Expr <||>  call.V.SF.Expr <&&> call.EndWithoutTimer
                    yield (sets) --@ (call.VC.TimeCheck, call.TimeDelayCheckMSec, fn)

            for alias in aliasCalls do
                let call = alias.V.Vertex.TryGetPureCall().Value
                if call.UsingTimeDelayCheck then 
                    let sets = alias.V.ST.Expr<||>  alias.V.SF.Expr  <&&> call.EndWithoutTimer
                    yield (sets) --@ (alias.VC.TimeCheck, call.TimeDelayCheckMSec, fn)
        |]
