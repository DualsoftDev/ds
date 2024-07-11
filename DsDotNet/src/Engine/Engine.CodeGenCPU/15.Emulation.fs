[<AutoOpen>]
module Engine.CodeGenCPU.ConvertEmulation

open System
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type TaskDev with

    member d.SensorEmulation(sys:DsSystem, job:Job) =

        let set = d.PE.Expr
        let rst = sys._off.Expr

        let inParam = d.GetInParam(job)
        if inParam.Type = DuBOOL
        then 
            let setBool = if inParam.Value.IsNull() || (inParam.Value |> Convert.ToBoolean)
                          then set 
                          else !@set

            (setBool, rst) --| (d.InTag, getFuncName())
        else 

            let setData = if inParam.DevValue.IsNull()
                            then failWithLog $"{d.Name} {d.InAddress} 은 value 값을 입력해야 합니다." 
                            else inParam.DevValue.Value|>literal2expr

            (set, setData) --> (d.InTag, getFuncName())

type DsSystem with

    member s.SetFlagForEmulation() =

        let set = s._on.Expr
        let rst = s._off.Expr

        (set, rst) --| (s._emulation, getFuncName())
