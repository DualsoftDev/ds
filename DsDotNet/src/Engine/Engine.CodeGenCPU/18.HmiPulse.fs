[<AutoOpen>]
module Engine.CodeGenCPU.ConvertHmiPulse

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type VertexTagManager with

    member v.H1_HmiPulse(): CommentedStatement [] =
        let fn = getFuncName()
        [|
            yield! (v.SF.Expr, v.System)  --^ (v.SFP, fn) 
            yield! (v.RF.Expr, v.System)  --^ (v.RFP, fn) 
            yield! (v.ON.Expr, v.System)  --^ (v.ONP, fn) 
        |]
