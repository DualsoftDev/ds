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
   
    member v.J1_JobActionAndSensor() =
        let vc = v :?> VertexMCall
        let coin = v.Vertex :?> Call
        let sets =
            match coin.TargetHasJob with
            | true -> getCallTags(coin).ToAnd()
            | false ->
                coin._off.Expr //  input 없으면 JobSensor은 off
          
        (sets, coin._off.Expr) --| (vc.JobAndSensor, getFuncName())

    member v.J2_JobActionOrSensor() =
        let vc = v :?> VertexMCall
        let coin = v.Vertex :?> Call
        let sets =
            match coin.TargetHasJob with
            | true -> getCallTags(coin).ToOr()
            | false ->
                coin._off.Expr //  input 없으면 JobSensor은 off
          
        (sets, coin._off.Expr) --| (vc.JobOrSensor, getFuncName())

        