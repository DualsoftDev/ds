[<AutoOpen>]
module Engine.CodeGenCPU.ConvertEmulation

open System
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type TaskDev with

    member d.SensorEmulation(sys:DsSystem, job:Job) =
        let set = d.GetPE(job).Expr
        let rst = sys._off.Expr

        let inParam = d.GetInParam(job)
        if inParam.Type = DuBOOL// || api.PureName = api.Name //bool type 은 파라메터 있는 타입은 제외    
        then 
            let setBool = if inParam.Value.IsNull() || (inParam.Value |> Convert.ToBoolean)
                          then set 
                          else !@set

            (setBool, rst) --| (d.InTag, getFuncName())
        else 

            let setData = inParam.Value|>literal2expr
            (set, setData) --> (d.InTag, getFuncName())

type DsSystem with

    member s.SetFlagForEmulation() =

        let set = s._on.Expr
        let rst = s._off.Expr

        (set, rst) --| (s._emulation, getFuncName())
