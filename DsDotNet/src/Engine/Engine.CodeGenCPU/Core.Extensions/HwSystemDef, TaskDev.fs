namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module ConvertCpuTaskDev =
    
    let addressExist address = address  <> TextSkip && address <> TextAddrEmpty

    type TaskDev with
        member td.AO = td.OutTag :?> Tag<bool>
        member td.ExistInput   = addressExist td.InAddress
        member td.ExistOutput  = addressExist td.OutAddress

    type HwSystemDef with
        member s.ActionINFunc = 
            match  s.InTag with
            | :? Tag<bool> as inTag -> inTag.Expr  //test ahn not 처리
                //if hasNot (s.OperatorFunction)
                //then !!inTag.Expr else inTag.Expr
            | _ -> failwithf $"{s.Name} input address is empty."
