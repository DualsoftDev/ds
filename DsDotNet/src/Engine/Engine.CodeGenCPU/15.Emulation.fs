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
        let set = coins.GetPureCalls().Select(fun c-> d.GetPlanEnd(c.TargetJob)).ToOr()
        let rst = sys._off.Expr

        let inParam = d.GetInParam(job)
        if inParam.Type = DuBOOL then // || api.PureName = api.Name //bool type 은 파라메터 있는 타입은 제외            
            let setBool =
                if inParam.Value.IsNull() || (inParam.Value |> Convert.ToBoolean) then
                    set
                else
                    !@set

            (setBool, rst) --| (d.InTag, getFuncName())
        else 
            let setData = inParam.Value|>literal2expr
            (set, setData) --> (d.InTag, getFuncName())

type DsSystem with

    member s.SetFlagForEmulation() =

        let set = s._on.Expr
        let rst = s._off.Expr

        (set, rst) --| (s._emulation, getFuncName())
