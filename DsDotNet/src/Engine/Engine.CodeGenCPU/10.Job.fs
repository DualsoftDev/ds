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
        match coin.IsJob with
        | true -> 
            let sensors = getCallTags(coin)
            let sets = if sensors.any() then sensors.ToAnd() else vc._off.Expr
            (sets, coin._off.Expr) --| (vc.JobAndSensor, getFuncName())
        | false ->
            failWithLog $"{coin.Name} Target Have not Job error "
