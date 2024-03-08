namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module ConvertCpuTaskDev =

    type TaskDev with
        member td.AO = td.OutTag :?> Tag<bool>

    type HwSystemDef with
        member s.ActionINFunc = 
            let inTag = (s.InTag :?> Tag<bool>).Expr
            if hasNot (s.Func)
            then !!inTag else inTag  

    //type ConditionDef with
    //    member s.ErrorConditionTag = s.ErrorCondition

        