[<AutoOpen>]
module Engine.CodeGenCPU.ConvertJob

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

let getCallTags(coin:Call) = 
    coin.TargetJob.DeviceDefs
        .Where(fun d-> d.OutAddress <> TextSkip
                    && d.OutAddress <> TextAddrEmpty)
        .Select(fun d-> d.InTag :?> Tag<bool>)

type VertexManager with
   
    member v.J1_JobActionSensor() =
        let vc = v :?> VertexMCall
        let coin = v.Vertex :?> Call
        [ 
            match coin.TargetHasJob with
            | true -> 
                (getCallTags(coin).ToAnd(), coin._off.Expr) --| (vc.JobAndSensor, getFuncName())
                (getCallTags(coin).ToOr(), coin._off.Expr) --| (vc.JobOrSensor, getFuncName())
            | false ->
                failWithLog $"{coin.Name} Target Have not Job error "
        ]
