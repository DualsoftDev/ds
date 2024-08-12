[<AutoOpen>]
module Engine.CodeGenCPU.ConvertEmulation

open System
open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type TaskDev with

    member d.SensorEmulation(sys:DsSystem, coins:Vertex seq) =
        let job = coins.OfType<Call>().First().TargetJob
        let rst = sys._off.Expr

        let inParam = d.GetInParam(job)
        [|
        if inParam.Type = DuBOOL then 
            let set = coins.GetPureCalls()
                           .Select(fun c-> d.GetPlanEnd(c.TargetJob)).ToOr()

            let positiveBool = inParam.Value |> Convert.ToBoolean
            yield ((if positiveBool then set else !@set), rst) --| (d.InTag, getFuncName())
        else 
            for c in coins.GetPureCalls() do
                let setData = d.GetInParam(c.TargetJob).Value|>literal2expr
                let set = d.GetPlanEnd(c.TargetJob).Expr
                yield (set, setData) --> (d.InTag, getFuncName())
        |]

type DsSystem with

    member s.SetFlagForEmulation() =

        let set = s._on.Expr
        let rst = s._off.Expr

        (set, rst) --| (s._emulation, getFuncName())
