namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module ConvertCpuTaskDev =
    

    type HwSystemDef with 
        member s.ActionINFunc = s.GetInExpr() 
        member s.DigitalOutputTarget =
            if s.OutDataType = DuBOOL then
                match s.TaskDevParamIO.OutParam with 
                | Some o -> Some (o.ValueParam.TargetValue.Value |>  Convert.ToBoolean)
                | None -> None
            else 
                None
