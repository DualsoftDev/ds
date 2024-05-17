namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module ConvertCpuTaskDev =
    

    type TaskDev with
        member td.ExistInput   = addressExist td.InAddress
        member td.ExistOutput  = addressExist td.OutAddress

    type HwSystemDef with 
        member s.ActionINFunc = s.GetInExpr() 
